using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all artworks in the gallery.
/// Handles loading, placement, and interaction coordination.
/// </summary>
public class ArtworkManager : MonoBehaviour
{
    [Header("Artwork Configuration")]
    [SerializeField] private List<ArtworkData> artworkDatabase = new List<ArtworkData>();
    [SerializeField] private string jsonConfigPath = "ArtworkConfig.json";
    
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
    
    private void Awake()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        // Try to load from JSON if database is empty
        if (artworkDatabase.Count == 0)
        {
            LoadArtworksFromJSON();
        }
    }
    
    private void Start()
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
        }
        
        return frame;
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
        
        Debug.Log($"ArtworkManager: Placing {artworkDatabase.Count} artworks at {placementTransforms.Count} transforms...");
        
        int placedCount = 0;
        
        for (int i = 0; i < artworkDatabase.Count; i++)
        {
            ArtworkData artwork = artworkDatabase[i];
            if (artwork == null)
            {
                Debug.LogWarning($"ArtworkManager: Skipping null artwork at index {i}");
                continue;
            }
            
            // Get the corresponding transform (cycle through if more artworks than transforms)
            Transform placementTransform = placementTransforms[i % placementTransforms.Count];
            
            if (placementTransform == null)
            {
                Debug.LogWarning($"ArtworkManager: Placement transform at index {i % placementTransforms.Count} is null. Skipping artwork '{artwork.title}'.");
                continue;
            }
            
            // Place artwork at transform
            ArtworkFrame frame = PlaceArtwork(artwork, placementTransform);
            if (frame != null)
            {
                placedCount++;
            }
        }
        
        Debug.Log($"ArtworkManager: Successfully placed {placedCount} out of {artworkDatabase.Count} artworks at transforms.");
        
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
                frame.OnArtworkClicked += HandleArtworkClicked;
                frame.OnArtworkHovered += HandleArtworkHovered;
            }
        }
    }
    
    private void HandleArtworkClicked(ArtworkData artwork)
    {
        if (artwork == null)
        {
            Debug.LogWarning("ArtworkManager: Received click event with null artwork data.");
            return;
        }
        
        if (artworkUI != null)
        {
            Debug.Log($"ArtworkManager: Showing artwork '{artwork.title}' in UI.");
            artworkUI.ShowArtwork(artwork);
        }
        else
        {
            Debug.LogWarning($"ArtworkManager: ArtworkUI reference is not assigned! Clicked artwork: {artwork.title} by {artwork.artist}");
            Debug.LogWarning($"ArtworkManager: Please assign the ArtworkUI component to ArtworkManager in the Inspector.");
        }
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

