using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple minimap system to help users navigate the gallery.
/// Shows player position and room layout.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private RectTransform minimapImage;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private Camera minimapCamera;
    
    [Header("Settings")]
    [SerializeField] private float minimapSize = 200f;
    [SerializeField] private float cameraHeight = 10f;
    [SerializeField] private bool rotateWithPlayer = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    
    private bool isVisible = true;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (player == null)
        {
            player = Camera.main?.transform;
        }
    }
    
    private void Start()
    {
        SetupMinimapCamera();
    }
    
    private void Update()
    {
        if (player == null) return;
        
        UpdateMinimapCamera();
        UpdatePlayerIcon();
        
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMinimap();
        }
    }
    
    private void SetupMinimapCamera()
    {
        if (minimapCamera == null)
        {
            GameObject cameraObject = new GameObject("Minimap Camera");
            minimapCamera = cameraObject.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 15f;
            minimapCamera.cullingMask = LayerMask.GetMask("Default"); // Adjust as needed
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            minimapCamera.depth = 10;
            minimapCamera.enabled = false; // We'll render manually if needed
        }
    }
    
    private void UpdateMinimapCamera()
    {
        if (minimapCamera == null) return;
        
        Vector3 playerPos = player.position;
        minimapCamera.transform.position = new Vector3(playerPos.x, cameraHeight, playerPos.z);
        
        if (rotateWithPlayer)
        {
            minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
        else
        {
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
    
    private void UpdatePlayerIcon()
    {
        if (playerIcon == null || minimapImage == null) return;
        
        // Position player icon in center (since camera follows player)
        playerIcon.anchoredPosition = Vector2.zero;
        
        // Rotate icon to match player rotation
        if (rotateWithPlayer)
        {
            playerIcon.localRotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);
        }
    }
    
    public void ToggleMinimap()
    {
        isVisible = !isVisible;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
    }
    
    public void SetMinimapVisible(bool visible)
    {
        isVisible = visible;
        ToggleMinimap();
    }
}




