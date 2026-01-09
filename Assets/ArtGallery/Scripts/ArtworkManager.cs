using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Utilities;

/// <summary>
/// Manages all artworks in the gallery.
/// Handles loading, placement, and interaction coordination.
/// </summary>
public class ArtworkManager : MonoBehaviour
{
    [Header("Artwork Configuration")]
    [SerializeField] private List<ArtworkData> artworkDatabase = new List<ArtworkData>();
    [SerializeField] private string jsonConfigPath = "ArtworkConfig.json";
    [SerializeField] private bool useProductsJSON = true; // Toggle to use products.json instead of ArtworkConfig.json
    [SerializeField] private string productsJsonPath = "products.json"; // Path to products.json in Resources folder
    
    [Header("API Configuration")]
    [SerializeField] private bool useAPI = false; // Toggle to use API instead of JSON
    [SerializeField] private string apiUrl = "https://api.example.com/artworks"; // API endpoint URL
    [SerializeField] private float apiTimeout = 10f; // Timeout in seconds
    [SerializeField] private bool loadImagesFromAPI = true; // Whether to download images from API URLs
    // Note: Custom headers should be added programmatically using AddAPIHeader() method
    // Dictionary is not serializable in Unity Inspector
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>(); // Custom headers for API requests

    [Header("Size Settings (Optional)")]
    [SerializeField] private bool useParsedAvailableSizeForPreferredSize = false;
    [SerializeField] private bool useLargestParsedSize = true; // If false, use last size in the list
    
    [Header("Prefabs")]
    [SerializeField] private GameObject artworkFramePrefab;
    
    [Header("References")]
    public ArtworkUI artworkUI;
    
    [Header("Placement Settings")]
    [SerializeField] private bool autoPlaceOnStart = true;
    [SerializeField] private float defaultWallDistance = 0.02f; // Distance from wall surface (in meters) - reduced for closer placement
    [SerializeField] private LayerMask wallLayer = 1 << 6; // Default layer 6 for walls
    
    [Header("Custom Placement Transforms")]
    [SerializeField] private List<Transform> placementTransforms = new List<Transform>();
    [SerializeField] private bool useCustomTransforms = false; // If true, use placementTransforms instead of auto-placing on walls
    
    private List<ArtworkFrame> placedFrames = new List<ArtworkFrame>();
    private Camera playerCamera;
    private bool isLoadingFromAPI = false;
    
    private void Awake()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        // Try to load artworks if database is empty
        if (artworkDatabase.Count == 0)
        {
            if (useAPI)
            {
                StartCoroutine(LoadArtworksFromAPI());
            }
            else if (useProductsJSON)
            {
                StartCoroutine(LoadArtworksFromProductsJSON());
            }
            else
            {
                LoadArtworksFromJSON();
            }
        }
    }
    
    private void Start()
    {
        // If loading from API, wait for it to complete before placing
        if (isLoadingFromAPI)
        {
            StartCoroutine(WaitForAPILoadAndPlace());
        }
        else
        {
            // Auto-place artworks if enabled
            if (autoPlaceOnStart && artworkDatabase.Count > 0)
            {
                if (useCustomTransforms && placementTransforms != null && placementTransforms.Count > 0)
                {
                    PlaceArtworksAtTransforms();
                }
                else
                {
                    AutoPlaceArtworksOnWalls();
                }
            }
            
            // Setup interaction for all frames
            SetupArtworkInteractions();
        }
    }
    
    /// <summary>
    /// Waits for API loading to complete before placing artworks.
    /// </summary>
    private IEnumerator WaitForAPILoadAndPlace()
    {
        while (isLoadingFromAPI)
        {
            yield return null;
        }
        
        // Auto-place artworks if enabled
        if (autoPlaceOnStart && artworkDatabase.Count > 0)
        {
            if (useCustomTransforms && placementTransforms != null && placementTransforms.Count > 0)
            {
                PlaceArtworksAtTransforms();
            }
            else
            {
                AutoPlaceArtworksOnWalls();
            }
        }
        
        // Setup interaction for all frames
        SetupArtworkInteractions();
    }
    
    /// <summary>
    /// Places an artwork on a wall at the specified position.
    /// </summary>
    public ArtworkFrame PlaceArtworkOnWall(ArtworkData artwork, Vector3 position, Vector3 wallNormal)
    {
        if (artwork == null) return null;
        
        // Create frame if prefab exists, otherwise create programmatically
        GameObject frameObject;
        if (artworkFramePrefab != null)
        {
            frameObject = Instantiate(artworkFramePrefab, position, Quaternion.LookRotation(-wallNormal));
        }
        else
        {
            frameObject = new GameObject($"ArtworkFrame_{artwork.title}");
            frameObject.transform.position = position;
            frameObject.transform.rotation = Quaternion.LookRotation(-wallNormal);
            frameObject.AddComponent<ArtworkFrame>();
        }
        
        ArtworkFrame frame = frameObject.GetComponent<ArtworkFrame>();
        if (frame != null)
        {
            frame.SetArtwork(artwork);
            placedFrames.Add(frame);
            
            // Set up interaction immediately
            frame.OnArtworkClicked -= HandleArtworkClicked;
            frame.OnArtworkHovered -= HandleArtworkHovered;
            frame.OnArtworkClicked += HandleArtworkClicked;
            frame.OnArtworkHovered += HandleArtworkHovered;
        }
        
        return frame;
    }

    /// <summary>
    /// Places an artwork on the center grid cell of an InchWall (using InchWallGridData).
    /// For odd grids (e.g., 3x3), this is the exact center cell (1,1 in 0-based, 2x2 in 1-based).
    /// For even grids, chooses the upper-left of the four center cells.
    /// </summary>
    public ArtworkFrame PlaceArtworkOnInchWallCenter(ArtworkData artwork, InchWallGridData gridData)
    {
        if (artwork == null || gridData == null)
        {
            Debug.LogWarning("ArtworkManager: Cannot place artwork on InchWall center, artwork or gridData is null.");
            return null;
        }

        int cellsX = gridData.CellsX;
        int cellsY = gridData.CellsY;
        if (cellsX <= 0 || cellsY <= 0)
        {
            Debug.LogWarning("ArtworkManager: InchWallGridData has invalid grid size.");
            return null;
        }

        // Smart detection: if center already has an artwork, do not place another one
        if (gridData.IsCenterOccupied())
        {
            Debug.LogError("ArtworkManager: InchWall center is already occupied. Cannot place another artwork in the center.");
            return null;
        }

        // Center indices: odd -> exact center, even -> upper-left of 4 central cells
        int centerX = (cellsX - 1) / 2;
        int centerY = (cellsY - 1) / 2;

        Vector3 centerWorldPos = gridData.GetWorldPosition(centerX, centerY);

        // Wall normal: front face is -forward of the wall root (InchWallBuilder used this convention)
        Vector3 wallNormal = -gridData.transform.forward;

        // Offset slightly from wall using existing defaultWallDistance
        Vector3 position = centerWorldPos + wallNormal * defaultWallDistance;

        ArtworkFrame frame = PlaceArtworkOnWall(artwork, position, wallNormal);
        if (frame != null)
        {
            // Use the frame's actual outer size in inches (artwork + bleeding + frame thickness)
            // to record how many 1x1 inch grid cells this frame occupies on the InchWall.
            Vector2 outerSizeInches = frame.GetOuterSizeInches();
            gridData.MarkCenterOccupied(frame, outerSizeInches);
        }

        return frame;
    }

    /// <summary>
    /// Attempts to place a second artwork on the same InchWall.
    /// The wall is conceptually divided horizontally into two equal parts; the first artwork
    /// is centered in the left half, and the second artwork is centered in the right half.
    /// If either artwork does not fit within its half (or exceeds wall height), placement fails
    /// and an error is logged.
    ///
    /// Usage pattern:
    /// 1) Call PlaceArtworkOnInchWallCenter() for the first artwork (initially at wall center).
    /// 2) Call TryPlaceSecondArtworkOnInchWallSideBySide() for the second artwork; both are then moved
    ///    so each is centered within its half of the InchWall.
    /// </summary>
    public bool TryPlaceSecondArtworkOnInchWallSideBySide(ArtworkData secondArtwork, InchWallGridData gridData)
    {
        if (secondArtwork == null || gridData == null)
        {
            Debug.LogWarning("ArtworkManager: Cannot place second artwork on InchWall; artwork or gridData is null.");
            return false;
        }

        int cellsX = gridData.CellsX;
        int cellsY = gridData.CellsY;
        if (cellsX <= 0 || cellsY <= 0)
        {
            Debug.LogWarning("ArtworkManager: InchWallGridData has invalid grid size.");
            return false;
        }

        // We require an existing center artwork to form a pair.
        if (!gridData.IsCenterOccupied())
        {
            Debug.LogError("ArtworkManager: No center artwork on InchWall. Place the first artwork in the center before adding a second.");
            return false;
        }

        ArtworkFrame firstFrame = gridData.GetCenterArtworkFrame();
        if (firstFrame == null)
        {
            Debug.LogError("ArtworkManager: Center artwork reference on InchWall is missing.");
            return false;
        }

        // Compute outer sizes for existing (first) and new (second) artworks in inches
        Vector2 firstSizeInches = firstFrame.GetOuterSizeInches();

        // We'll create the second frame near the center, compute its size, then reposition both if they fit.
        // Determine wall center and normal
        int centerCellX = (cellsX - 1) / 2;
        int centerCellY = (cellsY - 1) / 2;
        Vector3 centerWorldPos = gridData.GetWorldPosition(centerCellX, centerCellY);
        Vector3 wallNormal = -gridData.transform.forward;

        // Temporary placement for the second frame (at center) so it can build its geometry and report size
        ArtworkFrame secondFrame = PlaceArtworkOnWall(secondArtwork, centerWorldPos + wallNormal * defaultWallDistance, wallNormal);
        if (secondFrame == null)
        {
            Debug.LogError("ArtworkManager: Failed to create second artwork frame on InchWall.");
            return false;
        }

        Vector2 secondSizeInches = secondFrame.GetOuterSizeInches();
 
        // Convert sizes to grid cells (1 cell = 1 inch)
        int firstWidthCells   = Mathf.Max(1, Mathf.RoundToInt(firstSizeInches.x));
        int firstHeightCells  = Mathf.Max(1, Mathf.RoundToInt(firstSizeInches.y));
        int secondWidthCells  = Mathf.Max(1, Mathf.RoundToInt(secondSizeInches.x));
        int secondHeightCells = Mathf.Max(1, Mathf.RoundToInt(secondSizeInches.y));
 
        int maxHeightCells = Mathf.Max(firstHeightCells, secondHeightCells);
 
        // Vertical fit check for both artworks independently against wall height
        if (maxHeightCells > cellsY)
        {
            Debug.LogError($"ArtworkManager: Cannot place two artworks on InchWall; required height {maxHeightCells} cells exceeds wall height {cellsY} cells.");
            placedFrames.Remove(secondFrame);
#if UNITY_EDITOR
            DestroyImmediate(secondFrame.gameObject);
#else
            Destroy(secondFrame.gameObject);
#endif
            return false;
        }
 
        // Horizontal fit check per half: divide wall horizontally into two halves
        int mid = cellsX / 2;              // split index
        int leftHalfWidth  = mid;          // cells [0 .. mid-1]
        int rightHalfWidth = cellsX - mid; // cells [mid .. cellsX-1]
 
        if (firstWidthCells > leftHalfWidth)
        {
            Debug.LogError($"ArtworkManager: First artwork width {firstWidthCells} cells does not fit in left half of width {leftHalfWidth} cells.");
            placedFrames.Remove(secondFrame);
#if UNITY_EDITOR
            DestroyImmediate(secondFrame.gameObject);
#else
            Destroy(secondFrame.gameObject);
#endif
            return false;
        }
 
        if (secondWidthCells > rightHalfWidth)
        {
            Debug.LogError($"ArtworkManager: Second artwork width {secondWidthCells} cells does not fit in right half of width {rightHalfWidth} cells.");
            placedFrames.Remove(secondFrame);
#if UNITY_EDITOR
            DestroyImmediate(secondFrame.gameObject);
#else
            Destroy(secondFrame.gameObject);
#endif
            return false;
        }
 
        // Compute world distance for one cell step to the right (1 inch along wall)
        Vector3 rightStepWorld = gridData.GetWorldPosition(Mathf.Min(centerCellX + 1, cellsX - 1), centerCellY) - centerWorldPos;
 
        // Centers for left and right halves in cell indices
        int leftCount  = leftHalfWidth;
        int rightCount = rightHalfWidth;
 
        int leftCenterCellX  = (leftCount - 1) / 2;                    // 0-based in [0, leftHalfWidth-1]
        int rightCenterCellX = mid + (rightCount - 1) / 2;             // 0-based in [mid, cellsX-1]
 
        // Convert cell centers to world positions
        Vector3 leftCenterWorld  = gridData.GetWorldPosition(leftCenterCellX,  centerCellY);
        Vector3 rightCenterWorld = gridData.GetWorldPosition(rightCenterCellX, centerCellY);
 
        // Final positions for artwork centers
        Vector3 firstCenterWorld  = leftCenterWorld;
        Vector3 secondCenterWorld = rightCenterWorld;

        // Apply final positions and orientations
        firstFrame.transform.position  = firstCenterWorld + wallNormal * defaultWallDistance;
        firstFrame.transform.rotation  = Quaternion.LookRotation(-wallNormal);

        secondFrame.transform.position = secondCenterWorld + wallNormal * defaultWallDistance;
        secondFrame.transform.rotation = Quaternion.LookRotation(-wallNormal);

        // Update InchWall occupancy to reflect the combined pair footprint.
        // Occupied span is from the leftmost used cell to the rightmost used cell.
        // Estimate using discrete cell indices based on half centers and widths.
        int firstHalfSpan  = firstWidthCells;
        int secondHalfSpan = secondWidthCells;
 
        int firstMinCellX  = leftCenterCellX  - firstHalfSpan  / 2;
        int firstMaxCellX  = firstMinCellX    + firstHalfSpan  - 1;
        int secondMinCellX = rightCenterCellX - secondHalfSpan / 2;
        int secondMaxCellX = secondMinCellX   + secondHalfSpan - 1;
 
        int minCellXUsed = Mathf.Max(0, Mathf.Min(firstMinCellX, secondMinCellX));
        int maxCellXUsed = Mathf.Min(cellsX - 1, Mathf.Max(firstMaxCellX, secondMaxCellX));
        int occupiedWidthCells = Mathf.Max(1, maxCellXUsed - minCellXUsed + 1);
 
        float combinedWidthInches  = occupiedWidthCells; // 1 cell = 1 inch
        float combinedHeightInches = Mathf.Max(firstSizeInches.y, secondSizeInches.y);
        gridData.MarkCenterOccupied(firstFrame, new Vector2(combinedWidthInches, combinedHeightInches));

        Debug.Log($"ArtworkManager: Placed two artworks side by side on InchWall (total width {combinedWidthInches} inches).");
        return true;
    }
    
    /// <summary>
    /// Places an artwork at a specific transform position.
    /// Automatically snaps to the wall it's facing.
    /// </summary>
    public ArtworkFrame PlaceArtwork(ArtworkData artwork, Transform placementTransform)
    {
        if (artwork == null || placementTransform == null) return null;
        
        Vector3 position = placementTransform.position;
        Quaternion rotation = placementTransform.rotation;
        Vector3 wallNormal = -placementTransform.forward;
        
        // Snap to wall if one is found
        RaycastHit hit;
        Vector3 rayOrigin = placementTransform.position;
        Vector3 rayDirection = -placementTransform.forward; // Raycast in the direction the artwork is facing
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, 10f, wallLayer))
        {
            // Found a wall - snap to it
            position = hit.point + hit.normal * defaultWallDistance;
            wallNormal = hit.normal;
            rotation = Quaternion.LookRotation(-wallNormal);
        }
        else
        {
            // Try reverse direction in case artwork is behind the wall
            if (Physics.Raycast(rayOrigin, -rayDirection, out hit, 10f, wallLayer))
            {
                position = hit.point + hit.normal * defaultWallDistance;
                wallNormal = hit.normal;
                rotation = Quaternion.LookRotation(-wallNormal);
            }
        }
        
        GameObject frameObject;
        if (artworkFramePrefab != null)
        {
            frameObject = Instantiate(artworkFramePrefab, position, rotation);
        }
        else
        {
            frameObject = new GameObject($"ArtworkFrame_{artwork.title}");
            frameObject.transform.position = position;
            frameObject.transform.rotation = rotation;
            frameObject.AddComponent<ArtworkFrame>();
        }
        
        ArtworkFrame frame = frameObject.GetComponent<ArtworkFrame>();
        if (frame != null)
        {
            frame.SetArtwork(artwork);
            placedFrames.Add(frame);
            
            // Set up interaction immediately
            frame.OnArtworkClicked -= HandleArtworkClicked;
            frame.OnArtworkHovered -= HandleArtworkHovered;
            frame.OnArtworkClicked += HandleArtworkClicked;
            frame.OnArtworkHovered += HandleArtworkHovered;
        }
        
        return frame;
    }
    
    /// <summary>
    /// Snaps an existing artwork frame to the nearest wall it's facing.
    /// </summary>
    public bool SnapArtworkToWall(ArtworkFrame frame, float maxDistance = 10f)
    {
        if (frame == null) return false;
        
        Transform frameTransform = frame.transform;
        Vector3 rayOrigin = frameTransform.position;
        Vector3 rayDirection = -frameTransform.forward; // Raycast in the direction the artwork is facing
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance, wallLayer))
        {
            // Found a wall - snap to it
            Vector3 wallNormal = hit.normal;
            frameTransform.position = hit.point + wallNormal * defaultWallDistance;
            frameTransform.rotation = Quaternion.LookRotation(-wallNormal);
            return true;
        }
        else
        {
            // Try reverse direction
            if (Physics.Raycast(rayOrigin, -rayDirection, out hit, maxDistance, wallLayer))
            {
                Vector3 wallNormal = hit.normal;
                frameTransform.position = hit.point + wallNormal * defaultWallDistance;
                frameTransform.rotation = Quaternion.LookRotation(-wallNormal);
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Places artworks sequentially at the provided placement transforms.
    /// Limits placement to the number of available placement points (ignores extra artworks).
    /// </summary>
    public void PlaceArtworksAtTransforms()
    {
        // Check if we have artworks
        if (artworkDatabase == null || artworkDatabase.Count == 0)
        {
            Debug.LogWarning("ArtworkManager: No artworks in database. Add ArtworkData assets or configure JSON.");
            return;
        }
        
        // Check if we have placement transforms
        if (placementTransforms == null || placementTransforms.Count == 0)
        {
            Debug.LogWarning("ArtworkManager: No placement transforms provided. Falling back to wall placement.");
            AutoPlaceArtworksOnWalls();
            return;
        }
        
        // Limit artworks to available placement points
        int artworksToPlace = Mathf.Min(artworkDatabase.Count, placementTransforms.Count);
        
        if (artworkDatabase.Count > placementTransforms.Count)
        {
            Debug.Log($"ArtworkManager: {artworkDatabase.Count} artworks available, but only {placementTransforms.Count} placement points. Placing first {artworksToPlace} artworks.");
        }
        else
        {
            Debug.Log($"ArtworkManager: Placing {artworksToPlace} artworks at {placementTransforms.Count} transforms...");
        }
        
        int placedCount = 0;
        
        for (int i = 0; i < artworksToPlace; i++)
        {
            ArtworkData artwork = artworkDatabase[i];
            if (artwork == null)
            {
                Debug.LogWarning($"ArtworkManager: Skipping null artwork at index {i}");
                continue;
            }
            
            Transform placementTransform = placementTransforms[i];
            
            if (placementTransform == null)
            {
                Debug.LogWarning($"ArtworkManager: Placement transform at index {i} is null. Skipping artwork '{artwork.title}'.");
                continue;
            }
            
            // Place artwork at transform
            ArtworkFrame frame = PlaceArtwork(artwork, placementTransform);
            if (frame != null)
            {
                placedCount++;
            }
        }
        
        Debug.Log($"ArtworkManager: Successfully placed {placedCount} out of {artworksToPlace} artworks at transforms.");
        
        // Setup interactions for newly placed frames
        SetupArtworkInteractions();
    }
    
    /// <summary>
    /// Automatically places all artworks from the database on walls.
    /// </summary>
    public void AutoPlaceArtworksOnWalls()
    {
        // Check if we have artworks
        if (artworkDatabase == null || artworkDatabase.Count == 0)
        {
            Debug.LogWarning("ArtworkManager: No artworks in database. Add ArtworkData assets or configure JSON.");
            return;
        }
        
        // Find all walls in the scene
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        if (walls.Length == 0)
        {
            Debug.LogWarning("ArtworkManager: No walls found with 'Wall' tag. Please tag your wall objects or use GalleryBuilder to create walls.");
            return;
        }
        
        Debug.Log($"ArtworkManager: Placing {artworkDatabase.Count} artworks on {walls.Length} walls...");
        
        int artworkIndex = 0;
        int placedCount = 0;
        
        foreach (ArtworkData artwork in artworkDatabase)
        {
            if (artwork == null)
            {
                Debug.LogWarning($"ArtworkManager: Skipping null artwork at index {artworkIndex}");
                artworkIndex++;
                continue;
            }
            
            // Select a wall (round-robin)
            GameObject wall = walls[artworkIndex % walls.Length];
            
            // Get wall bounds and normal
            Renderer wallRenderer = wall.GetComponent<Renderer>();
            if (wallRenderer == null)
            {
                Debug.LogWarning($"ArtworkManager: Wall '{wall.name}' has no Renderer component. Skipping artwork '{artwork.title}'.");
                artworkIndex++;
                continue;
            }
            
            Bounds wallBounds = wallRenderer.bounds;
            Vector3 wallNormal = -wall.transform.forward;
            
            // Calculate position on wall (distribute artworks)
            float spacing = 3f; // 3 meters between artworks
            float xOffset = (artworkIndex % 3 - 1) * spacing; // 3 artworks per wall
            float yOffset = (artworkIndex / 3) * 2f; // Stack vertically
            
            Vector3 position = wallBounds.center + wall.transform.right * xOffset + wall.transform.up * yOffset;
            position += wallNormal * defaultWallDistance;
            
            ArtworkFrame frame = PlaceArtworkOnWall(artwork, position, wallNormal);
            if (frame != null)
            {
                placedCount++;
            }
            
            artworkIndex++;
        }
        
        Debug.Log($"ArtworkManager: Successfully placed {placedCount} out of {artworkDatabase.Count} artworks.");
        
        // Setup interactions for newly placed frames
        SetupArtworkInteractions();
    }
    
    private void SetupArtworkInteractions()
    {
        foreach (ArtworkFrame frame in placedFrames)
        {
            if (frame != null)
            {
                // Remove existing handlers first to avoid duplicates
                frame.OnArtworkClicked -= HandleArtworkClicked;
                frame.OnArtworkHovered -= HandleArtworkHovered;
                
                // Add handlers
                frame.OnArtworkClicked += HandleArtworkClicked;
                frame.OnArtworkHovered += HandleArtworkHovered;
            }
        }
        
        Debug.Log($"ArtworkManager: Set up interactions for {placedFrames.Count} artwork frames.");
    }
    
    private void HandleArtworkClicked(ArtworkData artwork)
    {
        Debug.Log($"ArtworkManager: HandleArtworkClicked called for artwork: {artwork?.title ?? "NULL"}");
        
        if (artwork == null)
        {
            Debug.LogWarning("ArtworkManager: Received click event with null artwork data.");
            return;
        }
        
        if (artworkUI == null)
        {
            Debug.LogError($"ArtworkManager: ArtworkUI reference is not assigned! Clicked artwork: {artwork.title} by {artwork.artist}");
            Debug.LogError($"ArtworkManager: Please assign the ArtworkUI component to ArtworkManager in the Inspector.");
            return;
        }
        
        Debug.Log($"ArtworkManager: Showing artwork '{artwork.title}' in UI.");
        artworkUI.ShowArtwork(artwork);
    }
    
    private void HandleArtworkHovered(ArtworkData artwork)
    {
        // Optional: Show hover tooltip or highlight
        // Debug.Log($"Artwork Hovered: {artwork.title}");
    }
    
    /// <summary>
    /// Loads artwork data from a JSON configuration file.
    /// </summary>
    private void LoadArtworksFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonConfigPath.Replace(".json", ""));
        if (jsonFile == null)
        {
            Debug.LogWarning($"Could not load artwork config from {jsonConfigPath}");
            return;
        }
        
        ArtworkConfig config = JsonUtility.FromJson<ArtworkConfig>(jsonFile.text);
        if (config != null && config.artworks != null)
        {
            foreach (ArtworkConfigEntry entry in config.artworks)
            {
                // Create ScriptableObject data from JSON entry
                ArtworkData data = ScriptableObject.CreateInstance<ArtworkData>();
                data.title = entry.title;
                data.artist = entry.artist;
                data.description = entry.description;
                data.year = entry.year;
                data.medium = entry.medium;
                data.category = entry.category;
                data.url = entry.url;
                
                // Load texture from Resources
                if (!string.IsNullOrEmpty(entry.imagePath))
                {
                    Texture2D texture = Resources.Load<Texture2D>(entry.imagePath);
                    if (texture != null)
                    {
                        data.image = texture;
                    }
                }
                
                artworkDatabase.Add(data);
            }
        }
    }
    
    /// <summary>
    /// Loads artwork data from products.json file.
    /// This is a coroutine that handles async image loading from URLs.
    /// </summary>
    private IEnumerator LoadArtworksFromProductsJSON()
    {
        isLoadingFromAPI = true; // Reuse this flag to indicate async loading
        
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>(productsJsonPath.Replace(".json", ""));
        if (jsonFile == null)
        {
            Debug.LogError($"ArtworkManager: Could not load products JSON from {productsJsonPath}. Make sure the file is in a Resources folder.");
            isLoadingFromAPI = false;
            yield break;
        }
        
        Debug.Log($"ArtworkManager: Loading artworks from products.json...");
        
        // Parse JSON using Newtonsoft.Json (try-catch only for parsing, not for yield operations)
        List<ProductData> products = null;
        try
        {
            products = JsonConvert.DeserializeObject<List<ProductData>>(jsonFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ArtworkManager: Error parsing products.json: {e.Message}\n{e.StackTrace}");
            isLoadingFromAPI = false;
            yield break;
        }
        
        if (products == null || products.Count == 0)
        {
            Debug.LogWarning("ArtworkManager: No products found in products.json or JSON parsing failed.");
            isLoadingFromAPI = false;
            yield break;
        }
        
        Debug.Log($"ArtworkManager: Found {products.Count} products in JSON. Converting to artworks...");

        // Parse availableSizes strings into numeric values for each product
        foreach (var p in products)
        {
            p?.ParseAvailableSizes();
        }
        
        // Limit to available placement points if using custom transforms
        int maxArtworks = products.Count;
        if (useCustomTransforms && placementTransforms != null && placementTransforms.Count > 0)
        {
            maxArtworks = Mathf.Min(products.Count, placementTransforms.Count);
            if (products.Count > placementTransforms.Count)
            {
                Debug.Log($"ArtworkManager: Limiting to {maxArtworks} artworks (matching {placementTransforms.Count} placement points).");
            }
        }
        
        int processedCount = 0;
        
        foreach (ProductData product in products)
        {
            if (processedCount >= maxArtworks)
            {
                break; // Stop if we've reached the limit
            }
            
            if (product == null || product.mainImage == null || string.IsNullOrEmpty(product.mainImage.src))
            {
                Debug.LogWarning($"ArtworkManager: Skipping product with missing image data: {product?.name ?? "Unknown"}");
                continue;
            }
            
            // Create ArtworkData from ProductData
            ArtworkData data = ScriptableObject.CreateInstance<ArtworkData>();
            data.title = product.name ?? "Untitled";
            data.artist = "Artwork"; // Products don't have artist field, using default
            data.description = product.description ?? product.shortDescription ?? "No description available.";
            data.year = 2024; // Default year since products don't have year
            data.medium = product.medium ?? "Digital";
            data.category = product.category ?? "General";
            data.url = product.slug ?? ""; // Using slug as URL

            // Optionally use parsed available sizes to drive the preferred display size
            if (useParsedAvailableSizeForPreferredSize &&
                product.parsedAvailableSizes != null &&
                product.parsedAvailableSizes.Count > 0)
            {
                ProductSize chosenSize = null;

                if (useLargestParsedSize)
                {
                    // Pick the size with the largest area
                    chosenSize = product.parsedAvailableSizes[0];
                    float largestArea = chosenSize.width * chosenSize.height;

                    for (int i = 1; i < product.parsedAvailableSizes.Count; i++)
                    {
                        var size = product.parsedAvailableSizes[i];
                        float area = size.width * size.height;
                        if (area > largestArea)
                        {
                            chosenSize = size;
                            largestArea = area;
                        }
                    }
                }
                else
                {
                    // Fallback: just use the last size in the list
                    chosenSize = product.parsedAvailableSizes[product.parsedAvailableSizes.Count - 1];
                }

                if (chosenSize != null)
                {
                    // Sizes from parsedAvailableSizes are in inches. Store directly in ArtworkData as inches.
                    // Conversion to Unity units (meters) is done at runtime via UnitConversionExtensions for
                    // maximum precision.
                    data.preferredSizeInches = new Vector2(chosenSize.width, chosenSize.height);
                }
            }
            
            // Load image from URL (yield return must be outside try-catch)
            yield return StartCoroutine(LoadImageFromURL(product.mainImage.src, data));
            
            artworkDatabase.Add(data);
            processedCount++;
            
            // Log progress every 10 items
            if (processedCount % 10 == 0)
            {
                Debug.Log($"ArtworkManager: Processed {processedCount}/{maxArtworks} artworks...");
            }
        }
        
        Debug.Log($"ArtworkManager: Successfully loaded {artworkDatabase.Count} artworks from products.json.");
        isLoadingFromAPI = false;
    }
    
    /// <summary>
    /// Loads artwork data from an API endpoint.
    /// This is a coroutine that handles async API calls.
    /// </summary>
    private IEnumerator LoadArtworksFromAPI()
    {
        isLoadingFromAPI = true;
        
        if (string.IsNullOrEmpty(apiUrl))
        {
            Debug.LogError("ArtworkManager: API URL is not set. Cannot load artworks from API.");
            isLoadingFromAPI = false;
            yield break;
        }
        
        Debug.Log($"ArtworkManager: Loading artworks from API: {apiUrl}");
        
        // Create UnityWebRequest for API call
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        
        // Add custom headers if any
        if (apiHeaders != null && apiHeaders.Count > 0)
        {
            foreach (var header in apiHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }
        
        // Set timeout
        request.timeout = (int)apiTimeout;
        
        // Send request
        yield return request.SendWebRequest();
        
        // Check for errors
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"ArtworkManager: API request failed: {request.error}");
            isLoadingFromAPI = false;
            yield break;
        }
        
        // Parse JSON response
        string jsonResponse = request.downloadHandler.text;
        Debug.Log($"ArtworkManager: Received API response: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");
        
        // Try to parse as ArtworkConfig (same structure as JSON file)
        // If your API returns a different structure, you'll need to adjust this
        ArtworkConfig config = JsonUtility.FromJson<ArtworkConfig>(jsonResponse);
        
        if (config != null && config.artworks != null)
        {
            Debug.Log($"ArtworkManager: Parsed {config.artworks.Length} artworks from API.");
            
            foreach (ArtworkConfigEntry entry in config.artworks)
            {
                // Create ScriptableObject data from API entry
                ArtworkData data = ScriptableObject.CreateInstance<ArtworkData>();
                data.title = entry.title ?? "Untitled";
                data.artist = entry.artist ?? "Unknown Artist";
                data.description = entry.description ?? "No description available.";
                data.year = entry.year;
                data.medium = entry.medium ?? "Unknown";
                data.category = entry.category ?? "General";
                data.url = entry.url ?? "";
                
                // Load image from API URL if provided and enabled
                if (loadImagesFromAPI && !string.IsNullOrEmpty(entry.imagePath))
                {
                    yield return StartCoroutine(LoadImageFromURL(entry.imagePath, data));
                }
                // Fallback: Try to load from Resources if imagePath is a local path
                else if (!string.IsNullOrEmpty(entry.imagePath))
                {
                    Texture2D texture = Resources.Load<Texture2D>(entry.imagePath);
                    if (texture != null)
                    {
                        data.image = texture;
                    }
                }
                
                artworkDatabase.Add(data);
            }
            
            Debug.Log($"ArtworkManager: Successfully loaded {artworkDatabase.Count} artworks from API.");
        }
        else
        {
            // If the API structure is different, try alternative parsing
            // You can uncomment and modify this section based on your actual API response structure
            /*
            // Example: If API returns a different structure, parse it here
            // ArtworkApiResponse apiResponse = JsonUtility.FromJson<ArtworkApiResponse>(jsonResponse);
            // Then convert apiResponse to ArtworkData objects
            */
            
            Debug.LogWarning("ArtworkManager: Could not parse API response as ArtworkConfig. " +
                           "The API response structure may be different. " +
                           "Check the API response format and update the parsing logic.");
        }
        
        request.Dispose();
        isLoadingFromAPI = false;
    }
    
    /// <summary>
    /// Flips a texture vertically to fix upside-down images loaded from URLs.
    /// </summary>
    private Texture2D FlipTextureVertically(Texture2D original)
    {
        if (original == null) return null;
        
        int width = original.width;
        int height = original.height;
        
        // Create a new texture with the same format
        Texture2D flipped = new Texture2D(width, height, original.format, false);
        
        // Read all pixels from original texture
        Color[] pixels = original.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];
        
        // Flip pixels vertically
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int originalIndex = y * width + x;
                int flippedIndex = (height - 1 - y) * width + x;
                flippedPixels[flippedIndex] = pixels[originalIndex];
            }
        }
        
        // Apply flipped pixels to new texture
        flipped.SetPixels(flippedPixels);
        flipped.Apply();
        
        return flipped;
    }
    
    /// <summary>
    /// Loads an image texture from a URL and assigns it to the artwork data.
    /// </summary>
    private IEnumerator LoadImageFromURL(string imageUrl, ArtworkData artworkData)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            yield break;
        }
        
        // Check if URL is valid
        if (!imageUrl.StartsWith("http://") && !imageUrl.StartsWith("https://"))
        {
            // Not a valid URL, try loading from Resources instead
            Texture2D texture = Resources.Load<Texture2D>(imageUrl);
            if (texture != null)
            {
                artworkData.image = texture;
            }
            yield break;
        }
        
        Debug.Log($"ArtworkManager: Loading image from URL: {imageUrl}");
        
        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl);
        imageRequest.timeout = (int)apiTimeout;
        
        yield return imageRequest.SendWebRequest();
        
        if (imageRequest.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
            if (texture != null)
            {
                // Flip texture vertically to fix upside-down issue
                texture = FlipTextureVertically(texture);
                artworkData.image = texture;
                Debug.Log($"ArtworkManager: Successfully loaded image for '{artworkData.title}'");
            }
        }
        else
        {
            Debug.LogWarning($"ArtworkManager: Failed to load image from URL: {imageUrl}. Error: {imageRequest.error}");
        }
        
        imageRequest.Dispose();
    }
    
    /// <summary>
    /// Manually triggers loading artworks from API.
    /// Useful for refreshing data or loading on demand.
    /// </summary>
    public void ReloadArtworksFromAPI()
    {
        if (isLoadingFromAPI)
        {
            Debug.LogWarning("ArtworkManager: Already loading from API. Please wait.");
            return;
        }
        
        artworkDatabase.Clear();
        StartCoroutine(LoadArtworksFromAPI());
    }
    
    /// <summary>
    /// Sets the API URL and optionally reloads artworks.
    /// </summary>
    public void SetAPIUrl(string url, bool reloadImmediately = false)
    {
        apiUrl = url;
        if (reloadImmediately)
        {
            ReloadArtworksFromAPI();
        }
    }
    
    /// <summary>
    /// Adds a custom header to API requests.
    /// </summary>
    public void AddAPIHeader(string key, string value)
    {
        if (apiHeaders == null)
        {
            apiHeaders = new Dictionary<string, string>();
        }
        apiHeaders[key] = value;
    }
    
    /// <summary>
    /// Removes a custom header from API requests.
    /// </summary>
    public void RemoveAPIHeader(string key)
    {
        if (apiHeaders != null)
        {
            apiHeaders.Remove(key);
        }
    }
    
    /// <summary>
    /// Adds an artwork to the database.
    /// </summary>
    public void AddArtwork(ArtworkData artwork)
    {
        if (artwork != null && !artworkDatabase.Contains(artwork))
        {
            artworkDatabase.Add(artwork);
        }
    }
    
    /// <summary>
    /// Gets all artworks in the database.
    /// </summary>
    public List<ArtworkData> GetAllArtworks()
    {
        return new List<ArtworkData>(artworkDatabase);
    }
    
    /// <summary>
    /// Sets the list of placement transforms and optionally places artworks immediately.
    /// </summary>
    public void SetPlacementTransforms(List<Transform> transforms, bool placeImmediately = false)
    {
        placementTransforms = transforms;
        useCustomTransforms = transforms != null && transforms.Count > 0;
        
        if (placeImmediately && useCustomTransforms)
        {
            PlaceArtworksAtTransforms();
        }
    }
    
    /// <summary>
    /// Adds a transform to the placement list.
    /// </summary>
    public void AddPlacementTransform(Transform transform)
    {
        if (transform != null && !placementTransforms.Contains(transform))
        {
            placementTransforms.Add(transform);
            useCustomTransforms = true;
        }
    }
    
    /// <summary>
    /// Removes a transform from the placement list.
    /// </summary>
    public void RemovePlacementTransform(Transform transform)
    {
        if (placementTransforms != null)
        {
            placementTransforms.Remove(transform);
            useCustomTransforms = placementTransforms.Count > 0;
        }
    }
    
    /// <summary>
    /// Clears all placement transforms.
    /// </summary>
    public void ClearPlacementTransforms()
    {
        if (placementTransforms != null)
        {
            placementTransforms.Clear();
            useCustomTransforms = false;
        }
    }
}

// JSON serialization classes
[System.Serializable]
public class ArtworkConfig
{
    public ArtworkConfigEntry[] artworks;
}

[System.Serializable]
public class ArtworkConfigEntry
{
    public string title;
    public string artist;
    public string description;
    public int year;
    public string medium;
    public string category;
    public string imagePath;
    public string url;
}

