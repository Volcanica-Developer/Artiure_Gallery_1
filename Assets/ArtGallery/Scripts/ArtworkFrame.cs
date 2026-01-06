using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Component that displays an artwork on a wall.
/// Handles placement, framing, and interaction.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArtworkFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Frame Components")]
    [SerializeField] private GameObject frameMesh;
    [SerializeField] private GameObject artworkPlane;
    [SerializeField] private Material frameMaterial;
    [SerializeField] private Material artworkMaterial;
    
    [Header("Frame Pieces (Optional - for separate top/right/left/bottom)")]
    [SerializeField] private bool useFramePieces = false;
    [SerializeField] private GameObject frameTop;
    [SerializeField] private GameObject frameRight;
    [SerializeField] private GameObject frameLeft;
    [SerializeField] private GameObject frameBottom;
    
    [Header("Artwork Data")]
    [SerializeField] private ArtworkData artworkData;
    
    [Header("Frame Settings")]
    [SerializeField] private float frameWidth = 0.1f;
    [SerializeField] private float frameDepth = 0.05f;
    [SerializeField] private Color frameColor = new Color(0.8f, 0.7f, 0.6f); // Gold/bronze color
    
    [Header("Interaction")]
    // Hover scale disabled - artworks spawn without scaling effects
    // [SerializeField] private float hoverScale = 1.05f;
    // [SerializeField] private float hoverTransitionSpeed = 5f;
    
    private Material artworkMatInstance;
    // private Vector3 originalScale;
    // private Vector3 targetScale;
    // private bool isHovered = false;
    
    // Events
    public System.Action<ArtworkData> OnArtworkClicked;
    public System.Action<ArtworkData> OnArtworkHovered;
    
    private void Awake()
    {
        // originalScale = transform.localScale;
        // targetScale = originalScale;
        
        // Ensure we have a collider for raycasting
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        SetupFrame();
    }
    
    private void Start()
    {
        if (artworkData != null)
        {
            SetArtwork(artworkData);
        }
    }
    
    // Hover scale animation removed - artworks spawn without scaling
    // private void Update()
    // {
    //     // Smooth hover scale animation
    //     if (transform.localScale != targetScale)
    //     {
    //         transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * hoverTransitionSpeed);
    //     }
    // }
    
    /// <summary>
    /// Sets the artwork to display in this frame.
    /// </summary>
    public void SetArtwork(ArtworkData data)
    {
        artworkData = data;
        
        if (artworkData == null) return;
        
        // Get or create artwork material
        if (artworkMaterial != null)
        {
            artworkMatInstance = new Material(artworkMaterial);
        }
        else
        {
            artworkMatInstance = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        
        // Set the texture
        Texture2D texture = artworkData.image;
        if (texture == null && artworkData.sprite != null)
        {
            texture = artworkData.sprite.texture;
        }
        
        if (texture != null)
        {
            artworkMatInstance.mainTexture = texture;
            
            // Calculate aspect ratio and adjust size
            float aspectRatio = (float)texture.width / texture.height;
            Vector2 size = artworkData.preferredSize;
            
            if (artworkData.maintainAspectRatio)
            {
                // Maintain aspect ratio, adjust height
                size.y = size.x / aspectRatio;
            }
            
            // Scale the artwork plane
            if (artworkPlane != null)
            {
                artworkPlane.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }
        
        // Apply material to artwork plane
        if (artworkPlane != null)
        {
            Renderer renderer = artworkPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = artworkMatInstance;
            }
        }
        
        // Update frame size to match artwork
        UpdateFrameSize();
    }
    
    private void SetupFrame()
    {
        // Create frame if it doesn't exist
        if (frameMesh == null)
        {
            frameMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameMesh.name = "Frame";
            frameMesh.transform.SetParent(transform);
            frameMesh.transform.localPosition = Vector3.zero;
            
            // Remove the collider from frame (we use parent collider)
            Collider frameCollider = frameMesh.GetComponent<Collider>();
            if (frameCollider != null)
            {
                DestroyImmediate(frameCollider);
            }
        }
        
        // Create artwork plane if it doesn't exist
        if (artworkPlane == null)
        {
            artworkPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            artworkPlane.name = "Artwork";
            artworkPlane.transform.SetParent(transform);
            artworkPlane.transform.localPosition = new Vector3(0, 0, -frameDepth / 2);
            artworkPlane.transform.localRotation = Quaternion.identity;
            
            // Remove collider from artwork plane
            Collider planeCollider = artworkPlane.GetComponent<Collider>();
            if (planeCollider != null)
            {
                DestroyImmediate(planeCollider);
            }
        }
        
        // Setup frame material
        if (frameMaterial == null)
        {
            frameMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            frameMaterial.color = frameColor;
        }
        
        Renderer frameRenderer = frameMesh.GetComponent<Renderer>();
        if (frameRenderer != null)
        {
            frameRenderer.material = frameMaterial;
        }
    }
    
    private void UpdateFrameSize()
    {
        if (artworkData == null || artworkPlane == null) return;
        
        Vector3 artworkScale = artworkPlane.transform.localScale;
        float frameThickness = frameWidth;
        
        if (useFramePieces && frameTop != null && frameRight != null && frameLeft != null && frameBottom != null)
        {
            // Use separate frame pieces with constant X scale, adjusting only Y scale
            UpdateFramePiecesSize(artworkScale, frameThickness);
        }
        else
        {
            // Original implementation: single frame mesh
            // Scale frame to be slightly larger than artwork
            if (frameMesh != null)
            {
                frameMesh.transform.localScale = new Vector3(
                    artworkScale.x + frameThickness * 2,
                    artworkScale.y + frameThickness * 2,
                    frameDepth
                );
            }
        }
        
        // Update collider size
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.size = new Vector3(
                artworkScale.x + frameThickness * 2,
                artworkScale.y + frameThickness * 2,
                frameDepth
            );
            collider.center = Vector3.zero;
        }
    }
    
    // Store original X and Z scales to keep them constant
    private float originalTopX = -1f;
    private float originalRightX = -1f;
    private float originalLeftX = -1f;
    private float originalBottomX = -1f;
    private float originalTopZ = -1f;
    private float originalRightZ = -1f;
    private float originalLeftZ = -1f;
    private float originalBottomZ = -1f;
    
    /// <summary>
    /// Updates the size of frame pieces (top, right, left, bottom).
    /// Keeps X and Z scales constant and adjusts only Y scale.
    /// </summary>
    private void UpdateFramePiecesSize(Vector3 artworkScale, float frameThickness)
    {
        // Store original X and Z scales on first call (they remain constant)
        if (originalTopX < 0f)
        {
            originalTopX = frameTop.transform.localScale.x;
            originalRightX = frameRight.transform.localScale.x;
            originalLeftX = frameLeft.transform.localScale.x;
            originalBottomX = frameBottom.transform.localScale.x;
            
            originalTopZ = frameTop.transform.localScale.z;
            originalRightZ = frameRight.transform.localScale.z;
            originalLeftZ = frameLeft.transform.localScale.z;
            originalBottomZ = frameBottom.transform.localScale.z;
        }
        
        // Position the frame pieces around the artwork
        float halfWidth = artworkScale.x * 0.5f;
        float halfHeight = artworkScale.y * 0.5f;
        float halfThickness = frameThickness * 0.5f;
        
        // Top/Bottom pieces extend fully (overlapping corners)
        // Left/Right pieces fit between them (shorter to avoid overlap)
        float topBottomY = artworkScale.x + frameThickness * 1.55f; // Full width including corners
        float leftRightY = artworkScale.y - (originalTopX + originalBottomX); // Height minus top/bottom thickness
        
        // Ensure we don't get negative values
        leftRightY = Mathf.Max(leftRightY, 0.01f);
        
        // Top piece: constant X and Z scale, Y scale extends fully to overlap corners
        frameTop.transform.localPosition = new Vector3(0, halfHeight + halfThickness, 0);
        frameTop.transform.localScale = new Vector3(
            originalTopX, // Keep X constant
            topBottomY, // Full width including corners (overlaps left/right)
            originalTopZ // Keep Z constant
        );
        
        // Bottom piece: constant X and Z scale, Y scale extends fully to overlap corners
        frameBottom.transform.localPosition = new Vector3(0, -halfHeight - halfThickness, 0);
        frameBottom.transform.localScale = new Vector3(
            originalBottomX, // Keep X constant
            topBottomY, // Full width including corners (overlaps left/right)
            originalBottomZ // Keep Z constant
        );
        
        // Left piece: constant X and Z scale, Y scale fits between top/bottom pieces
        frameLeft.transform.localPosition = new Vector3(-halfWidth - halfThickness, 0, 0);
        frameLeft.transform.localScale = new Vector3(
            originalLeftX, // Keep X constant
            leftRightY, // Height minus top/bottom thickness (fits between them)
            originalLeftZ // Keep Z constant
        );
        
        // Right piece: constant X and Z scale, Y scale fits between top/bottom pieces
        frameRight.transform.localPosition = new Vector3(halfWidth + halfThickness, 0, 0);
        frameRight.transform.localScale = new Vector3(
            originalRightX, // Keep X constant
            leftRightY, // Height minus top/bottom thickness (fits between them)
            originalRightZ // Keep Z constant
        );
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Hover scale removed - no visual effects on hover
        // isHovered = true;
        // targetScale = originalScale * hoverScale;
        OnArtworkHovered?.Invoke(artworkData);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hover scale removed - no visual effects on hover
        // isHovered = false;
        // targetScale = originalScale;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnArtworkClicked?.Invoke(artworkData);
    }
    
    private void OnDestroy()
    {
        // Clean up material instance
        if (artworkMatInstance != null)
        {
            Destroy(artworkMatInstance);
        }
    }
}


