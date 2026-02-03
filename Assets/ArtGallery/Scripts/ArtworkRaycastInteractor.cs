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
        // If the pointer is over UI, do not interact with the 3D world.
        if (IsPointerOverUI())
        {
            // Clear any hovered frame so it doesn't stay visually highlighted.
            if (currentHoveredFrame != null)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    ExecuteEvents.Execute(currentHoveredFrame.gameObject,
                        new PointerEventData(eventSystem),
                        ExecuteEvents.pointerExitHandler);
                }
                else
                {
                    currentHoveredFrame.OnPointerExit(null);
                }

                currentHoveredFrame = null;
            }

            return;
        }

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

    /// <summary>
    /// Returns true if the current mouse or any active touch is over a UI element.
    /// Used to prevent artwork interaction while interacting with UI.
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        // Mouse-based pointer (desktop/WebGL)
        return EventSystem.current.IsPointerOverGameObject();
#else
        // Touch-based pointers (mobile)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        // Fallback for other pointer types
        return EventSystem.current.IsPointerOverGameObject();
#endif
    }
}

