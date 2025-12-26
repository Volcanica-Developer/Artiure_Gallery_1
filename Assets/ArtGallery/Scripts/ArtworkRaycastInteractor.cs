using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles raycast-based interaction with artwork frames.
/// Works with both mouse and touch input.
/// </summary>
public class ArtworkRaycastInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float maxInteractionDistance = 5f;
    [SerializeField] private LayerMask artworkLayer = -1;
    [SerializeField] private bool showDebugRay = false;
    
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private bool useTouch = true;
    
    private Camera playerCamera;
    private ArtworkFrame currentHoveredFrame;
    
    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    
    private void Update()
    {
        HandleRaycast();
        
        // Handle input
        if (useMouseClick && Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
        
        if (useTouch && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryInteract();
        }
        
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }
    
    private void HandleRaycast()
    {
        // Cast ray from cursor position (desktop) or touch position (mobile)
        Vector3 screenPoint;
        
        if (Input.touchCount > 0)
        {
            // For mobile, use touch position
            screenPoint = Input.GetTouch(0).position;
        }
        else
        {
            // For desktop/WebGL, use cursor position
            screenPoint = Input.mousePosition;
        }
        
        Ray ray = playerCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        
        ArtworkFrame hitFrame = null;
        
        if (Physics.Raycast(ray, out hit, maxInteractionDistance, artworkLayer))
        {
            hitFrame = hit.collider.GetComponent<ArtworkFrame>();
            
            if (showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            }
        }
        else
        {
            if (showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxInteractionDistance, Color.red);
            }
        }
        
        // Update hover state
        if (hitFrame != currentHoveredFrame)
        {
            if (currentHoveredFrame != null)
            {
                // Exit previous frame
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    ExecuteEvents.Execute(currentHoveredFrame.gameObject, 
                        new PointerEventData(eventSystem), 
                        ExecuteEvents.pointerExitHandler);
                }
                else
                {
                    // Fallback: call directly
                    currentHoveredFrame.OnPointerExit(null);
                }
            }
            
            currentHoveredFrame = hitFrame;
            
            if (currentHoveredFrame != null)
            {
                // Enter new frame
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    ExecuteEvents.Execute(currentHoveredFrame.gameObject, 
                        new PointerEventData(eventSystem), 
                        ExecuteEvents.pointerEnterHandler);
                }
                else
                {
                    // Fallback: call directly
                    currentHoveredFrame.OnPointerEnter(null);
                }
            }
        }
    }
    
    private void TryInteract()
    {
        if (currentHoveredFrame != null)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                // Click on frame using EventSystem
                ExecuteEvents.Execute(currentHoveredFrame.gameObject, 
                    new PointerEventData(eventSystem), 
                    ExecuteEvents.pointerClickHandler);
            }
            else
            {
                // Fallback: call directly if EventSystem doesn't exist
                Debug.LogWarning("EventSystem not found. Calling artwork click directly.");
                currentHoveredFrame.OnPointerClick(null);
            }
        }
    }
}

