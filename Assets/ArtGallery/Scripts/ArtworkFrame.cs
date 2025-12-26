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
        
        // Scale frame to be slightly larger than artwork
        frameMesh.transform.localScale = new Vector3(
            artworkScale.x + frameThickness * 2,
            artworkScale.y + frameThickness * 2,
            frameDepth
        );
        
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


