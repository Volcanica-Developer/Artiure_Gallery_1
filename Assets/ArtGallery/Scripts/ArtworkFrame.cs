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

    [Header("Debug JSON Data")]
    [Tooltip("Debug: PaintingConfigNew object coming from the JSON for this frame.")]
    [SerializeField] private PaintingConfigNew debugPaintingData;
    
    [Header("Frame Settings (in Inches)")]
    [Tooltip("Bleeding (gap between artwork and frame inner edge) in inches.")]
    [SerializeField] private float bleedingInches = 0.5f;

    [Tooltip("Frame thickness (width of the frame border) in inches.")]
    [SerializeField] private float frameThicknessInches = 1.0f;

    [Tooltip("Frame depth (Z-axis protrusion) in inches.")]
    [SerializeField] private float frameDepthInches = 0.5f;

    [Header("Frame Editing Helpers")]
    [Tooltip("Target OUTER width in inches for the 'Rebuild Frame To Outer Size' editor button.")]
    [SerializeField, HideInInspector] private float editorTargetOuterWidthInches = 0f;

    [Tooltip("Target OUTER height in inches for the 'Rebuild Frame To Outer Size' editor button.")]
    [SerializeField, HideInInspector] private float editorTargetOuterHeightInches = 0f;

    [Tooltip("If true, the frame editor will enforce an outer aspect ratio when rebuilding to outer size.")]
    [SerializeField, HideInInspector] private bool editorUseAspectRatio = false;

    [Tooltip("Aspect ratio width component (W in W:H) for outer size.")]
    [SerializeField, HideInInspector] private float editorAspectWidth = 1f;

    [Tooltip("Aspect ratio height component (H in W:H) for outer size.")]
    [SerializeField, HideInInspector] private float editorAspectHeight = 1f;

    [Header("Inspector Edge-to-Edge Helper (Inches)")]
    [Tooltip("If enabled, use the custom next artwork outer size below when computing edge-to-edge offsets in the inspector.")]
    [SerializeField] private bool useCustomNextArtworkSize = false;

    [Tooltip("Next artwork OUTER width in inches (including frame and bleeding). Used only for inspector calculations.")]
    [SerializeField] private float customNextArtworkOuterWidthInches = 0f;

    [Tooltip("Next artwork OUTER height in inches (including frame and bleeding). Used only for inspector calculations.")]
    [SerializeField] private float customNextArtworkOuterHeightInches = 0f;

    /// <summary>
    /// Public accessors so other systems (e.g., InchWallGridData) can compute total occupied space in inches.
    /// </summary>
    public float BleedingInches => bleedingInches;
    public float FrameThicknessInches => frameThicknessInches;
    public float FrameDepthInches => frameDepthInches;

    /// <summary>
    /// When true, the inspector will use the configured custom next artwork outer size
    /// (customNextArtworkOuterWidthInches/customNextArtworkOuterHeightInches) when
    /// computing edge-to-edge offsets.
    /// </summary>
    public bool UseCustomNextArtworkSize => useCustomNextArtworkSize;

    /// <summary>
    /// Returns the configured custom next artwork OUTER size in inches (including frame and bleeding).
    /// Values are clamped to non-negative for safety.
    /// </summary>
    public Vector2 CustomNextArtworkOuterSizeInches => new Vector2(
        Mathf.Max(0f, customNextArtworkOuterWidthInches),
        Mathf.Max(0f, customNextArtworkOuterHeightInches)
    );

    [SerializeField] private Color frameColor = new Color(0.8f, 0.7f, 0.6f); // Gold/bronze color

    // Legacy fields (in Unity units/meters) - kept for backward-compatibility, not used.
    [HideInInspector] private float frameWidth = 0.1f;
    [HideInInspector] private float frameDepth = 0.05f;
    
    [Header("Interaction")]
    // Hover scale disabled - artworks spawn without scaling effects
    // [SerializeField] private float hoverScale = 1.05f;
    // [SerializeField] private float hoverTransitionSpeed = 5f;

    [Header("Info Screen Interaction (New JSON)")]
    [Tooltip("Maximum horizontal distance (in meters) between the player and the frame's standing position on the wall to treat a click as an 'info' click.")]
    [SerializeField] private float infoClickMaxDistance = 0.1f;
    
    private Material artworkMatInstance;
    // private Vector3 originalScale;
    // private Vector3 targetScale;
    // private bool isHovered = false;
    
    // Events
    public System.Action<ArtworkData> OnArtworkClicked;
    public System.Action<ArtworkData> OnArtworkHovered;
    
    /// <summary>
    /// Exposes the JSON painting object that was assigned to this frame (for debugging).
    /// </summary>
    public PaintingConfigNew DebugPaintingData => debugPaintingData;

    /// <summary>
    /// Assigns the JSON painting object to this frame for debugging/inspection.
    /// </summary>
    public void SetDebugPaintingData(PaintingConfigNew painting)
    {
        debugPaintingData = painting;
    }
    
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
    /// Sets the artwork to display in this frame using the legacy ArtworkData pipeline.
    /// This path can optionally resize the artwork plane and frame to match preferred size.
    /// For the new JSON-driven exhibition pipeline, prefer SetTexture() instead so that
    /// the prefab-authored size remains unchanged.
    /// </summary>
    public void SetArtwork(ArtworkData data)
    {
        artworkData = data;
        if (artworkData == null) return;

        // Derive a texture from the ArtworkData
        Texture2D texture = artworkData.image;
        if (texture == null && artworkData.sprite != null)
        {
            texture = artworkData.sprite.texture;
        }

        // Use the generic texture-based path
        SetTexture(texture);

        // Optionally: if you still want ArtworkData to drive physical size, you can
        // extend this method to adjust artworkPlane scale and call UpdateFrameSize().
        // For the new exhibition flow, size is kept as defined in the prefab.
    }

    /// <summary>
    /// Sets a texture on the artwork plane without modifying its scale.
    /// This is the preferred method for the JSON exhibition pipeline: the
    /// frame and artworkPlane keep whatever size is authored in the prefab.
    /// </summary>
    public void SetTexture(Texture2D texture)
    {
        if (artworkPlane == null)
        {
            // Ensure underlying geometry exists
            SetupFrame();
        }

        if (texture == null)
        {
            ClearTexture();
            return;
        }

        // Get or create artwork material
        if (artworkMatInstance == null)
        {
            if (artworkMaterial != null)
            {
                artworkMatInstance = new Material(artworkMaterial);
            }
            else
            {
                artworkMatInstance = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
        }

        artworkMatInstance.mainTexture = texture;

        // Apply material to artwork plane
        Renderer renderer = artworkPlane.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = artworkMatInstance;
        }

        // Important: DO NOT change artworkPlane scale or frame size here; these are
        // now controlled by the prefab so all artworks in a layout use consistent size.
    }

    /// <summary>
    /// Clears any texture from the artwork plane (used when there are more frames than paintings).
    /// </summary>
    public void ClearTexture()
    {
        if (artworkPlane == null)
            return;

        Renderer renderer = artworkPlane.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null && renderer.material.HasProperty("_MainTex"))
        {
            renderer.material.mainTexture = null;
        }
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
    
    /// <summary>
    /// Rebuilds the frame pieces so that the OUTER size of the frame stays the same
    /// (based on the current collider/frame mesh), but the inner artwork plane and
    /// border positions are recomputed using the current bleeding/frameThickness.
    ///
    /// This means:
    /// - if you DECREASE bleeding, the artwork area GROWS and the frame effectively
    ///   moves outward to keep the same outer bounds;
    /// - if you INCREASE bleeding, the artwork area shrinks accordingly but the
    ///   outside size is unchanged.
    /// </summary>
    public void RebuildFrameFromArtwork()
    {
        if (artworkPlane == null)
        {
            Debug.LogWarning("ArtworkFrame: Cannot rebuild frame; artworkPlane is missing.");
            return;
        }

        // Prefer the collider as the source of truth for current outer size in Unity units
        float outerWidthUnits;
        float outerHeightUnits;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            outerWidthUnits = Mathf.Max(0.01f, col.size.x);
            outerHeightUnits = Mathf.Max(0.01f, col.size.y);
        }
        else if (frameMesh != null)
        {
            Vector3 s = frameMesh.transform.localScale;
            outerWidthUnits = Mathf.Max(0.01f, s.x);
            outerHeightUnits = Mathf.Max(0.01f, s.y);
        }
        else
        {
            // Fallback: use logical outer size from artwork + bleeding + frameThickness
            Vector2 outerInches = GetOuterSizeInches();
            outerWidthUnits = Mathf.Max(0.01f, outerInches.x.FromInches());
            outerHeightUnits = Mathf.Max(0.01f, outerInches.y.FromInches());
        }

        // Convert current bleeding/frame thickness to Unity units
        float bleedingUnits = Mathf.Max(0f, bleedingInches).FromInches();
        float frameThicknessUnits = Mathf.Max(0f, frameThicknessInches).FromInches();
        float borderPerSideUnits = bleedingUnits + frameThicknessUnits;

        // New artwork size in units so that outer stays the same:
        // outer = artwork + 2 * borderPerSide  =>  artwork = outer - 2 * borderPerSide
        float newArtWidthUnits = Mathf.Max(0.01f, outerWidthUnits - 2f * borderPerSideUnits);
        float newArtHeightUnits = Mathf.Max(0.01f, outerHeightUnits - 2f * borderPerSideUnits);

        Vector3 currentScale = artworkPlane.transform.localScale;
        artworkPlane.transform.localScale = new Vector3(newArtWidthUnits, newArtHeightUnits, currentScale.z);
        // Now recompute frame pieces/collider around the new artwork size.
        UpdateFrameSize();
    }

    /// <summary>
    /// Rebuilds the entire frame to a specific OUTER size, given in inches.
    /// The new outer size is respected exactly (within unit conversion), and
    /// the inner artwork is resized based on the current bleeding and frame
    /// thickness.
    /// </summary>
    public void RebuildFrameToOuterSize(float targetOuterWidthInches, float targetOuterHeightInches)
    {
        if (artworkPlane == null)
        {
            Debug.LogWarning("ArtworkFrame: Cannot rebuild frame to outer size; artworkPlane is missing.");
            return;
        }

        targetOuterWidthInches = Mathf.Max(0.01f, targetOuterWidthInches);
        targetOuterHeightInches = Mathf.Max(0.01f, targetOuterHeightInches);

        // Store for inspector convenience
        editorTargetOuterWidthInches = targetOuterWidthInches;
        editorTargetOuterHeightInches = targetOuterHeightInches;

        float outerWidthUnits = targetOuterWidthInches.FromInches();
        float outerHeightUnits = targetOuterHeightInches.FromInches();

        float bleedingUnits = Mathf.Max(0f, bleedingInches).FromInches();
        float frameThicknessUnits = Mathf.Max(0f, frameThicknessInches).FromInches();
        float borderPerSideUnits = bleedingUnits + frameThicknessUnits;

        // Compute artwork size from desired outer size
        float newArtWidthUnits = Mathf.Max(0.01f, outerWidthUnits - 2f * borderPerSideUnits);
        float newArtHeightUnits = Mathf.Max(0.01f, outerHeightUnits - 2f * borderPerSideUnits);

        Vector3 currentScale = artworkPlane.transform.localScale;
        artworkPlane.transform.localScale = new Vector3(newArtWidthUnits, newArtHeightUnits, currentScale.z);

        // Refresh frame pieces and collider to match the new outer size
        UpdateFrameSize();
    }

    /// <summary>
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
        // Legacy pipeline: still forward ArtworkData-based click events.
        OnArtworkClicked?.Invoke(artworkData);

        // New JSON pipeline: when the player clicks this frame, send the PaintingConfigNew
        // for this frame to the active Information UI (desktop or mobile).
        TrySendJsonPaintingToInformationScreen();
    }

    /// <summary>
    /// New JSON-driven exhibition pipeline: as soon as this frame is clicked (via
    /// pointer interaction), push its PaintingConfigNew into the appropriate
    /// information UI (desktop or mobile) using InformationScreenRouter if present.
    /// </summary>
    private void TrySendJsonPaintingToInformationScreen()
    {
        // Require JSON painting data on this frame.
        if (debugPaintingData == null)
            return;

        // Prefer the router so we can choose between desktop and mobile UIs via inspector.
        if (InformationScreenRouter.Instance != null)
        {
            InformationScreenRouter.Instance.SetPaintingOnActive(debugPaintingData);
            return;
        }

        // Fallback: direct singleton if no router is configured.
        if (InformationScreenUiManager.Instance != null)
        {
            InformationScreenUiManager.Instance.SetPainting(debugPaintingData);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up material instance
        if (artworkMatInstance != null)
        {
            Destroy(artworkMatInstance);
        }
    }

    public float EditorTargetOuterWidthInches
    {
        get => editorTargetOuterWidthInches;
        set => editorTargetOuterWidthInches = Mathf.Max(0f, value);
    }

    public float EditorTargetOuterHeightInches
    {
        get => editorTargetOuterHeightInches;
        set => editorTargetOuterHeightInches = Mathf.Max(0f, value);
    }

    public bool EditorUseAspectRatio
    {
        get => editorUseAspectRatio;
        set => editorUseAspectRatio = value;
    }

    public float EditorAspectWidth
    {
        get => editorAspectWidth;
        set => editorAspectWidth = Mathf.Max(0.0001f, value);
    }

    public float EditorAspectHeight
    {
        get => editorAspectHeight;
        set => editorAspectHeight = Mathf.Max(0.0001f, value);
    }

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

    /// <summary>
    /// Computes how far, in inches, the center of a NEXT artwork needs to move in X/Y
    /// to be placed exactly edge-to-edge with THIS artwork+frame.
    ///
    /// If UseCustomNextArtworkSize is false, this assumes the next artwork's outer size
    /// is identical to this frame's outer size, so the offset is simply this outer
    /// width/height.
    ///
    /// If UseCustomNextArtworkSize is true, it will use CustomNextArtworkOuterSizeInches
    /// as the "other" outer size and compute the center-to-center spacing as:
    ///   delta = (thisOuter + otherOuter) / 2
    /// </summary>
    public Vector2 GetEdgeToEdgeOffsetInches()
    {
        Vector2 thisOuter = GetOuterSizeInches();

        if (!UseCustomNextArtworkSize)
        {
            // Same outer size: move by this outer width/height
            return thisOuter;
        }

        Vector2 otherOuter = CustomNextArtworkOuterSizeInches;
        float deltaX = 0.5f * (thisOuter.x + otherOuter.x);
        float deltaY = 0.5f * (thisOuter.y + otherOuter.y);
        return new Vector2(deltaX, deltaY);
    }
}


