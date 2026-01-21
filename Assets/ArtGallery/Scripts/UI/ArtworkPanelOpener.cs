using UnityEngine;

/// <summary>
/// TEMPORARY SCRIPT: Opens a UI panel when an artwork in 3D space is clicked.
/// Attach this to any 3D GameObject (artwork) that should open a panel when clicked.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArtworkPanelOpener : MonoBehaviour
{
    [Header("Panel Reference")]
    [Tooltip("Drag the UIPanel component here that should open when this artwork is clicked")]
    [SerializeField] private UIPanel targetPanel;
    
    [Tooltip("Or use panel name (must be registered in UIPanelManager)")]
    [SerializeField] private string panelName = "";
    
    [Header("Click Detection")]
    [Tooltip("Use OnMouseDown (requires collider and camera)")]
    [SerializeField] private bool useOnMouseDown = true;
    
    [Tooltip("Use raycast from camera (more flexible)")]
    [SerializeField] private bool useRaycast = false;
    
    [Tooltip("Maximum distance for raycast")]
    [SerializeField] private float maxRaycastDistance = 10f;
    
    [Header("Settings")]
    [Tooltip("Log when artwork is clicked")]
    [SerializeField] private bool logClicks = true;
    
    private Camera playerCamera;
    private Collider artworkCollider;
    
    private void Awake()
    {
        artworkCollider = GetComponent<Collider>();
        
        // Find camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        // Ensure collider is not a trigger (for OnMouseDown to work)
        if (useOnMouseDown && artworkCollider != null)
        {
            artworkCollider.isTrigger = false;
        }
    }
    
    private void Update()
    {
        if (useRaycast && Input.GetMouseButtonDown(0))
        {
            HandleRaycastClick();
        }
    }
    
    /// <summary>
    /// Called when mouse clicks on this GameObject (requires collider and camera).
    /// </summary>
    private void OnMouseDown()
    {
        if (useOnMouseDown)
        {
            OpenPanel();
        }
    }
    
    /// <summary>
    /// Handles raycast-based click detection.
    /// </summary>
    private void HandleRaycastClick()
    {
        if (playerCamera == null || artworkCollider == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance))
        {
            if (hit.collider == artworkCollider)
            {
                OpenPanel();
            }
        }
    }
    
    /// <summary>
    /// Opens the target panel.
    /// </summary>
    private void OpenPanel()
    {
        if (logClicks)
            Debug.Log($"ArtworkPanelOpener: Opening panel for artwork '{gameObject.name}'");
        
        // Try panel reference first
        if (targetPanel != null)
        {
            if (UIPanelManager.Instance != null)
            {
                UIPanelManager.Instance.OpenPanel(targetPanel);
            }
            else
            {
                targetPanel.Open();
            }
            
            if (logClicks)
                Debug.Log($"ArtworkPanelOpener: Opened panel '{targetPanel.gameObject.name}'");
            return;
        }
        
        // Try panel name
        if (!string.IsNullOrEmpty(panelName))
        {
            if (UIPanelManager.Instance != null)
            {
                UIPanelManager.Instance.OpenPanel(panelName);
                
                if (logClicks)
                    Debug.Log($"ArtworkPanelOpener: Opened panel '{panelName}'");
            }
            else
            {
                Debug.LogWarning($"ArtworkPanelOpener: Panel name '{panelName}' specified but UIPanelManager not found. Use panel reference instead.");
            }
            return;
        }
        
        Debug.LogWarning($"ArtworkPanelOpener: No panel reference or panel name set for artwork '{gameObject.name}'. Please assign a panel in the inspector.");
    }
    
    /// <summary>
    /// Public method to open panel programmatically.
    /// </summary>
    public void OpenTargetPanel()
    {
        OpenPanel();
    }
    
    /// <summary>
    /// Sets the target panel programmatically.
    /// </summary>
    public void SetTargetPanel(UIPanel panel)
    {
        targetPanel = panel;
    }
    
    /// <summary>
    /// Sets the target panel by name.
    /// </summary>
    public void SetTargetPanelName(string name)
    {
        panelName = name;
    }
}
