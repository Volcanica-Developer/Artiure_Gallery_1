using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// First-person camera controller with keyboard/mouse and touch support.
/// Optimized for WebGL and desktop builds.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool invertRotation = false;

    [Header("Touch Settings")]
    [SerializeField] private float touchSensitivity = 0.5f;
    
    [Header("Click and Drag Settings")]
    [SerializeField] private bool requireClickToRotate = true; // If true, must click and drag to rotate
    [SerializeField] private int mouseButtonForRotation = 0; // 0 = Left, 1 = Right, 2 = Middle
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    
    // Input system
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;
    private bool isJumping;
    
    // Mouse drag input
    private bool isMouseDragging = false;
    private Vector2 lastMousePosition;
    
    // Touch input
    private Vector2 lastTouchPosition;
    private bool isTouching = false;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Keep cursor visible and unlocked for click-and-drag interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void Update()
    {
#if !ENABLE_INPUT_SYSTEM
        // Use legacy input if new Input System is not available
        HandleLegacyInput();
#endif
        HandleMovement();
        HandleMouseDragInput();
        HandleLook();
        HandleTouchInput();
    }
    
    private void HandleMovement()
    {
        // Check if grounded
        bool isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        // Calculate movement direction
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // Apply movement
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        
        // Handle jumping
        if (isJumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleMouseDragInput()
    {
        if (!requireClickToRotate)
        {
            // Use continuous mouse movement (old behavior)
            return;
        }
        
        // Check if mouse button is pressed
        bool mouseButtonDown = Input.GetMouseButtonDown(mouseButtonForRotation);
        bool mouseButtonHeld = Input.GetMouseButton(mouseButtonForRotation);
        bool mouseButtonUp = Input.GetMouseButtonUp(mouseButtonForRotation);
        
        if (mouseButtonDown)
        {
            // Start dragging - keep cursor visible
            isMouseDragging = true;
            lastMousePosition = Input.mousePosition;
            // Don't lock cursor - keep it visible
            lookInput = Vector2.zero;
        }
        else if (mouseButtonUp)
        {
            // Stop dragging
            isMouseDragging = false;
            lookInput = Vector2.zero;
        }
        else if (mouseButtonHeld && isMouseDragging)
        {
            // Calculate mouse delta from screen position
            // This works with visible cursor
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            
            // Invert direction so dragging feels like moving the environment
            lookInput = new Vector2(
                -mouseDelta.x * mouseSensitivity * 0.1f, // Invert horizontal, scale for screen space
                -mouseDelta.y * mouseSensitivity * 0.1f  // Invert vertical, scale for screen space
            );
            
            lastMousePosition = currentMousePosition;
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }
    
    private void HandleLook()
    {
        if (lookInput.sqrMagnitude < 0.01f) return;
        
        // Horizontal rotation (Y-axis)
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity * (invertRotation ? 1f : -1f));
        
        // Vertical rotation (X-axis) - clamped
        float yRotation = lookInput.y * mouseSensitivity * (invertY ? 1f : -1f);
        verticalRotation += yRotation;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void HandleTouchInput()
    {
        // Touch input for mobile/WebGL - only rotate on drag
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                isTouching = true;
                lookInput = Vector2.zero;
            }
            else if (touch.phase == TouchPhase.Moved && isTouching)
            {
                // Calculate touch delta for rotation
                // Invert direction so dragging feels like moving the environment
                Vector2 deltaPosition = touch.position - lastTouchPosition;
                lookInput = new Vector2(
                    -deltaPosition.x * touchSensitivity * 0.1f, // Invert horizontal
                    -deltaPosition.y * touchSensitivity * 0.1f  // Invert vertical
                );
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouching = false;
                lookInput = Vector2.zero;
            }
            else if (touch.phase == TouchPhase.Stationary)
            {
                // Don't rotate when touch is stationary
                lookInput = Vector2.zero;
            }
        }
        else
        {
            // No touch input
            isTouching = false;
            lookInput = Vector2.zero;
        }
    }
    
#if ENABLE_INPUT_SYSTEM
    // Input System callbacks (for new Input System)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }
    
    public void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }
    
    public void OnJump(InputValue value)
    {
        isJumping = value.isPressed;
    }
#endif
    
    // Legacy input support (for older Unity versions or WebGL compatibility)
    private void HandleLegacyInput()
    {
        // Movement
        moveInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // Look input is now handled by HandleMouseDragInput() for click-and-drag
        // Only use continuous mouse movement if requireClickToRotate is false
        if (!requireClickToRotate)
        {
            lookInput = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );
        }
        
        // Run
        isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // Jump
        isJumping = Input.GetButtonDown("Jump");
    }
    
    // Enable legacy input if new Input System is not available
    private void OnEnable()
    {
#if !ENABLE_INPUT_SYSTEM
        // Use legacy input system
        InvokeRepeating(nameof(HandleLegacyInput), 0f, 0f);
#endif
    }
    
    private void OnDisable()
    {
#if !ENABLE_INPUT_SYSTEM
        CancelInvoke(nameof(HandleLegacyInput));
#endif
    }
}

