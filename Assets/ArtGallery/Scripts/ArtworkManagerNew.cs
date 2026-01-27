using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

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
    [Header("Data Source")]
    [Tooltip("If true, load JSON from a remote API instead of a local Resources JSON file.")]
    [SerializeField] private bool useAPI = false;

    [Tooltip("Resources path (without .json extension) to the local ArtworkConfig_New file.")]
    [SerializeField] private string resourcesJsonPath = "ArtworkConfig_New"; // Resources/ArtworkConfig_New.json

    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "https://stg.artiure.com/api/artist/exhibition/getExhibitionFromId";

    [Tooltip("Exhibition UUID to request from the API. For now this can be hard-coded; later it can come from user selection.")]
    [SerializeField] private string exhibitionId = "e459a070-3f67-4624-8d33-4dc33d1d6af0";

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
        // Populate displayWalls with all DisplayWall components in the scene if none are assigned.
        if (displayWalls == null || displayWalls.Count == 0)
        {
            RefreshDisplayWalls();
        }
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
            var parsed = JsonConvert.DeserializeObject<ArtworkConfigNew>(json);
            if (parsed == null)
            {
                Debug.LogError($"ArtworkManagerNew: Failed to parse JSON from {sourceDescription} into ArtworkConfigNew.");
                return;
            }

            currentConfig = parsed;
            lastLoadSucceeded = true;

            // Count how many images we expect to download for this config so UI can track progress.
            totalImagesToDownload = CountImagesToDownload();
            downloadedImagesCount = 0;
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
            if (exhibition?.walls?.paintings == null) continue;
            result.AddRange(exhibition.walls.paintings);
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
            if (exhibition?.walls?.paintings == null)
                continue;

            foreach (var painting in exhibition.walls.paintings)
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
            if (exhibition?.walls == null) continue;
            if (!string.Equals(exhibition.walls.wallId, wallId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (exhibition.walls.paintings != null)
            {
                result.AddRange(exhibition.walls.paintings);
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
            if (exhibition?.walls == null) continue;
            if (string.Equals(exhibition.walls.wallId, wallId, StringComparison.OrdinalIgnoreCase))
            {
                return exhibition;
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
    /// Convenience helper: given an ExhibitionConfigNew entry, instantiate the correct FrameLayout
    /// on the correct DisplayWall using exhibition.walls.layoutId and exhibition.walls.wallId.
    /// </summary>
    public FrameLayout InstantiateLayoutForExhibition(ExhibitionConfigNew exhibition)
    {
        if (exhibition == null || exhibition.walls == null)
        {
            Debug.LogWarning("ArtworkManagerNew: InstantiateLayoutForExhibition called with null exhibition or walls.");
            return null;
        }

        return InstantiateLayoutOnWall(exhibition.walls.layoutId, exhibition.walls.wallId);
    }

    /// <summary>
    /// Instantiates layouts for all exhibitions in the current JSON config.
    /// Uses each exhibition.walls.layoutId and exhibition.walls.wallId to pick the
    /// appropriate FrameLayout prefab and spawn it under the right DisplayWall.
    /// This does NOT load any images; call PopulateLayoutsWithImagesFromJson() after
    /// this if you want to fill the frames.
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

            var layoutId = exhibition.walls.layoutId;
            var wallId = exhibition.walls.wallId;

            var layoutInstance = InstantiateLayoutOnWall(layoutId, wallId);
            if (layoutInstance == null)
            {
                Debug.LogWarning($"ArtworkManagerNew: Failed to instantiate layout '{layoutId}' on wall '{wallId}'.");
            }
        }
    }

    /// <summary>
    /// Sequentially instantiates a layout and then applies its images for each
    /// exhibition in the current config. This avoids kicking off many image
    /// download coroutines at once, which can cause some images to fail.
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

            var layoutId = exhibition.walls.layoutId;
            var wallId = exhibition.walls.wallId;

            var layoutInstance = InstantiateLayoutOnWall(layoutId, wallId);
            if (layoutInstance == null)
            {
                Debug.LogWarning($"ArtworkManagerNew: Failed to instantiate layout '{layoutId}' on wall '{wallId}' during sequential build.");
                continue;
            }

            // Apply images for this one layout before moving to the next
            yield return ApplyPaintingsToLayout(exhibition, layoutInstance);

            if (layoutBuildDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(layoutBuildDelaySeconds);
            }
        }
    }

    /// <summary>
    /// For all exhibitions in the current JSON config, finds the instantiated FrameLayout
    /// under the appropriate DisplayWall and downloads mainImage.src for each painting
    /// into the corresponding ArtworkFrame.
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

            // Find the wall instance in the scene
            var wall = GetDisplayWallForWallId(exhibition.walls.wallId);
            if (wall == null)
            {
                Debug.LogWarning($"ArtworkManagerNew: No DisplayWall found for wallId '{exhibition.walls.wallId}' when populating images.");
                continue;
            }

            // Find a FrameLayout instance on this wall whose LayoutId matches the numeric part of JSON layoutId
            int layoutNumericId = ParseNumericSuffix(exhibition.walls.layoutId);
            if (layoutNumericId < 0)
            {
                Debug.LogWarning($"ArtworkManagerNew: Could not parse numeric layout index from layoutId '{exhibition.walls.layoutId}' when populating images.");
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
            StartCoroutine(ApplyPaintingsToLayout(exhibition, targetLayout));
        }
    }

    /// <summary>
    /// For a given exhibition and instantiated FrameLayout, download and assign
    /// mainImage textures to each ArtworkFrame child. Extra paintings are ignored;
    /// extra frames are left empty (texture cleared).
    /// </summary>
    private IEnumerator ApplyPaintingsToLayout(ExhibitionConfigNew exhibition, FrameLayout layout)
    {
        if (exhibition == null || exhibition.walls == null || layout == null)
            yield break;

        var paintings = exhibition.walls.paintings;
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

        Debug.Log($"ArtworkManagerNew: Loading image for frame '{frame.name}' from URL: {imageUrl}");

        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl);
        imageRequest.timeout = (int)apiTimeoutSeconds;

        yield return imageRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (imageRequest.result == UnityWebRequest.Result.Success)
#else
        if (!imageRequest.isNetworkError && !imageRequest.isHttpError)
#endif
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
            texture = FlipTextureVertically(texture);
            frame.SetTexture(texture);
        }
        else
        {
            Debug.LogWarning($"ArtworkManagerNew: Failed to load image from URL: {imageUrl}. Error: {imageRequest.error}");
            frame.ClearTexture();
        }

        imageRequest.Dispose();

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
    public WallsConfigNew walls;
}

/// <summary>
/// Wall configuration for an exhibition.
/// </summary>
[Serializable]
public class WallsConfigNew
{
    public List<PaintingConfigNew> paintings;
    public string wallId;
    public string layoutId;
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
}

/// <summary>
/// Tag information (e.g. { "name": "love", "id": "..." }).
/// </summary>
[Serializable]
public class TagConfigNew
{
    public string name;
    public string id; // optional in JSON
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
