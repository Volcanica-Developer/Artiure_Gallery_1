using UnityEngine;

/// <summary>
/// Utility script to build gallery rooms programmatically.
/// Creates walls, floors, and basic gallery structure.
/// </summary>
public class GalleryBuilder : MonoBehaviour
{
    [Header("Room Settings")]
    [SerializeField] private Vector3 roomSize = new Vector3(10f, 3f, 10f); // Width, Height, Depth
    [SerializeField] private float wallThickness = 0.2f;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material ceilingMaterial;
    
    [Header("Lighting")]
    [SerializeField] private bool createLighting = true;
    [SerializeField] private Light mainLight;
    [SerializeField] private Color ambientColor = new Color(0.5f, 0.5f, 0.6f);
    [SerializeField] private float lightIntensity = 1.2f;
    
    [Header("Artwork Placement")]
    [SerializeField] private bool createArtworkPlaceholders = true;
    [SerializeField] private float artworkSpacing = 3f;
    [SerializeField] private float artworkHeight = 1.5f; // Height from floor
    [SerializeField] private bool autoSnapPlaceholders = true; // Automatically snap placeholders to walls
    [SerializeField] private float placeholderOffsetFromWall = 0.02f; // Distance from wall surface (in meters)
    
    private GameObject roomParent;
    
    /// <summary>
    /// Builds a complete gallery room.
    /// </summary>
    [ContextMenu("Build Gallery Room")]
    public void BuildRoom()
    {
        // Create parent object
        roomParent = new GameObject("Gallery Room");
        roomParent.transform.SetParent(transform);
        
        // Build walls
        BuildWalls();
        
        // Build floor
        BuildFloor();
        
        // Build ceiling
        BuildCeiling();
        
        // Setup lighting
        if (createLighting)
        {
            SetupLighting();
        }
        
        // Create artwork placeholders
        if (createArtworkPlaceholders)
        {
            CreateArtworkPlaceholders();
        }
    }
    
    private void BuildWalls()
    {
        float halfWidth = roomSize.x / 2f;
        float halfDepth = roomSize.z / 2f;
        float height = roomSize.y;
        
        // Front wall
        CreateWall("Front Wall", new Vector3(0, height / 2, halfDepth), new Vector3(roomSize.x, height, wallThickness), Vector3.forward);
        
        // Back wall
        CreateWall("Back Wall", new Vector3(0, height / 2, -halfDepth), new Vector3(roomSize.x, height, wallThickness), Vector3.back);
        
        // Left wall
        CreateWall("Left Wall", new Vector3(-halfWidth, height / 2, 0), new Vector3(wallThickness, height, roomSize.z), Vector3.left);
        
        // Right wall
        CreateWall("Right Wall", new Vector3(halfWidth, height / 2, 0), new Vector3(wallThickness, height, roomSize.z), Vector3.right);
    }
    
    private void CreateWall(string name, Vector3 position, Vector3 scale, Vector3 normal)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(roomParent.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.tag = "Wall";
        wall.layer = 6; // Set to layer 6 (can be configured)
        
        // Apply material
        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer != null && wallMaterial != null)
        {
            renderer.material = wallMaterial;
        }
    }
    
    private void BuildFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(roomParent.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(roomSize.x / 10f, 1f, roomSize.z / 10f);
        floor.tag = "Floor";
        
        // Apply material
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null && floorMaterial != null)
        {
            renderer.material = floorMaterial;
        }
    }
    
    private void BuildCeiling()
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(roomParent.transform);
        ceiling.transform.position = new Vector3(0, roomSize.y, 0);
        ceiling.transform.localScale = new Vector3(roomSize.x / 10f, 1f, roomSize.z / 10f);
        ceiling.transform.Rotate(180f, 0, 0);
        
        // Apply material
        Renderer renderer = ceiling.GetComponent<Renderer>();
        if (renderer != null && ceilingMaterial != null)
        {
            renderer.material = ceilingMaterial;
        }
    }
    
    private void SetupLighting()
    {
        // Main directional light
        if (mainLight == null)
        {
            GameObject lightObject = new GameObject("Main Light");
            lightObject.transform.SetParent(roomParent.transform);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            mainLight = lightObject.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.intensity = lightIntensity;
            mainLight.color = Color.white;
            mainLight.shadows = LightShadows.Soft;
        }
        
        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientColor;
        RenderSettings.ambientEquatorColor = ambientColor * 0.8f;
        RenderSettings.ambientGroundColor = ambientColor * 0.6f;
    }
    
    private void CreateArtworkPlaceholders()
    {
        float halfWidth = roomSize.x / 2f - 1f; // Leave some margin
        float halfDepth = roomSize.z / 2f - 1f;
        
        // Create placeholder transforms for artwork placement
        GameObject placeholdersParent = new GameObject("Artwork Placeholders");
        placeholdersParent.transform.SetParent(roomParent.transform);
        
        // Front wall placeholders - position closer to wall surface
        CreatePlaceholdersOnWall(placeholdersParent, new Vector3(0, artworkHeight, halfDepth), Vector3.right, halfWidth * 2f);
        
        // Back wall placeholders
        CreatePlaceholdersOnWall(placeholdersParent, new Vector3(0, artworkHeight, -halfDepth), Vector3.right, halfWidth * 2f);
        
        // Left wall placeholders
        CreatePlaceholdersOnWall(placeholdersParent, new Vector3(-halfWidth, artworkHeight, 0), Vector3.forward, halfDepth * 2f);
        
        // Right wall placeholders
        CreatePlaceholdersOnWall(placeholdersParent, new Vector3(halfWidth, artworkHeight, 0), Vector3.forward, halfDepth * 2f);
    }
    
    private void CreatePlaceholdersOnWall(GameObject parent, Vector3 center, Vector3 direction, float wallLength)
    {
        int count = Mathf.FloorToInt(wallLength / artworkSpacing);
        float startOffset = -(count - 1) * artworkSpacing / 2f;
        
        // Find walls in the scene to snap to
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        LayerMask wallLayer = 1 << 6; // Default layer 6 for walls
        
        for (int i = 0; i < count; i++)
        {
            GameObject placeholder = new GameObject($"Artwork Placeholder {i + 1}");
            placeholder.transform.SetParent(parent.transform);
            
            // Initial position
            Vector3 initialPosition = center + direction * (startOffset + i * artworkSpacing);
            Vector3 rayDirection = -direction; // Raycast towards the wall
            
            // Raycast to find the wall surface
            // Start raycast from a point closer to where the wall should be
            // Use the wall's bounds if available to get a better starting point
            Vector3 rayStart = initialPosition;
            if (walls.Length > 0)
            {
                // Find the closest wall to use for better positioning
                GameObject closestWall = null;
                float closestDist = float.MaxValue;
                foreach (GameObject wall in walls)
                {
                    Renderer wallRenderer = wall.GetComponent<Renderer>();
                    if (wallRenderer != null)
                    {
                        float dist = Vector3.Distance(initialPosition, wallRenderer.bounds.ClosestPoint(initialPosition));
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestWall = wall;
                        }
                    }
                }
                
                // If we found a wall, start raycast from its surface
                if (closestWall != null)
                {
                    Renderer wallRenderer = closestWall.GetComponent<Renderer>();
                    if (wallRenderer != null)
                    {
                        // Project initial position onto wall plane
                        Bounds wallBounds = wallRenderer.bounds;
                        Vector3 wallCenter = wallBounds.center;
                        Vector3 wallNormal = -closestWall.transform.forward;
                        
                        // Calculate position on wall surface
                        Vector3 toInitial = initialPosition - wallCenter;
                        float projection = Vector3.Dot(toInitial, wallNormal);
                        rayStart = initialPosition - wallNormal * projection;
                    }
                }
            }
            
            RaycastHit hit;
            bool snapped = false;
            if (Physics.Raycast(rayStart, rayDirection, out hit, 5f, wallLayer))
            {
                // Snap to wall surface with minimal offset
                placeholder.transform.position = hit.point + hit.normal * placeholderOffsetFromWall;
                placeholder.transform.rotation = Quaternion.LookRotation(-hit.normal);
                snapped = true;
            }
            else
            {
                // Try reverse direction
                if (Physics.Raycast(rayStart, -rayDirection, out hit, 5f, wallLayer))
                {
                    placeholder.transform.position = hit.point + hit.normal * placeholderOffsetFromWall;
                    placeholder.transform.rotation = Quaternion.LookRotation(-hit.normal);
                    snapped = true;
                }
                else
                {
                    // Fallback: project onto wall surface using wall bounds
                    if (walls.Length > 0)
                    {
                        GameObject closestWall = null;
                        float closestDist = float.MaxValue;
                        foreach (GameObject wall in walls)
                        {
                            Renderer wallRenderer = wall.GetComponent<Renderer>();
                            if (wallRenderer != null)
                            {
                                float dist = Vector3.Distance(initialPosition, wallRenderer.bounds.ClosestPoint(initialPosition));
                                if (dist < closestDist)
                                {
                                    closestDist = dist;
                                    closestWall = wall;
                                }
                            }
                        }
                        
                        if (closestWall != null)
                        {
                            Renderer wallRenderer = closestWall.GetComponent<Renderer>();
                            if (wallRenderer != null)
                            {
                                Bounds wallBounds = wallRenderer.bounds;
                                Vector3 wallNormal = -closestWall.transform.forward;
                                Vector3 wallSurfacePoint = wallBounds.ClosestPoint(initialPosition);
                                
                                // Project onto wall surface
                                placeholder.transform.position = wallSurfacePoint + wallNormal * placeholderOffsetFromWall;
                                placeholder.transform.rotation = Quaternion.LookRotation(-wallNormal);
                                snapped = true;
                            }
                        }
                    }
                    
                    if (!snapped)
                    {
                        // Final fallback: use initial position and rotation
                        placeholder.transform.position = initialPosition;
                        placeholder.transform.rotation = Quaternion.LookRotation(-direction);
                    }
                }
            }
            
            // Add snapper component if auto-snap is enabled (for later re-snapping if walls move)
            if (autoSnapPlaceholders)
            {
                ArtworkPlaceholderSnapper snapper = placeholder.AddComponent<ArtworkPlaceholderSnapper>();
                if (!snapped)
                {
                    // If we couldn't snap initially, try again on Start
                    // (useful if walls are created after placeholders)
                }
            }
        }
    }
    
    /// <summary>
    /// Clears the built room.
    /// </summary>
    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        if (roomParent != null)
        {
            DestroyImmediate(roomParent);
            roomParent = null;
        }
    }
}

