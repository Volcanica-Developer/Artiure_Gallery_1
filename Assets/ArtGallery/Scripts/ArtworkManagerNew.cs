using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;

/// <summary>
/// ArtworkManagerNew is responsible for loading and exposing the new exhibition-based
/// artwork JSON format (ArtworkConfig_New.json).
///
/// For now it only reads from a JSON file in a Resources folder, but it is structured
/// so the same JSON can later be fetched from an HTTP API by enabling <see cref="useAPI"/>.
///
/// Typical use:
/// - Place this component in your scene.
/// - Drop ArtworkConfig_New.json into any Resources folder.
/// - Configure the inspector to load from Resources or from an API.
/// - Use the public getters (e.g. GetAllPaintings, GetPaintingsForWall) to drive your UI/logic.
/// </summary>
public class ArtworkManagerNew : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetExhibitionIdFromUrl();
#endif

    [Header("Data Source")]
    [Tooltip("If true, load JSON from a remote API instead of a local Resources JSON file.")]
    [SerializeField] private bool useAPI = false;

    [Tooltip("Resources path (without .json extension) to the local ArtworkConfig_New file.")]
    [SerializeField] private string resourcesJsonPath = "ArtworkConfig_New"; // Resources/ArtworkConfig_New.json

    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "";

    [Tooltip("Exhibition UUID to request from the API. For now this can be hard-coded; later it can come from user selection.")]
    [SerializeField] private string exhibitionId = "e459a070-3f67-4624-8d33-4dc33d1d6af0";

    [Tooltip("Token to be sent as a header in the API request.")]
    [SerializeField] private string token = "";

    [SerializeField] private float apiTimeoutSeconds = 10f;

    [Header("Auto Setup")]
    [Tooltip("If true, automatically load data on startup.")]
    [SerializeField] private bool loadOnAwake = false; // Default off; will be triggered from UI.

    [Tooltip("Delay (in seconds) before automatically loading and building layouts when loadOnAwake is true.")]
    [SerializeField] private float autoSetupDelaySeconds = 0.5f;

    [Tooltip("Optional delay (in seconds) between building each layout/image set, to avoid bursting many image requests at once.")]
    [SerializeField] private float layoutBuildDelaySeconds = 0f;

    // Custom headers for API requests (e.g. Authorization). Not serialized in Inspector.
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>();

    [Header("Debug / State")]
    [SerializeField] private bool isLoading = false;
    [SerializeField] private bool lastLoadSucceeded = false;

    // Parsed data from the new JSON format
    [SerializeField] private ArtworkConfigNew currentConfig;

    [Header("Scene References")]
    [Tooltip("All GameObjects in the scene that have a DisplayWall component.")]
    [SerializeField] private List<DisplayWall> displayWalls = new List<DisplayWall>();

    [Header("Layout Prefabs")]
    [Tooltip("Prefabs that contain a FrameLayout component, one per JSON layoutId (e.g. layout_28).")]
    [SerializeField] private List<FrameLayout> layoutPrefabs = new List<FrameLayout>();

    [Header("Image Download Progress")]
    [SerializeField] private int totalImagesToDownload = 0;
    [SerializeField] private int downloadedImagesCount = 0;
    [SerializeField] private int successfulDownloads = 0;
    [SerializeField] private int failedDownloads = 0;

    [Header("Download Retry Settings")]
    [Tooltip("Number of retry attempts for failed image downloads.")]
    [SerializeField] private int maxRetryAttempts = 3;

    [Tooltip("Delay in seconds between retry attempts.")]
    [SerializeField] private float retryDelaySeconds = 1f;

    [Tooltip("Timeout in seconds specifically for image downloads (separate from API timeout).")]
    [SerializeField] private float imageDownloadTimeoutSeconds = 30f;

    [Header("Music")]
    [Tooltip("Optional AudioSource used to play exhibition music fetched from the API.")]
    [SerializeField] private AudioSource musicAudioSource;

    [Tooltip("Last downloaded/assigned music clip from the exhibition data.")]
    [SerializeField] private AudioClip currentMusicClip;

    [Header("Exhibition Media")]
    [Tooltip("VideoPlayer component to display the exhibition video.")]
    [SerializeField] private VideoPlayer exhibitionVideoPlayer;

    [Tooltip("SpriteRenderer to display the exhibition preview image.")]
    [SerializeField] private SpriteRenderer previewImageRenderer;

    [Header("Artist Info")]
    [Tooltip("TMP_Text component to display the artist's full name.")]
    [SerializeField] private TMP_Text artistNameText;

    [Tooltip("TMP_Text component to display the artist's statement.")]
    [SerializeField] private TMP_Text artistStatementText;

    /// <summary>
    /// The last successfully parsed configuration.
    /// </summary>
    public ArtworkConfigNew CurrentConfig => currentConfig;

    /// <summary>
    /// All DisplayWall components currently registered with this manager.
    /// </summary>
    public IReadOnlyList<DisplayWall> DisplayWallList => displayWalls;

    /// <summary>
    /// Total number of images expected to be downloaded for the current config.
    /// </summary>
    public int TotalImagesToDownload => totalImagesToDownload;

    /// <summary>
    /// Number of images that have finished downloading (successfully or failed).
    /// </summary>
    public int DownloadedImagesCount => downloadedImagesCount;

    /// <summary>
    /// Normalized download progress in [0,1]. Returns 0 if there are no images.
    /// </summary>
    public float DownloadProgress => totalImagesToDownload <= 0 ? 0f : (float)downloadedImagesCount / totalImagesToDownload;

    /// <summary>
    /// Raised when we have counted how many images need to be downloaded for the current config.
    /// Argument is the total number of images.
    /// </summary>
    public event Action<int> OnImageDownloadStarted;

    /// <summary>
    /// Raised every time an image finishes downloading (successfully or with error).
    /// Arguments are (completedCount, totalCount).
    /// </summary>
    public event Action<int, int> OnImageDownloadProgress;

    /// <summary>
    /// Raised once when all expected images for the current config have finished downloading.
    /// </summary>
    public event Action OnAllImagesDownloaded;

    private void Awake()
    {
        // Use global config URL if apiUrl is not set in Inspector
        if (string.IsNullOrEmpty(apiUrl))
        {
            apiUrl = AppConfig.ExhibitionUrl;
        }

        // In WebGL builds, try to extract exhibition ID from the browser URL
        TryExtractExhibitionIdFromUrl();

        // Populate displayWalls with all DisplayWall components in the scene if none are assigned.
        if (displayWalls == null || displayWalls.Count == 0)
        {
            RefreshDisplayWalls();
        }
    }

    /// <summary>
    /// Attempts to extract the exhibition ID from the browser URL in WebGL builds.
    /// URL format expected: /exhibition/{exhibition-uuid}
    /// Fallback chain:
    /// 1. Try jslib GetExhibitionIdFromUrl()
    /// 2. Try Application.absoluteURL with regex parsing
    /// 3. Use the hardcoded exhibitionId from Inspector
    /// </summary>
    private void TryExtractExhibitionIdFromUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string extractedId = null;

        // Attempt 1: Try jslib method
        try
        {
            extractedId = GetExhibitionIdFromUrl();
            if (!string.IsNullOrEmpty(extractedId))
            {
                Debug.Log($"ArtworkManagerNew: Extracted exhibition ID from jslib: {extractedId}");
                exhibitionId = extractedId;
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ArtworkManagerNew: jslib GetExhibitionIdFromUrl failed: {ex.Message}");
        }

        // Attempt 2: Try Application.absoluteURL
        extractedId = TryParseExhibitionIdFromAbsoluteUrl();
        if (!string.IsNullOrEmpty(extractedId))
        {
            Debug.Log($"ArtworkManagerNew: Extracted exhibition ID from Application.absoluteURL: {extractedId}");
            exhibitionId = extractedId;
            return;
        }

        // Fallback: Use hardcoded exhibitionId
        Debug.Log($"ArtworkManagerNew: Could not extract exhibition ID from URL, using fallback: {exhibitionId}");
#else
        // In Editor, also try Application.absoluteURL for testing (usually empty)
        string extractedId = TryParseExhibitionIdFromAbsoluteUrl();
        if (!string.IsNullOrEmpty(extractedId))
        {
            Debug.Log($"ArtworkManagerNew: Extracted exhibition ID from Application.absoluteURL: {extractedId}");
            exhibitionId = extractedId;
            return;
        }
        Debug.Log($"ArtworkManagerNew: Using configured exhibition ID: {exhibitionId}");
#endif
    }

    /// <summary>
    /// Parses the exhibition ID from Application.absoluteURL using regex.
    /// Returns null if parsing fails or URL doesn't contain exhibition ID.
    /// </summary>
    private string TryParseExhibitionIdFromAbsoluteUrl()
    {
        try
        {
            string absoluteUrl = Application.absoluteURL;
            if (string.IsNullOrEmpty(absoluteUrl))
            {
                Debug.Log("ArtworkManagerNew: Application.absoluteURL is empty.");
                return null;
            }

            Debug.Log($"ArtworkManagerNew: Application.absoluteURL = {absoluteUrl}");

            // Match /exhibition/{uuid} pattern
            var match = System.Text.RegularExpressions.Regex.Match(
                absoluteUrl,
                @"/exhibition/([a-zA-Z0-9-]+)"
            );

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ArtworkManagerNew: Failed to parse Application.absoluteURL: {ex.Message}");
        }

        return null;
    }

    private void Start()
    {
        if (loadOnAwake)
        {
            StartCoroutine(AutoReloadAfterDelay());
        }
    }

    private IEnumerator AutoReloadAfterDelay()
    {
        if (autoSetupDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(autoSetupDelaySeconds);
        }

        Reload();
    }

    /// <summary>
    /// Scans the current scene and populates the list of DisplayWall components.
    /// Call this if you dynamically add/remove walls at runtime.
    /// </summary>
    public void RefreshDisplayWalls()
    {
        displayWalls = new List<DisplayWall>(FindObjectsOfType<DisplayWall>());
    }
    /// <summary>
    /// Public entry point to reload data, from either Resources JSON or API depending on <see cref="useAPI"/>.
    /// </summary>
    public void Reload()
    {
        if (isLoading)
        {
            Debug.LogWarning("ArtworkManagerNew: Already loading, ignoring Reload() call.");
            return;
        }

        if (useAPI)
        {
            StartCoroutine(LoadFromAPI());
        }
        else
        {
            LoadFromResources();
        }
    }

    /// <summary>
    /// Explicitly starts loading from the API regardless of the useAPI flag.
    /// Can be wired to a UI button to force API loading.
    /// </summary>
    public void LoadFromAPIButton()
    {
        if (isLoading)
        {
            Debug.LogWarning("ArtworkManagerNew: Already loading, ignoring LoadFromAPIButton() call.");
            return;
        }

        StartCoroutine(LoadFromAPI());
    }

    #region Loading from Resources

    /// <summary>
    /// Loads the JSON text from a Resources folder and parses it into <see cref="ArtworkConfigNew"/>.
    /// </summary>
    private void LoadFromResources()
    {
        isLoading = true;
        lastLoadSucceeded = false;

        if (string.IsNullOrEmpty(resourcesJsonPath))
        {
            Debug.LogError("ArtworkManagerNew: resourcesJsonPath is empty. Set it to e.g. 'ArtworkConfig_New'.");
            isLoading = false;
            return;
        }

        // Resources.Load expects a path without extension
        string pathWithoutExtension = resourcesJsonPath.Replace(".json", string.Empty);
        TextAsset jsonAsset = Resources.Load<TextAsset>(pathWithoutExtension);

        if (jsonAsset == null)
        {
            Debug.LogError($"ArtworkManagerNew: Could not find JSON at Resources path '{pathWithoutExtension}'. Make sure ArtworkConfig_New.json is inside a Resources folder.");
            isLoading = false;
            return;
        }

        ParseAndStoreConfig(jsonAsset.text, sourceDescription: $"Resources/{pathWithoutExtension}.json");

        isLoading = false;
    }

    #endregion

    #region Loading from API

    /// <summary>
    /// Loads the JSON from a remote API endpoint and parses it into <see cref="ArtworkConfigNew"/>.
    /// </summary>
    private IEnumerator LoadFromAPI()
    {
        isLoading = true;
        lastLoadSucceeded = false;

        if (string.IsNullOrEmpty(apiUrl))
        {
            Debug.LogError("ArtworkManagerNew: API URL is not set. Cannot load data from API.");
            isLoading = false;
            yield break;
        }

        if (string.IsNullOrEmpty(exhibitionId))
        {
            Debug.LogError("ArtworkManagerNew: exhibitionId is not set. Cannot load exhibition from API.");
            isLoading = false;
            yield break;
        }

        Debug.Log($"ArtworkManagerNew: Loading exhibition data from API: {apiUrl} with id={exhibitionId}");

        // Build JSON body expected by the endpoint: { "id": "<uuid>" }
        string requestBody = JsonConvert.SerializeObject(new { id = exhibitionId });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Add token header if provided
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("token", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI2OTgzNDM4MDU5ZDlmNmI1YzU4YjhmNzEiLCJ1dWlkIjoiYjA2OTRlOGUtZWE3Zi00NjgyLThlZjMtNzgyZDJlZDdhMTFkIiwiZXhwIjoxNzcwOTAwNjgyLCJpYXQiOjE3NzAyOTU4ODIsIm5iZiI6MTc3MDI5NTg4Mn0.DD2cS-NCVtMs2FHbU9qag9JCnwLqIZnE0JoTC2mXYH0");
            }

            // Apply custom headers if any (e.g. Authorization)
            if (apiHeaders != null)
            {
                foreach (var header in apiHeaders)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            request.timeout = Mathf.CeilToInt(apiTimeoutSeconds);

            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"ArtworkManagerNew: API request failed: {request.error}");
                isLoading = false;
                yield break;
            }

            string json = request.downloadHandler.text;
            ParseAndStoreConfig(json, sourceDescription: $"API: {apiUrl}");
        }

        isLoading = false;
    }

    /// <summary>
    /// Add or update a custom HTTP header for API requests (e.g. Authorization).
    /// </summary>
    public void AddAPIHeader(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (apiHeaders == null) apiHeaders = new Dictionary<string, string>();
        apiHeaders[key] = value;
    }

    /// <summary>
    /// Remove a previously added HTTP header.
    /// </summary>
    public void RemoveAPIHeader(string key)
    {
        if (apiHeaders == null || string.IsNullOrEmpty(key)) return;
        apiHeaders.Remove(key);
    }

    /// <summary>
    /// Change the API URL at runtime. Optionally trigger an immediate reload.
    /// </summary>
    public void SetAPIUrl(string url, bool reloadImmediately = false)
    {
        apiUrl = url;
        if (reloadImmediately)
        {
            Reload();
        }
    }

    #endregion

    #region Parsing & Accessors

    /// <summary>
    /// Shared JSON parsing routine for both Resources and API sources.
    /// After successfully parsing and storing the config, this will automatically
    /// kick off a sequential coroutine that instantiates layouts and populates
    /// them with images, one exhibition at a time.
    /// </summary>
    private void ParseAndStoreConfig(string json, string sourceDescription)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError($"ArtworkManagerNew: Received empty JSON from {sourceDescription}.");
            return;
        }

        try
        {
            Debug.Log(json);

            // Parse JSON and detect if "data" is an object or array
            JObject root = JObject.Parse(json);
            ArtworkConfigNew parsed = new ArtworkConfigNew();
            parsed.success = root["success"]?.ToObject<bool>() ?? false;

            // Check if "data" is an array or a single object
            JToken dataToken = root["data"];
            if (dataToken == null)
            {
                Debug.LogError($"ArtworkManagerNew: No 'data' field found in JSON from {sourceDescription}.");
                return;
            }

            if (dataToken.Type == JTokenType.Array)
            {
                // Data is an array of exhibitions (e.g. from Resources JSON)
                parsed.data = dataToken.ToObject<List<ExhibitionConfigNew>>();
            }
            else if (dataToken.Type == JTokenType.Object)
            {
                // Data is a single exhibition object (e.g. from API)
                var singleExhibition = dataToken.ToObject<ExhibitionConfigNew>();
                parsed.data = new List<ExhibitionConfigNew> { singleExhibition };
            }
            else
            {
                Debug.LogError($"ArtworkManagerNew: Unexpected 'data' type in JSON from {sourceDescription}. Expected object or array.");
                return;
            }

            if (parsed == null || parsed.data == null)
            {
                Debug.LogError($"ArtworkManagerNew: Failed to parse JSON from {sourceDescription} into ArtworkConfigNew.");
                return;
            }

            currentConfig = parsed;
            lastLoadSucceeded = true;

            // As soon as we have valid config, try to start exhibition music (if any "music" field is present).
            TryStartExhibitionMusic();

            // Apply exhibition video and preview image to their respective components.
            TryApplyExhibitionVideo();
            TryApplyExhibitionPreviewImage();

            // Apply artist info to text components.
            TryApplyArtistInfo();

            // Count how many images we expect to download for this config so UI can track progress.
            totalImagesToDownload = CountImagesToDownload();
            downloadedImagesCount = 0;
            successfulDownloads = 0;
            failedDownloads = 0;
            OnImageDownloadStarted?.Invoke(totalImagesToDownload);

            int exhibitionCount = currentConfig.data != null ? currentConfig.data.Count : 0;
            int totalPaintings = GetAllPaintings().Count;

            Debug.Log($"ArtworkManagerNew: Loaded config from {sourceDescription}. success={currentConfig.success}, exhibitions={exhibitionCount}, paintings={totalPaintings}, imagesToDownload={totalImagesToDownload}.");

            // Automatically build layouts and populate them with images sequentially
            StartCoroutine(BuildLayoutsAndImagesSequentially());
        }
        catch (Exception ex)
        {
            Debug.LogError($"ArtworkManagerNew: Exception while parsing JSON from {sourceDescription}: {ex.Message}\\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Looks for a non-empty music URL in the current exhibition config and, if found,
    /// downloads and plays it through an AudioSource.
    /// </summary>
    private void TryStartExhibitionMusic()
    {
        string musicUrl = FindFirstMusicUrlFromConfig();
        if (string.IsNullOrEmpty(musicUrl))
        {
            Debug.Log("ArtworkManagerNew: No music field found in exhibition data.");
            return;
        }

        // Stop currently playing music (if any) before starting new one.
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }

        StartCoroutine(LoadAndPlayMusicFromUrl(musicUrl));
    }

    /// <summary>
    /// Scans all exhibitions in the current config and returns the first non-empty
    /// music URL it finds. This assumes the API returns a string field named "music"
    /// on each exhibition object.
    /// </summary>
    private string FindFirstMusicUrlFromConfig()
    {
        if (currentConfig?.data == null)
        {
            return null;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(exhibition.music))
            {
                return exhibition.music;
            }
        }

        return null;
    }

    /// <summary>
    /// Downloads an audio clip from the given URL and plays it using an AudioSource.
    /// The clip is stored in <see cref="currentMusicClip"/> so it can be reused if needed.
    /// </summary>
    private IEnumerator LoadAndPlayMusicFromUrl(string musicUrl)
    {
        if (string.IsNullOrEmpty(musicUrl))
        {
            yield break;
        }

        // Ensure we have an AudioSource to play music on.
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.GetComponent<AudioSource>();
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Basic heuristic: if the URL ends with .wav use WAV, otherwise assume MP3.
        AudioType audioType = AudioType.MPEG;
        string lowerUrl = musicUrl.ToLowerInvariant();
        if (lowerUrl.EndsWith(".wav"))
        {
            audioType = AudioType.WAV;
        }

        Debug.Log($"ArtworkManagerNew: Loading exhibition music from URL: {musicUrl}");

        // Check cache first to avoid re-downloading the same music clip.
        if (WebGLMediaCache.TryGetAudioClip(musicUrl, out var cachedClip))
        {
            currentMusicClip = cachedClip;
        }
        else
        {
            using (UnityWebRequest musicRequest = UnityWebRequestMultimedia.GetAudioClip(musicUrl, audioType))
            {
                musicRequest.timeout = Mathf.CeilToInt(apiTimeoutSeconds);
                yield return musicRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (musicRequest.result != UnityWebRequest.Result.Success)
#else
                    if (musicRequest.isNetworkError || musicRequest.isHttpError)
#endif
                {
                    Debug.LogError($"ArtworkManagerNew: Failed to load music from URL: {musicUrl}. Error: {musicRequest.error}");
                    yield break;
                }

                currentMusicClip = DownloadHandlerAudioClip.GetContent(musicRequest);
                WebGLMediaCache.StoreAudioClip(musicUrl, currentMusicClip);
            }
        }

        if (currentMusicClip == null)
        {
            Debug.LogError("ArtworkManagerNew: Music request succeeded but returned null AudioClip.");
            yield break;
        }

        musicAudioSource.clip = currentMusicClip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    /// <summary>
    /// Toggles the exhibition music on/off.
    /// - If music is currently playing, it will be stopped.
    /// - If music is not playing, it will either resume the existing clip or
    ///   (if none is loaded yet) start loading/playing from the exhibition's music URL.
    /// </summary>
    public void ToggleExhibitionMusic()
    {
        // Ensure we have an AudioSource reference if one already exists on this GameObject.
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.GetComponent<AudioSource>();
        }

        // Case 1: We already have an AudioSource and a clip assigned.
        if (musicAudioSource != null && musicAudioSource.clip != null)
        {
            if (musicAudioSource.isPlaying)
            {
                // Stop current playback.
                musicAudioSource.Stop();
            }
            else
            {
                // Start (or restart) playback from the beginning.
                musicAudioSource.Play();
            }
            return;
        }

        // Case 2: No clip loaded yet, but we have config data. Attempt to load
        // and start playing from the exhibition's music URL.
        if (currentConfig != null)
        {
            TryStartExhibitionMusic();
        }
    }

    /// <summary>
    /// Applies the exhibition video URL to the VideoPlayer component if available.
    /// </summary>
    private void TryApplyExhibitionVideo()
    {
        if (exhibitionVideoPlayer == null)
        {
            Debug.Log("ArtworkManagerNew: No VideoPlayer assigned for exhibition video.");
            return;
        }

        string videoUrl = FindFirstVideoUrlFromConfig();
        if (string.IsNullOrEmpty(videoUrl))
        {
            Debug.Log("ArtworkManagerNew: No video field found in exhibition data.");
            return;
        }

        Debug.Log($"ArtworkManagerNew: Applying exhibition video URL: {videoUrl}");
        exhibitionVideoPlayer.source = VideoSource.Url;
        exhibitionVideoPlayer.url = videoUrl;
        exhibitionVideoPlayer.Play();
    }

    /// <summary>
    /// Finds the first non-empty video URL from the exhibition config.
    /// </summary>
    private string FindFirstVideoUrlFromConfig()
    {
        if (currentConfig?.data == null)
        {
            return null;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.video != null && !string.IsNullOrEmpty(exhibition.video.src))
            {
                return exhibition.video.src;
            }
        }

        return null;
    }

    /// <summary>
    /// Applies the exhibition preview image to the SpriteRenderer component if available.
    /// </summary>
    private void TryApplyExhibitionPreviewImage()
    {
        if (previewImageRenderer == null)
        {
            Debug.Log("ArtworkManagerNew: No SpriteRenderer assigned for exhibition preview image.");
            return;
        }

        string previewImageUrl = FindFirstPreviewImageUrlFromConfig();
        if (string.IsNullOrEmpty(previewImageUrl))
        {
            Debug.Log("ArtworkManagerNew: No preview image field found in exhibition data.");
            return;
        }

        StartCoroutine(LoadPreviewImageFromUrl(previewImageUrl));
    }

    /// <summary>
    /// Finds the first non-empty preview image URL from the exhibition config.
    /// </summary>
    private string FindFirstPreviewImageUrlFromConfig()
    {
        if (currentConfig?.data == null)
        {
            return null;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.previewImage != null && !string.IsNullOrEmpty(exhibition.previewImage.src))
            {
                return exhibition.previewImage.src;
            }
        }

        return null;
    }

    /// <summary>
    /// Downloads the preview image from the given URL and applies it to the SpriteRenderer.
    /// </summary>
    private IEnumerator LoadPreviewImageFromUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            yield break;
        }

        Debug.Log($"ArtworkManagerNew: Loading exhibition preview image from URL: {imageUrl}");

        // Check cache first.
        if (WebGLMediaCache.TryGetTexture(imageUrl, out var cachedTexture))
        {
            ApplyTextureToSpriteRenderer(cachedTexture);
            yield break;
        }

        using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            imageRequest.timeout = Mathf.CeilToInt(apiTimeoutSeconds);
            yield return imageRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (imageRequest.result != UnityWebRequest.Result.Success)
#else
            if (imageRequest.isNetworkError || imageRequest.isHttpError)
#endif
            {
                Debug.LogError($"ArtworkManagerNew: Failed to load preview image from URL: {imageUrl}. Error: {imageRequest.error}");
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
            if (texture != null)
            {
                WebGLMediaCache.StoreTexture(imageUrl, texture);
                ApplyTextureToSpriteRenderer(texture);
            }
        }
    }

    /// <summary>
    /// Converts a Texture2D to a Sprite and applies it to the preview image SpriteRenderer.
    /// Calculates pixels-per-unit to preserve the original size if a sprite already exists.
    /// </summary>
    private void ApplyTextureToSpriteRenderer(Texture2D texture)
    {
        if (previewImageRenderer == null || texture == null)
        {
            return;
        }

        // Default pixels-per-unit.
        float pixelsPerUnit = 100f;

        // If there's an existing sprite, calculate PPU to match the current world size.
        if (previewImageRenderer.sprite != null)
        {
            Vector2 currentWorldSize = previewImageRenderer.bounds.size;
            Vector3 scale = previewImageRenderer.transform.lossyScale;

            if (currentWorldSize.x > 0 && currentWorldSize.y > 0)
            {
                // Calculate PPU so the new texture fits the same world size.
                // Use the larger dimension to ensure it fits within bounds.
                float ppuForWidth = texture.width / (currentWorldSize.x / Mathf.Abs(scale.x)) * Mathf.Abs(scale.x);
                float ppuForHeight = texture.height / (currentWorldSize.y / Mathf.Abs(scale.y)) * Mathf.Abs(scale.y);
                pixelsPerUnit = Mathf.Max(ppuForWidth, ppuForHeight);
            }
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        previewImageRenderer.sprite = sprite;

        Debug.Log($"ArtworkManagerNew: Applied preview image to SpriteRenderer ({texture.width}x{texture.height}, PPU: {pixelsPerUnit})");
    }

    /// <summary>
    /// Applies the artist's full name and statement to the text components if available.
    /// </summary>
    private void TryApplyArtistInfo()
    {
        ArtistConfigNew artist = FindFirstArtistFromConfig();
        if (artist == null)
        {
            Debug.Log("ArtworkManagerNew: No artist info found in exhibition data.");
            return;
        }

        if (artistNameText != null)
        {
            artistNameText.text = artist.fullName ?? string.Empty;
            Debug.Log($"ArtworkManagerNew: Applied artist name: {artist.fullName}");
        }
        else
        {
            Debug.Log("ArtworkManagerNew: No TMP_Text assigned for artist name.");
        }

        if (artistStatementText != null)
        {
            artistStatementText.text = artist.statement ?? string.Empty;
            Debug.Log($"ArtworkManagerNew: Applied artist statement (length: {artist.statement?.Length ?? 0})");
        }
        else
        {
            Debug.Log("ArtworkManagerNew: No TMP_Text assigned for artist statement.");
        }
    }

    /// <summary>
    /// Finds the first artist from the exhibition config.
    /// </summary>
    private ArtistConfigNew FindFirstArtistFromConfig()
    {
        if (currentConfig?.data == null)
        {
            return null;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.artist != null && exhibition.artist.Count > 0)
            {
                return exhibition.artist[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all exhibitions from the current config. Never returns null (returns empty list instead).
    /// </summary>
    public List<ExhibitionConfigNew> GetExhibitions()
    {
        if (currentConfig?.data == null)
        {
            return new List<ExhibitionConfigNew>();
        }
        return new List<ExhibitionConfigNew>(currentConfig.data);
    }

    /// <summary>
    /// Returns all paintings placed on all walls across all exhibitions.
    /// </summary>
    public List<PaintingConfigNew> GetAllPaintings()
    {
        List<PaintingConfigNew> result = new List<PaintingConfigNew>();

        if (currentConfig?.data == null)
            return result;

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.walls == null)
                continue;

            foreach (var wall in exhibition.walls)
            {
                if (wall?.paintings == null)
                    continue;

                result.AddRange(wall.paintings);
            }
        }

        return result;
    }

    /// <summary>
    /// Counts how many images we will attempt to download based on the currentConfig.
    /// Only HTTP(S) mainImage URLs are counted (matching LoadTextureIntoFrame's conditions).
    /// </summary>
    private int CountImagesToDownload()
    {
        if (currentConfig?.data == null)
            return 0;

        int count = 0;
        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.walls == null)
                continue;

            foreach (var wall in exhibition.walls)
            {
                if (wall?.paintings == null)
                    continue;

                foreach (var painting in wall.paintings)
                {
                    string url = painting?.mainImage != null ? painting.mainImage.src : null;
                    if (string.IsNullOrEmpty(url))
                        continue;

                    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Returns all paintings that belong to a specific wallId across all exhibitions.
    /// </summary>
    public List<PaintingConfigNew> GetPaintingsForWall(string wallId)
    {
        List<PaintingConfigNew> result = new List<PaintingConfigNew>();
        if (string.IsNullOrEmpty(wallId) || currentConfig?.data == null)
            return result;

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.walls == null)
                continue;

            foreach (var wall in exhibition.walls)
            {
                if (wall == null)
                    continue;

                if (!string.Equals(wall.wallId, wallId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (wall.paintings != null)
                {
                    result.AddRange(wall.paintings);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the first exhibition that matches the given wallId (useful when you know a wall belongs to a single exhibition).
    /// </summary>
    public ExhibitionConfigNew GetExhibitionForWall(string wallId)
    {
        if (string.IsNullOrEmpty(wallId) || currentConfig?.data == null)
            return null;

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition?.walls == null)
                continue;

            foreach (var wall in exhibition.walls)
            {
                if (wall == null)
                    continue;

                if (string.Equals(wall.wallId, wallId, StringComparison.OrdinalIgnoreCase))
                {
                    return exhibition;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a numeric displayWallId from a JSON wallId string like "WALL_14".
    /// Returns -1 if parsing fails.
    /// </summary>
    public int ParseDisplayWallIdFromWallId(string wallId)
    {
        if (string.IsNullOrEmpty(wallId))
            return -1;

        const string prefix = "WALL_";
        string numericPart = wallId.Trim();

        if (numericPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            numericPart = numericPart.Substring(prefix.Length);
        }

        if (int.TryParse(numericPart, out int id))
        {
            return id;
        }

        Debug.LogWarning($"ArtworkManagerNew: Could not parse displayWallId from wallId '{wallId}'. Expected format like 'WALL_14'.");
        return -1;
    }

    /// <summary>
    /// Parses the last continuous numeric sequence in a string, e.g. "layout_28" -> 28, "3" -> 3.
    /// Returns -1 if no digits are found.
    /// </summary>
    private int ParseNumericSuffix(string value)
    {
        if (string.IsNullOrEmpty(value))
            return -1;

        int i = value.Length - 1;
        // Skip non-digits from the end
        while (i >= 0 && !char.IsDigit(value[i]))
        {
            i--;
        }

        if (i < 0)
            return -1;

        int end = i;
        // Move left while digits
        while (i >= 0 && char.IsDigit(value[i]))
        {
            i--;
        }

        string numericPart = value.Substring(i + 1, end - i);
        return int.TryParse(numericPart, out int result) ? result : -1;
    }

    /// <summary>
    /// Returns the DisplayWall component that has the given displayWallId (e.g. 14 for WALL_14).
    /// </summary>
    public DisplayWall GetDisplayWallById(int displayWallId)
    {
        if (displayWallId < 0)
            return null;

        if (displayWalls == null || displayWalls.Count == 0)
        {
            RefreshDisplayWalls();
        }

        foreach (var wall in displayWalls)
        {
            if (wall != null && wall.displayWallId == displayWallId)
            {
                return wall;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the DisplayWall that corresponds to a JSON wallId string (e.g. "WALL_14").
    /// </summary>
    public DisplayWall GetDisplayWallForWallId(string wallId)
    {
        int id = ParseDisplayWallIdFromWallId(wallId);
        if (id < 0)
            return null;

        return GetDisplayWallById(id);
    }

    /// <summary>
    /// Returns the FrameLayout prefab whose numeric LayoutId matches the numeric part of the JSON layoutId.
    /// Example: JSON "layout_3" or "3" will match a FrameLayout whose LayoutId == 3.
    /// </summary>
    public FrameLayout GetLayoutPrefabById(string layoutId)
    {
        if (string.IsNullOrEmpty(layoutId) || layoutPrefabs == null)
            return null;

        int numericId = ParseNumericSuffix(layoutId);
        if (numericId < 0)
        {
            Debug.LogWarning($"ArtworkManagerNew: Could not parse numeric layout index from layoutId '{layoutId}'.");
            return null;
        }

        foreach (var layout in layoutPrefabs)
        {
            if (layout != null && layout.LayoutId == numericId)
            {
                return layout;
            }
        }

        Debug.LogWarning($"ArtworkManagerNew: No FrameLayout prefab in layoutPrefabs with LayoutId {numericId} for JSON layoutId '{layoutId}'.");
        return null;
    }

    /// <summary>
    /// Instantiates the FrameLayout prefab for the given layoutId on the DisplayWall that matches wallId.
    /// Parent is set to the DisplayWall's transform, with zeroed local position/rotation.
    /// If the wall's UseSlots is true and wallConfig provides startSlot/slotSpan, the layout will be
    /// positioned at the center of those slots.
    /// Returns the instantiated FrameLayout, or null if anything is missing.
    ///
    /// After instantiation, all ArtworkFrame children in the layout have their local Z position
    /// normalized so that any prefab-authored per-frame Z offsets are removed. This keeps the
    /// depth of the artwork relative to the wall controlled by the wall/layout, not individual frames.
    /// </summary>
    public FrameLayout InstantiateLayoutOnWall(string layoutId, string wallId)
    {
        var layoutPrefab = GetLayoutPrefabById(layoutId);
        if (layoutPrefab == null)
        {
            Debug.LogWarning($"ArtworkManagerNew: No FrameLayout prefab found for layoutId '{layoutId}'.");
            return null;
        }

        var wall = GetDisplayWallForWallId(wallId);
        if (wall == null)
        {
            Debug.LogWarning($"ArtworkManagerNew: No DisplayWall found for wallId '{wallId}'.");
            return null;
        }

        FrameLayout instance = Instantiate(layoutPrefab, wall.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        // Ensure its internal frame list is up-to-date.
        instance.RefreshFrames();

        // Normalize per-frame local Z so all frames sit on the same depth plane
        ZeroChildFrameLocalZ(instance);

        return instance;
    }

    /// <summary>
    /// Ensures all ArtworkFrame children of a layout sit at local Z = 0 relative to the
    /// layout root, regardless of prefab-authored offsets.
    /// </summary>
    private void ZeroChildFrameLocalZ(FrameLayout layout)
    {
        if (layout == null)
            return;

        var frames = layout.Frames;
        if (frames == null)
            return;

        foreach (var frame in frames)
        {
            if (frame == null)
                continue;

            Transform t = frame.transform;
            Vector3 lp = t.localPosition;

            // Keep X/Y placement from the prefab, but clear any Z offset
            t.localPosition = new Vector3(lp.x, lp.y, 0f);
        }
    }

    /// <summary>
    /// Positions a layout instance based on slot configuration if the wall uses slots.
    /// If UseSlots is true, the layout will be positioned at the center of the slots
    /// defined by startSlot and slotSpan.
    /// </summary>
    private void ApplySlotBasedPositioning(FrameLayout layoutInstance, DisplayWall wall, WallsConfigNew wallConfig)
    {
        if (layoutInstance == null || wall == null || wallConfig == null)
            return;

        if (!wall.UseSlots)
        {
            // Slot-based positioning is disabled, keep default position
            return;
        }

        // Calculate the center position based on startSlot and slotSpan
        Vector3 slotCenter = wall.CalculateSlotCenterPosition(wallConfig.startSlot, wallConfig.slotSpan);
        if (slotCenter == Vector3.zero)
        {
            Debug.LogWarning($"ArtworkManagerNew: Could not calculate slot center for wall '{wall.name}' with startSlot={wallConfig.startSlot}, slotSpan={wallConfig.slotSpan}. Using default position.");
            return;
        }

        // Set the layout's world position to the calculated slot center
        layoutInstance.transform.position = slotCenter;

        Debug.Log($"ArtworkManagerNew: Positioned layout '{layoutInstance.name}' on wall '{wall.name}' at slot center (startSlot={wallConfig.startSlot}, slotSpan={wallConfig.slotSpan}, position={slotCenter})");
    }

    /// <summary>
    /// Convenience helper: given an ExhibitionConfigNew entry, instantiate FrameLayout
    /// instances for all of its walls using each wall's layoutId and wallId.
    /// Returns the first successfully instantiated FrameLayout (or null if none succeed).
    /// </summary>
    public FrameLayout InstantiateLayoutForExhibition(ExhibitionConfigNew exhibition)
    {
        if (exhibition == null || exhibition.walls == null || exhibition.walls.Count == 0)
        {
            Debug.LogWarning("ArtworkManagerNew: InstantiateLayoutForExhibition called with null or empty walls.");
            return null;
        }

        FrameLayout firstInstance = null;

        foreach (var wallConfig in exhibition.walls)
        {
            if (wallConfig == null)
                continue;

            var layoutInstance = InstantiateLayoutOnWall(wallConfig.layoutId, wallConfig.wallId);
            if (layoutInstance != null)
            {
                // Apply slot-based positioning if the wall uses slots
                var wall = GetDisplayWallForWallId(wallConfig.wallId);
                if (wall != null)
                {
                    ApplySlotBasedPositioning(layoutInstance, wall, wallConfig);
                }

                if (firstInstance == null)
                {
                    firstInstance = layoutInstance;
                }
            }
        }

        return firstInstance;
    }

    /// <summary>
    /// Instantiates layouts for all exhibitions in the current JSON config.
    /// Uses each wall's layoutId and wallId to pick the appropriate FrameLayout prefab
    /// and spawn it under the right DisplayWall. This does NOT load any images; call
    /// PopulateLayoutsWithImagesFromJson() after this if you want to fill the frames.
    /// </summary>
    public void InstantiateLayoutsFromJson()
    {
        if (currentConfig?.data == null || currentConfig.data.Count == 0)
        {
            Debug.LogWarning("ArtworkManagerNew: InstantiateLayoutsFromJson called but currentConfig has no data.");
            return;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition == null || exhibition.walls == null)
                continue;

            foreach (var wallConfig in exhibition.walls)
            {
                if (wallConfig == null)
                    continue;

                var layoutId = wallConfig.layoutId;
                var wallId = wallConfig.wallId;

                var layoutInstance = InstantiateLayoutOnWall(layoutId, wallId);
                if (layoutInstance == null)
                {
                    Debug.LogWarning($"ArtworkManagerNew: Failed to instantiate layout '{layoutId}' on wall '{wallId}'.");
                    continue;
                }

                // Apply slot-based positioning if the wall uses slots
                var wall = GetDisplayWallForWallId(wallId);
                if (wall != null)
                {
                    ApplySlotBasedPositioning(layoutInstance, wall, wallConfig);
                }
            }
        }
    }

    /// <summary>
    /// Sequentially instantiates a layout and then applies its images for each
    /// wall of each exhibition in the current config. This avoids kicking off many
    /// image download coroutines at once, which can cause some images to fail.
    /// </summary>
    private IEnumerator BuildLayoutsAndImagesSequentially()
    {
        if (currentConfig?.data == null || currentConfig.data.Count == 0)
        {
            Debug.LogWarning("ArtworkManagerNew: BuildLayoutsAndImagesSequentially called but currentConfig has no data.");
            yield break;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition == null || exhibition.walls == null)
                continue;

            foreach (var wallConfig in exhibition.walls)
            {
                if (wallConfig == null)
                    continue;

                var layoutId = wallConfig.layoutId;
                var wallId = wallConfig.wallId;

                var layoutInstance = InstantiateLayoutOnWall(layoutId, wallId);
                if (layoutInstance == null)
                {
                    Debug.LogWarning($"ArtworkManagerNew: Failed to instantiate layout '{layoutId}' on wall '{wallId}' during sequential build.");
                    continue;
                }

                // Apply slot-based positioning if the wall uses slots
                var wall = GetDisplayWallForWallId(wallId);
                if (wall != null)
                {
                    ApplySlotBasedPositioning(layoutInstance, wall, wallConfig);
                }

                // Apply images for this one layout before moving to the next
                yield return ApplyPaintingsToLayout(wallConfig, layoutInstance);

                if (layoutBuildDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(layoutBuildDelaySeconds);
                }
            }
        }
    }

    /// <summary>
    /// For all exhibitions in the current JSON config, finds the instantiated FrameLayout
    /// under the appropriate DisplayWall for each wall and downloads mainImage.src for
    /// each painting into the corresponding ArtworkFrame.
    ///
    /// Extra paintings (more paintings than frames) are ignored.
    /// Extra frames (more frames than paintings) are cleared.
    /// </summary>
    public void PopulateLayoutsWithImagesFromJson()
    {
        if (currentConfig?.data == null || currentConfig.data.Count == 0)
        {
            Debug.LogWarning("ArtworkManagerNew: PopulateLayoutsWithImagesFromJson called but currentConfig has no data.");
            return;
        }

        foreach (var exhibition in currentConfig.data)
        {
            if (exhibition == null || exhibition.walls == null)
                continue;

            foreach (var wallConfig in exhibition.walls)
            {
                if (wallConfig == null)
                    continue;

                // Find the wall instance in the scene
                var wall = GetDisplayWallForWallId(wallConfig.wallId);
                if (wall == null)
                {
                    Debug.LogWarning($"ArtworkManagerNew: No DisplayWall found for wallId '{wallConfig.wallId}' when populating images.");
                    continue;
                }

                // Find a FrameLayout instance on this wall whose LayoutId matches the numeric part of JSON layoutId
                int layoutNumericId = ParseNumericSuffix(wallConfig.layoutId);
                if (layoutNumericId < 0)
                {
                    Debug.LogWarning($"ArtworkManagerNew: Could not parse numeric layout index from layoutId '{wallConfig.layoutId}' when populating images.");
                    continue;
                }

                FrameLayout targetLayout = null;
                var layoutsOnWall = wall.GetComponentsInChildren<FrameLayout>(includeInactive: true);
                foreach (var layout in layoutsOnWall)
                {
                    if (layout != null && layout.LayoutId == layoutNumericId)
                    {
                        targetLayout = layout;
                        break;
                    }
                }

                if (targetLayout == null)
                {
                    Debug.LogWarning($"ArtworkManagerNew: No FrameLayout instance with LayoutId {layoutNumericId} found under wall '{wall.name}' when populating images.");
                    continue;
                }

                // Start async loading of textures into this layout's frames
                StartCoroutine(ApplyPaintingsToLayout(wallConfig, targetLayout));
            }
        }
    }

    /// <summary>
    /// For a given wall configuration and instantiated FrameLayout, download and assign
    /// mainImage textures to each ArtworkFrame child. Extra paintings are ignored;
    /// extra frames are left empty (texture cleared).
    /// </summary>
    private IEnumerator ApplyPaintingsToLayout(WallsConfigNew wallConfig, FrameLayout layout)
    {
        if (wallConfig == null || layout == null)
            yield break;

        var paintings = wallConfig.paintings;
        if (paintings == null)
            yield break;

        var frames = layout.Frames;
        int frameCount = frames != null ? frames.Count : 0;
        int paintingCount = paintings.Count;
        int count = Mathf.Min(frameCount, paintingCount);

        // Assign textures and debug data for each matching pair
        for (int i = 0; i < count; i++)
        {
            var frame = frames[i];
            var painting = paintings[i];
            if (frame == null || painting == null)
                continue;

            // Store full painting JSON object on the frame for debugging
            frame.SetDebugPaintingData(painting);

            string url = painting.mainImage != null ? painting.mainImage.src : null;
            if (string.IsNullOrEmpty(url))
            {
                frame.ClearTexture();
                continue;
            }

            yield return StartCoroutine(LoadTextureIntoFrame(url, frame));
        }

        // Clear any remaining frames if there are fewer paintings than frames
        for (int i = count; i < frameCount; i++)
        {
            var frame = frames[i];
            if (frame != null)
            {
                frame.ClearTexture();
                frame.SetDebugPaintingData(null);
            }
        }
    }

    /// <summary>
    /// Flips a texture vertically (used after downloading textures from the API/CDN).
    /// </summary>
    private Texture2D FlipTextureVertically(Texture2D original)
    {
        if (original == null) return null;

        int width = original.width;
        int height = original.height;
        var flipped = new Texture2D(width, height, original.format, false);
        Color[] pixels = original.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcIndex = y * width + x;
                int dstIndex = (height - 1 - y) * width + x;
                flippedPixels[dstIndex] = pixels[srcIndex];
            }
        }

        flipped.SetPixels(flippedPixels);
        flipped.Apply();
        return flipped;
    }

    /// <summary>
    /// Downloads an image from a URL and assigns it to the given ArtworkFrame's texture.
    /// Also updates download progress counters and fires progress events.
    /// Retries failed downloads up to maxRetryAttempts times.
    /// </summary>
    private IEnumerator LoadTextureIntoFrame(string imageUrl, ArtworkFrame frame)
    {
        if (frame == null)
            yield break;

        bool willCountThisImage = !string.IsNullOrEmpty(imageUrl) &&
                                  (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                   imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(imageUrl))
        {
            frame.ClearTexture();
            if (willCountThisImage)
            {
                IncrementDownloadProgress();
            }
            yield break;
        }

        // Only handle HTTP/HTTPS URLs here; non-HTTP can be extended to local Resources if needed
        if (!imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"ArtworkManagerNew: Image URL '{imageUrl}' is not an HTTP(S) URL. Skipping.");
            if (willCountThisImage)
            {
                IncrementDownloadProgress();
            }
            yield break;
        }

        // Check cache first so we don't re-download the same image during this session.
        if (WebGLMediaCache.TryGetTexture(imageUrl, out var cachedTexture))
        {
            frame.SetTexture(cachedTexture);
            Debug.Log($"ArtworkManagerNew: Using cached image for frame '{frame.name}' from URL: {imageUrl}");
            if (willCountThisImage)
            {
                successfulDownloads++;
                IncrementDownloadProgress();
            }
            yield break;
        }

        Debug.Log($"ArtworkManagerNew: Loading image for frame '{frame.name}' from URL: {imageUrl}");

        bool downloadSucceeded = false;
        Texture2D downloadedTexture = null;
        string lastError = "";

        // Retry loop for failed downloads
        for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
        {
            if (attempt > 0)
            {
                Debug.LogWarning($"ArtworkManagerNew: Retry attempt {attempt}/{maxRetryAttempts} for image: {imageUrl} (Previous error: {lastError})");
                yield return new WaitForSeconds(retryDelaySeconds);
            }

            using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                // Use dedicated image download timeout (usually longer than API timeout)
                imageRequest.timeout = Mathf.CeilToInt(imageDownloadTimeoutSeconds);

                yield return imageRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool success = imageRequest.result == UnityWebRequest.Result.Success;
                lastError = imageRequest.result.ToString();
#else
                bool success = !imageRequest.isNetworkError && !imageRequest.isHttpError;
                lastError = imageRequest.error ?? "Unknown error";
#endif

                if (success)
                {
                    downloadedTexture = DownloadHandlerTexture.GetContent(imageRequest);
                    if (downloadedTexture != null && downloadedTexture.width > 0 && downloadedTexture.height > 0)
                    {
                        downloadSucceeded = true;
                        Debug.Log($"ArtworkManagerNew: Successfully downloaded image ({downloadedTexture.width}x{downloadedTexture.height}) for frame '{frame.name}' on attempt {attempt + 1}");
                        break;
                    }
                    else
                    {
                        lastError = $"Invalid texture dimensions: {downloadedTexture?.width ?? 0}x{downloadedTexture?.height ?? 0}";
                        Debug.LogWarning($"ArtworkManagerNew: Downloaded texture is invalid for URL: {imageUrl}. {lastError}");
                    }
                }
                else
                {
                    lastError = imageRequest.error ?? "Unknown error";
                    Debug.LogWarning($"ArtworkManagerNew: Failed to load image (attempt {attempt + 1}/{maxRetryAttempts + 1}) from URL: {imageUrl}. Error: {lastError}");
                }
            }
        }

        if (downloadSucceeded && downloadedTexture != null)
        {
            downloadedTexture = FlipTextureVertically(downloadedTexture);
            WebGLMediaCache.StoreTexture(imageUrl, downloadedTexture);
            frame.SetTexture(downloadedTexture);
            successfulDownloads++;
            Debug.Log($"ArtworkManagerNew: Successfully loaded and applied image for frame '{frame.name}'");
        }
        else
        {
            failedDownloads++;
            Debug.LogError($"ArtworkManagerNew: FAILED to load image after {maxRetryAttempts + 1} attempts from URL: {imageUrl}\nFrame: {frame.name}\nLast Error: {lastError}\nThis frame will remain blank.");
            frame.ClearTexture();
        }

        if (willCountThisImage)
        {
            IncrementDownloadProgress();
        }
    }

    /// <summary>
    /// Increments the downloaded image counter and fires progress / completion events.
    /// </summary>
    private void IncrementDownloadProgress()
    {
        downloadedImagesCount = Mathf.Clamp(downloadedImagesCount + 1, 0, Mathf.Max(downloadedImagesCount + 1, totalImagesToDownload));
        OnImageDownloadProgress?.Invoke(downloadedImagesCount, totalImagesToDownload);

        if (totalImagesToDownload > 0 && downloadedImagesCount >= totalImagesToDownload)
        {
            // Log summary of download results
            Debug.Log($"=== ArtworkManagerNew: Image Download Complete ===");
            Debug.Log($"Total images: {totalImagesToDownload}");
            Debug.Log($"Successful: {successfulDownloads}");
            Debug.Log($"Failed: {failedDownloads}");
            Debug.Log($"Success rate: {(totalImagesToDownload > 0 ? (float)successfulDownloads / totalImagesToDownload * 100f : 0f):F1}%");
            
            if (failedDownloads > 0)
            {
                Debug.LogWarning($"ArtworkManagerNew: {failedDownloads} image(s) failed to download. Check the logs above for specific URLs and errors.");
            }
            
            OnAllImagesDownloaded?.Invoke();
        }
    }

    /// <summary>
    /// Editor helper so you can right-click the ArtworkManagerNew component and run the
    /// layout instantiation manually while in Play mode.
    /// </summary>
    [ContextMenu("Instantiate Layouts From JSON")]
    private void EditorInstantiateLayoutsFromJson()
    {
        InstantiateLayoutsFromJson();
    }

    /// <summary>
    /// Editor helper so you can right-click the ArtworkManagerNew component and populate
    /// all instantiated layouts with images from the current JSON while in play mode.
    /// </summary>
    [ContextMenu("Populate Layout Images From JSON")]
    private void EditorPopulateLayoutImagesFromJson()
    {
        PopulateLayoutsWithImagesFromJson();
    }

    #endregion
}

#region New JSON data model (ArtworkConfig_New)

/// <summary>
/// Root of the new exhibition JSON (matches ArtworkConfig_New.json).
/// </summary>
[Serializable]
public class ArtworkConfigNew
{
    public bool success;
    public List<ExhibitionConfigNew> data;
}

/// <summary>
/// One exhibition / configuration entry inside the JSON "data" array.
/// </summary>
[Serializable]
public class ExhibitionConfigNew
{
    public string _id;
    public string uuid;
    public string name;
    public string description;

    public string slug;
    public string status;

    // New fields from updated API structure
    public string userId;
    public string createdAt;
    public string updatedAt;

    /// <summary>
    /// Optional preview image for the exhibition (cover/thumbnail).
    /// Matches the "previewImage" object from the API.
    /// </summary>
    public ImageConfigNew previewImage;

    /// <summary>
    /// Optional video associated with the exhibition (e.g. intro or walkthrough).
    /// Matches the "video" object from the API.
    /// </summary>
    public ImageConfigNew video;

    /// <summary>
    /// Optional music clip URL associated with this exhibition. This is expected to be
    /// provided by the getExhibitionFromId API under the field name "music".
    /// </summary>
    public string music;

    /// <summary>
    /// Optional list of artist profiles attached to this exhibition.
    /// </summary>
    public List<ArtistConfigNew> artist;

    /// <summary>
    /// One or more walls that belong to this exhibition.
    /// </summary>
    public List<WallsConfigNew> walls;
}

/// <summary>
/// Artist metadata returned with an exhibition.
/// </summary>
[Serializable]
public class ArtistConfigNew
{
    public string _id;
    public string uuid;
    public string fullName;
    public string email;
    public string phone;
    public string location;
    public string instagramOrWebsite;
    public string portfolioLink;
    public List<string> mediums;
    public List<string> styles;
    public string statement;
    public string heardFrom;
    public string awards;
    public List<string> additionalLinks;
    public bool reviewed;
    public string role;
    public string status;
    public string createdAt;
    public string updatedAt;
}

/// <summary>
/// Wall configuration for an exhibition.
/// </summary>
[Serializable]
public class WallsConfigNew
{
    /// <summary>
    /// Identifier of the target DisplayWall, e.g. "WALL_17".
    /// </summary>
    public string wallId;

    /// <summary>
    /// Layout identifier, e.g. "layout_6".
    /// </summary>
    public string layoutId;

    /// <summary>
    /// Slot index on the gallery grid this wall starts at (as provided by the API).
    /// Currently used only for debugging / layout mapping.
    /// </summary>
    public int startSlot;

    /// <summary>
    /// Number of slots this wall spans on the gallery grid.
    /// </summary>
    public int slotSpan;

    /// <summary>
    /// All paintings assigned to this wall.
    /// </summary>
    public List<PaintingConfigNew> paintings;
}

/// <summary>
/// A single painting / product entry on a wall.
/// Only a subset of fields from the JSON is modeled here; add more as needed.
/// </summary>
[Serializable]
public class PaintingConfigNew
{
    public long image_number;
    public string name;
    public string shortDescription;
    public string description;
    public string slug;
    public string uuid;
    public string ratio;
    public string category;
    public string subCategory;
    public List<string> collectionName;
    public List<TagConfigNew> tags;
    public List<string> regions;
    public string status;
    public List<string> style;
    public List<string> colors;
    public string medium;
    public string baseSKU;
    public List<PriceConfigNew> price;
    public List<ImageConfigNew> images;
    public List<FrameConfigNew> frames;
    public ImageConfigNew mainImage;
    public bool sold;
    public bool favourite;
    public bool cart;
    public bool priceOnRequest;
}

/// <summary>
/// Tag information (e.g. { "name": "canvas", "exist": true }).
/// </summary>
[Serializable]
public class TagConfigNew
{
    public string name;

    /// <summary>
    /// True when the tag exists in the master tag set (API field "exist").
    /// </summary>
    public bool exist;

    /// <summary>
    /// Optional internal identifier, kept for backward compatibility if the API ever
    /// provides an "id" field for tags.
    /// </summary>
    public string id;
}

/// <summary>
/// Region-specific price configuration for a given size.
/// </summary>
[Serializable]
public class PriceConfigNew
{
    public RegionConfigNew region;
    public string size;
    public float price;
    public string sku;
    public long stock;
    public bool maintainStock;
    public string _id;
    public float finalPrice;
}

/// <summary>
/// Region metadata inside each price entry.
/// </summary>
[Serializable]
public class RegionConfigNew
{
    public string id;         // optional in some entries
    public string name;       // e.g. "United States"
    public string currency;   // e.g. "USD"
    public string countryCode; // e.g. "US"
}

/// <summary>
/// Image or mainImage entry (gallery images and hero image share the same structure).
/// </summary>
[Serializable]
public class ImageConfigNew
{
    public string image_id;
    public string src;
    public string alt;
    public string key;
    public string _id; // present on some entries
}

/// <summary>
/// Placeholder for future frame configuration (frames is currently an empty array in the JSON).
/// Add properties here if/when the API starts returning frame data.
/// </summary>
[Serializable]
public class FrameConfigNew
{
    // Intentionally left minimal for now
}

#endregion
