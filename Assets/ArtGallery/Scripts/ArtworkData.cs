using UnityEngine;

/// <summary>
/// ScriptableObject to store artwork information.
/// Can be created via Assets > Create > Art Gallery > Artwork Data
/// </summary>
[CreateAssetMenu(fileName = "New Artwork", menuName = "Art Gallery/Artwork Data")]
public class ArtworkData : ScriptableObject
{
    [Header("Artwork Information")]
    public string title = "Untitled";
    public string artist = "Unknown Artist";
    [TextArea(3, 10)]
    public string description = "No description available.";
    public int year = 2024;
    
    [Header("Artwork Image")]
    public Texture2D image;
    public Sprite sprite; // Alternative to Texture2D
    
    [Header("Display Settings (Size in Inches)")]
    [Tooltip("Preferred artwork size in inches (Width x Height). This will be converted to Unity units (meters) at runtime.")]
    public Vector2 preferredSizeInches = new Vector2(20f, 30f); // Width x Height in inches
    public bool maintainAspectRatio = true;

    // Legacy field kept for backward-compatibility (was meters). Not used anymore.
    [HideInInspector]
    public Vector2 preferredSize = new Vector2(1f, 1.5f);
    
    [Header("Additional Info")]
    public string medium = "Digital";
    public string category = "General";
    public string url; // Optional link to more info
}




