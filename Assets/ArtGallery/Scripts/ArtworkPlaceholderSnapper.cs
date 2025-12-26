using UnityEngine;

/// <summary>
/// Utility component to snap artwork placeholders to walls.
/// Attach to placeholder GameObjects or call SnapAllPlaceholders() to snap all placeholders in scene.
/// </summary>
public class ArtworkPlaceholderSnapper : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private float maxSnapDistance = 10f;
    [SerializeField] private float offsetFromWall = 0.02f; // Distance from wall surface (in meters) - reduced for closer placement
    [SerializeField] private LayerMask wallLayer = 1 << 6; // Default layer 6 for walls
    
    /// <summary>
    /// Snaps this placeholder to the nearest wall it's facing.
    /// </summary>
    [ContextMenu("Snap To Wall")]
    public void SnapToWall()
    {
        Transform placeholderTransform = transform;
        Vector3 rayOrigin = placeholderTransform.position;
        Vector3 rayDirection = -placeholderTransform.forward; // Raycast in the direction the placeholder is facing
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxSnapDistance, wallLayer))
        {
            // Found a wall - snap to it
            placeholderTransform.position = hit.point + hit.normal * offsetFromWall;
            placeholderTransform.rotation = Quaternion.LookRotation(-hit.normal);
            Debug.Log($"Snapped placeholder '{gameObject.name}' to wall '{hit.collider.name}'");
        }
        else
        {
            // Try reverse direction
            if (Physics.Raycast(rayOrigin, -rayDirection, out hit, maxSnapDistance, wallLayer))
            {
                placeholderTransform.position = hit.point + hit.normal * offsetFromWall;
                placeholderTransform.rotation = Quaternion.LookRotation(-hit.normal);
                Debug.Log($"Snapped placeholder '{gameObject.name}' to wall '{hit.collider.name}' (reverse direction)");
            }
            else
            {
                Debug.LogWarning($"Could not find a wall to snap placeholder '{gameObject.name}' to within {maxSnapDistance} units.");
            }
        }
    }
    
    /// <summary>
    /// Snaps all placeholders in the scene to walls.
    /// </summary>
    [ContextMenu("Snap All Placeholders")]
    public static void SnapAllPlaceholders()
    {
        // Find all placeholders (objects with "Placeholder" in name or ArtworkPlaceholderSnapper component)
        ArtworkPlaceholderSnapper[] snappers = FindObjectsOfType<ArtworkPlaceholderSnapper>();
        int snappedCount = 0;
        
        foreach (ArtworkPlaceholderSnapper snapper in snappers)
        {
            snapper.SnapToWall();
            snappedCount++;
        }
        
        // Also try to find placeholders by name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Placeholder") && obj.GetComponent<ArtworkPlaceholderSnapper>() == null)
            {
                ArtworkPlaceholderSnapper tempSnapper = obj.AddComponent<ArtworkPlaceholderSnapper>();
                tempSnapper.SnapToWall();
                snappedCount++;
            }
        }
        
        Debug.Log($"Snapped {snappedCount} placeholders to walls.");
    }
    
    private void Start()
    {
        // Auto-snap on start if component is enabled
        if (enabled)
        {
            SnapToWall();
        }
    }
}

