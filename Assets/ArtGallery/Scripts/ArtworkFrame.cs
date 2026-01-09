using UnityEngine;
using Utilities;
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
    
    [Header("Frame Settings (in Inches)")]
    [Tooltip("Bleeding (gap between artwork and frame inner edge) in inches.")]
    [SerializeField] private float bleedingInches = 0.5f;

    [Tooltip("Frame thickness (width of the frame border) in inches.")]
    [SerializeField] private float frameThicknessInches = 1.0f;

    [Tooltip("Frame depth (Z-axis protrusion) in inches.")]
    [SerializeField] private float frameDepthInches = 0.5f;

    /// <summary>
    /// Public accessors so other systems (e.g., InchWallGridData) can compute total occupied space in inches.
    /// </summary>
    public float BleedingInches => bleedingInches;
    public float FrameThicknessInches => frameThicknessInches;
    public float FrameDepthInches => frameDepthInches;

    [SerializeField] private Color frameColor = new Color(0.8f, 0.7f, 0.6f); // Gold/bronze color

    // Legacy fields (in Unity units/meters) - kept for backward-compatibility, not used.
    [HideInInspector] private float frameWidth = 0.1f;
    [HideInInspector] private float frameDepth = 0.05f;
    
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

            // Convert preferred size from inches to Unity units (meters) with high-precision utility
            Vector2 sizeInches = artworkData.preferredSizeInches;
            Vector2 size = new Vector2(sizeInches.x.FromInches(), sizeInches.y.FromInches());
            
            if (artworkData.maintainAspectRatio)
            {
                // Maintain aspect ratio, adjust height
                size.y = size.x / aspectRatio;
            }
            
            // Scale the artwork plane
            if (artworkPlane != null)
            {
                artworkPlane.transform.localScale = new Vector3(size.x, size.y, 0.01f);
                artworkPlane.transform.localPosition = new Vector3(0, 0, 0);
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
            //artworkPlane.transform.localPosition = new Vector3(0, 0, -frameDepth / 2);
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

        // Convert inches to Unity units (meters) for accurate placement
        float bleedingUnits = bleedingInches.FromInches();
        float frameThicknessUnits = frameThicknessInches.FromInches();
        float frameDepthUnits = frameDepthInches.FromInches();

        // Total border = bleeding + frame thickness
        float totalBorder = bleedingUnits + frameThicknessUnits;
        
        if (useFramePieces && frameTop != null && frameRight != null && frameLeft != null && frameBottom != null)
        {
            // Use separate frame pieces with constant X scale, adjusting only Y scale
            UpdateFramePiecesSize(artworkScale, bleedingUnits, frameThicknessUnits, frameDepthUnits);
        }
        else
        {
            // Original implementation: single frame mesh
            // Scale frame to be larger than artwork by bleeding + frame thickness on all sides
            if (frameMesh != null)
            {
                frameMesh.transform.localScale = new Vector3(
                    artworkScale.x + totalBorder * 2,
                    artworkScale.y + totalBorder * 2,
                    frameDepthUnits
                );
            }
        }
        
        // Update collider size to match full frame extent
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.size = new Vector3(
                artworkScale.x + totalBorder * 2,
                artworkScale.y + totalBorder * 2,
                frameDepthUnits
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
    /// bleedingUnits: gap between artwork edge and frame inner edge (Unity units).
    /// frameThicknessUnits: width of frame border (Unity units).
    /// frameDepthUnits: Z-axis depth (Unity units).
    /// </summary>
    private void UpdateFramePiecesSize(Vector3 artworkScale, float bleedingUnits, float frameThicknessUnits, float frameDepthUnits)
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
        
        // Total border on one side: bleeding + frame thickness
        float totalBorder = bleedingUnits + frameThicknessUnits;

        // Position the frame pieces around the artwork
        float halfWidth = artworkScale.x * 0.5f;
        float halfHeight = artworkScale.y * 0.5f;
        
        // Frame inner edge is offset from artwork by bleeding
        float innerOffset = bleedingUnits + frameThicknessUnits * 0.5f;
        
        // Top/Bottom pieces extend fully (overlapping corners)
        // Left/Right pieces fit between them and extend by 2 * bleeding to avoid visible gaps
        float topBottomY = artworkScale.x + totalBorder * 2f;         // Full width including corners
        float leftRightY = artworkScale.y + bleedingUnits * 2f;       // Height + 2 * bleed
        
        // Ensure we don't get negative values
        leftRightY = Mathf.Max(leftRightY, 0.01f);
        
        // Top piece: constant X scale (frame thickness), Y scale extends fully to overlap corners, Z = depth
        frameTop.transform.localPosition = new Vector3(0, halfHeight + innerOffset, 0);
        frameTop.transform.localScale = new Vector3(
            frameThicknessUnits, // Frame thickness
            topBottomY,          // Full width including corners (overlaps left/right)
            frameDepthUnits      // Frame depth
        );
        
        // Bottom piece: constant X scale, Y scale extends fully to overlap corners, Z = depth
        frameBottom.transform.localPosition = new Vector3(0, -halfHeight - innerOffset, 0);
        frameBottom.transform.localScale = new Vector3(
            frameThicknessUnits, // Frame thickness
            topBottomY,          // Full width including corners (overlaps left/right)
            frameDepthUnits      // Frame depth
        );
        
        // Left piece: constant X scale, Y scale fits between top/bottom pieces, Z = depth
        frameLeft.transform.localPosition = new Vector3(-halfWidth - innerOffset, 0, 0);
        frameLeft.transform.localScale = new Vector3(
            frameThicknessUnits, // Frame thickness
            leftRightY,          // Height fits between top/bottom
            frameDepthUnits      // Frame depth
        );
        
        // Right piece: constant X scale, Y scale fits between top/bottom pieces, Z = depth
        frameRight.transform.localPosition = new Vector3(halfWidth + innerOffset, 0, 0);
        frameRight.transform.localScale = new Vector3(
            frameThicknessUnits, // Frame thickness
            leftRightY,          // Height fits between top/bottom
            frameDepthUnits      // Frame depth
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

    /// <summary>
    /// Returns the total outer size of this artwork+frame in inches as it appears on the wall.
    /// Width/height are derived from the current artwork plane scale, then expanded by
    /// bleeding + frame thickness on each side.
    /// </summary>
    public Vector2 GetOuterSizeInches()
    {
        if (artworkPlane == null)
        {
            // Fallback to preferred size in inches if plane not yet created/scaled
            Vector2 baseInches = artworkData != null ? artworkData.preferredSizeInches : Vector2.zero;
            float border = bleedingInches + frameThicknessInches;
            return new Vector2(baseInches.x + border * 2f, baseInches.y + border * 2f);
        }

        Vector3 s = artworkPlane.transform.localScale;
        // Convert current artwork world size back to inches for high-accuracy accounting
        float artWidthInches = s.x.ToInches();
        float artHeightInches = s.y.ToInches();

        float borderInches = bleedingInches + frameThicknessInches;
        float totalWidthInches = artWidthInches + borderInches * 2f;
        float totalHeightInches = artHeightInches + borderInches * 2f;

        return new Vector2(totalWidthInches, totalHeightInches);
    }
}


