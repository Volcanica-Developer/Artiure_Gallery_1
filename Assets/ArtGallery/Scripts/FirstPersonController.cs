using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DG.Tweening;

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
    [SerializeField] private float gravity = -9.81f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool invertRotation = false;

    [Header("Touch Look Settings")]
    [SerializeField] private float touchSensitivity = 0.5f;

    [Header("Game Controls Settings")]
    [SerializeField] private bool enableGameControls = true;
    [SerializeField] private TouchMovementJoystick touchMovementJoystick;

    [Header("Mobile Movement (Mobile Only)")]
    [SerializeField] private bool useNewMobileControl = false;
    [SerializeField] private float pinchMoveSensitivity = 1f;
    [SerializeField] private float pinchReferenceDistance = 100f;
    [SerializeField] private float swipeStrafeSensitivity = 1f;
    [SerializeField] private float swipeStrafeReferenceDistance = 100f;

    
    [Header("Click To Move Settings")]
    [SerializeField] private bool enableClickToMove = false;
    [SerializeField] private LayerMask clickMoveLayerMask = ~0; // Layers that can be clicked for movement (e.g. Ground)
    [SerializeField] private float clickMoveLerpSpeed = 5f;
    [SerializeField] private float clickMoveStopDistance = 0.05f;
    [SerializeField] private float clickMoveMaxClickDistance = 25f; // max mouse drag (pixels) to still count as a click
    [SerializeField] private Transform clickMoveMarker; // optional marker object shown on the floor where we will move
    [SerializeField] private Renderer clickMoveMarkerRenderer; // renderer used to control marker opacity
    [SerializeField, Range(0f, 1f)] private float clickMoveMarkerBaseAlpha = 0.25f;
    [SerializeField] private float clickMoveMarkerBreathScaleMult = 1.1f;
    [SerializeField] private float clickMoveMarkerBreathDuration = 0.8f;
    
    [Header("Click and Drag Settings")]
    [SerializeField] private bool requireClickToRotate = true; // If true, must click and drag to rotate
    [SerializeField] private int mouseButtonForRotation = 0; // 0 = Left, 1 = Right, 2 = Middle

    [Header("Smoothing Settings")]
    [SerializeField, Range(0f, 20f)] private float movementSmoothing = 10f;
    [SerializeField, Range(0f, 30f)] private float lookSmoothing = 15f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    
    // Input system
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 smoothedMoveInput;
    private Vector2 smoothedLookInput;
    private bool isRunning;
    
    // Mouse drag input
    private bool isMouseDragging = false;
    private Vector2 lastMousePosition;
    private bool isMouseMovementDragging = false;
    private Vector2 mouseMovementStartPosition;
    
    // Touch input
    private Vector2 lastTouchPosition;
    private bool isTouching = false;

    // Click-to-move state
    private bool isClickMoving = false;
    private Vector3 clickMoveTargetPosition;
    private Vector2 clickMoveMouseDownPosition;
    private Tween clickMoveMarkerTween;
    private int clickMoveTouchId = -1;

    // Split touch input (left = movement, right = look)
    private int leftTouchId = -1;
    private int rightTouchId = -1;
    private Vector2 leftTouchStartPosition;
    private Vector2 rightTouchLastPosition;
    [SerializeField] private float touchMoveDragMaxDistance = 150f; // pixels for full movement

    // New mobile control (pinch movement + swipe strafe)
    private bool isPinchActive;
    private float lastPinchDistance;
    private bool isSwipeActive;
    private Vector2 swipeStartPosition;

    // True when running on a mobile device / mobile browser (e.g., mobile WebGL)
    private bool isMobileBrowser;
    
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

        // Initialize marker opacity and breathing effect
        if (clickMoveMarkerRenderer != null)
        {
            Color c = clickMoveMarkerRenderer.material.color;
            c.a = clickMoveMarkerBaseAlpha;
            clickMoveMarkerRenderer.material.color = c;
        }

        if (clickMoveMarker != null)
        {
            // Start a subtle breathing (pulsing) effect on the marker scale
            clickMoveMarkerTween = clickMoveMarker.DOScale(
                    clickMoveMarker.localScale * clickMoveMarkerBreathScaleMult,
                    clickMoveMarkerBreathDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .Pause(); // we'll play it only when marker is visible
        }

        // Decide between PC controls (WASD) and mobile touch controls
        // This is true on Android/iOS and most mobile WebGL browsers.
        isMobileBrowser = Application.isMobilePlatform;

        if (isMobileBrowser)
        {
            // Mobile browser / device: enable game controls (touch controls)
            enableGameControls = true;
        }
        else
        {
            // PC browser / desktop: disable mobile game controls, use WASD instead
            enableGameControls = false;
        }
    }
    
    private void Update()
    {
#if !ENABLE_INPUT_SYSTEM
        // Use legacy input if new Input System is not available
        HandleLegacyInput();
#endif
        
        if (isMobileBrowser && useNewMobileControl)
        {
            // New mobile control scheme: pinch for forward/back, swipe for look.
            HandleNewMobileControl();
        }
        else
        {
            // Old control scheme: mouse drag + split touch (left = move, right = look) + optional joystick.

            // Mouse drag (PC) and real touch input both update moveInput / lookInput
            HandleMouseDragInput();
            HandleTouchInput();

            // If a touch joystick is active, it can override moveInput with its direction
            HandleTouchMovement();
        }

        // Mouse / touch click-to-move (raycast to ground)
        HandleClickToMoveInput();

        // Keyboard yaw (Q/E) for left/right rotation (desktop/WebGL)
        float keyboardLookX = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            keyboardLookX += 1f; // rotate left
        }
        if (Input.GetKey(KeyCode.E))
        {
            keyboardLookX -= 1f; // rotate right
        }
        if (Mathf.Abs(keyboardLookX) > 0.01f)
        {
            // Add to any existing look input (mouse, touch, or gamepad)
            lookInput.x += keyboardLookX;
        }

        // Smooth movement and look so they ease out when input stops
        float moveLerp = movementSmoothing > 0f ? 1f - Mathf.Exp(-movementSmoothing * Time.deltaTime) : 1f;
        float lookLerp = lookSmoothing > 0f ? 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime) : 1f;
        smoothedMoveInput = Vector2.Lerp(smoothedMoveInput, moveInput, moveLerp);
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, lookLerp);

        // Finally apply movement and look based on the latest smoothed input values
        HandleMovement();
        HandleLook();
    }
    
    private void HandleMovement()
    {
        // Check if grounded
        bool isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // If player starts providing manual movement input, cancel click-to-move
        bool hasManualInput = moveInput.sqrMagnitude > 0.0001f;
        if (isClickMoving && hasManualInput)
        {
            isClickMoving = false;
        }

        // Horizontal movement (either click-to-move or input-based)
        Vector3 horizontalMove = Vector3.zero;

        if (isClickMoving)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = new Vector3(clickMoveTargetPosition.x, currentPos.y, clickMoveTargetPosition.z);
            Vector3 toTarget = targetPos - currentPos;
            float distance = toTarget.magnitude;

            if (distance <= clickMoveStopDistance)
            {
                isClickMoving = false;
            }
            else
            {
                // Lerp towards the target position for smooth click-move motion
                Vector3 desiredPos = Vector3.Lerp(currentPos, targetPos, clickMoveLerpSpeed * Time.deltaTime);
                horizontalMove = desiredPos - currentPos;
            }
        }

        if (!isClickMoving)
        {
            // Calculate movement direction (using smoothed input for easing)
            Vector3 moveDirection = transform.right * smoothedMoveInput.x + transform.forward * smoothedMoveInput.y;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            horizontalMove += moveDirection * currentSpeed * Time.deltaTime;
        }

        // Apply horizontal movement
        characterController.Move(horizontalMove);
        
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

        float halfWidth = Screen.width * 0.5f;
        
        if (mouseButtonDown)
        {
            Vector2 startPos = Input.mousePosition;
            bool isLeftSide = startPos.x < halfWidth;

            // If click starts on the left side and game controls are enabled (and no joystick),
            // treat this mouse drag as movement for testing on PC.
            if (isLeftSide && enableGameControls && touchMovementJoystick == null)
            {
                isMouseMovementDragging = true;
                isMouseDragging = false;
                mouseMovementStartPosition = startPos;
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
            }
            else
            {
                // Start dragging for look - keep cursor visible
                isMouseDragging = true;
                isMouseMovementDragging = false;
                lastMousePosition = startPos;
                // Don't lock cursor - keep it visible
                lookInput = Vector2.zero;
            }
        }
        else if (mouseButtonUp)
        {
            // Stop any drag
            if (isMouseMovementDragging)
            {
                moveInput = Vector2.zero;
            }

            isMouseDragging = false;
            isMouseMovementDragging = false;
            lookInput = Vector2.zero;
        }
        else if (mouseButtonHeld)
        {
            // Movement drag (left side)
            if (isMouseMovementDragging)
            {
                Vector2 current = Input.mousePosition;
                Vector2 delta = current - mouseMovementStartPosition;
                Vector2 clamped = Vector2.ClampMagnitude(delta, touchMoveDragMaxDistance);
                Vector2 normalized = clamped / touchMoveDragMaxDistance; // -1..1

                // x = left/right, y = forward/back
                moveInput = new Vector2(normalized.x, normalized.y);
            }
            // Look drag (right side)
            else if (isMouseDragging)
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
        }
        else
        {
            // No drag in progress
            if (!isMouseMovementDragging)
            {
                lookInput = Vector2.zero;
            }
        }
    }
    
    private void HandleLook()
    {
        if (smoothedLookInput.sqrMagnitude < 0.0001f) return;
        
        // Horizontal rotation (Y-axis)
        transform.Rotate(Vector3.up * smoothedLookInput.x * mouseSensitivity * (invertRotation ? 1f : -1f));
        
        // Vertical rotation (X-axis) - clamped
        float yRotation = smoothedLookInput.y * mouseSensitivity * (invertY ? 1f : -1f);
        verticalRotation += yRotation;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void HandleTouchInput()
    {
        // No touches: reset state used for split-touch controls
        if (Input.touchCount == 0)
        {
            isTouching = false;

            // If we're not currently dragging with the mouse, clear look input.
            // Mouse drag look is handled in HandleMouseDragInput().
            if (!isMouseDragging && !isMouseMovementDragging)
            {
                lookInput = Vector2.zero;
            }

            // Only clear movement if we previously had an active touch-move finger.
            // This avoids fighting with keyboard or mouse-based movement on PC.
            if (enableGameControls && touchMovementJoystick == null && leftTouchId != -1)
            {
                leftTouchId = -1;
                rightTouchId = -1;
                moveInput = Vector2.zero;
            }

            return;
        }

        float halfWidth = Screen.width * 0.5f;
        Vector2 newMoveInput = moveInput;
        Vector2 lookDelta = Vector2.zero;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            bool isLeftSide = touch.position.x < halfWidth;

            // LEFT side: drag to move (only if no joystick is assigned)
            if (isLeftSide && enableGameControls && touchMovementJoystick == null)
            {
                if (leftTouchId == -1 && (touch.phase == TouchPhase.Began ||
                                          touch.phase == TouchPhase.Moved ||
                                          touch.phase == TouchPhase.Stationary))
                {
                    leftTouchId = touch.fingerId;
                    leftTouchStartPosition = touch.position;
                }

                if (touch.fingerId == leftTouchId)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        Vector2 delta = touch.position - leftTouchStartPosition;
                        Vector2 clamped = Vector2.ClampMagnitude(delta, touchMoveDragMaxDistance);
                        Vector2 normalized = clamped / touchMoveDragMaxDistance; // -1..1

                        // x = left/right, y = forward/back
                        newMoveInput = new Vector2(normalized.x, normalized.y);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        leftTouchId = -1;
                        newMoveInput = Vector2.zero;
                    }
                }
            }
            // RIGHT side: drag to rotate camera
            else if (!isLeftSide)
            {
                if (rightTouchId == -1 && touch.phase == TouchPhase.Began)
                {
                    rightTouchId = touch.fingerId;
                    rightTouchLastPosition = touch.position;
                    isTouching = true;
                    lookInput = Vector2.zero;
                }
                else if (touch.fingerId == rightTouchId && touch.phase == TouchPhase.Moved && isTouching)
                {
                    Vector2 deltaPosition = touch.position - rightTouchLastPosition;
                    lookDelta += new Vector2(
                        -deltaPosition.x * touchSensitivity * 0.1f, // Invert horizontal
                        -deltaPosition.y * touchSensitivity * 0.1f  // Invert vertical
                    );
                    rightTouchLastPosition = touch.position;
                }
                else if (touch.fingerId == rightTouchId &&
                         (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    rightTouchId = -1;
                    isTouching = false;
                }
            }
        }

        // Apply movement from left-drag when using drag-based touch movement
        if (enableGameControls && touchMovementJoystick == null)
        {
            moveInput = newMoveInput;
        }

        // Apply look from right-drag
        lookInput = lookDelta;
    }

    private void HandleTouchMovement()
    {
        // If game controls are disabled entirely, do nothing
        if (!enableGameControls)
            return;

        // Joystick is optional: only use it when assigned
        if (touchMovementJoystick == null)
            return;

        // If joystick is being used, override moveInput with joystick direction
        if (touchMovementJoystick.HasInput)
        {
            // x = left/right, y = forward/back
            moveInput = touchMovementJoystick.Direction;
        }
    }

    /// <summary>
    /// New mobile control scheme: pinch to move forward/back, horizontal swipe to rotate, gyro for camera rotation.
    /// Replaces the old split-touch scheme when useNewMobileControl is true.
    /// </summary>
    private void HandleNewMobileControl()
    {
        // Reset inputs each frame; new scheme does not use old split-touch movement/look.
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        // --- Pinch for forward/back movement ---
        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float currentDistance = Vector2.Distance(t0.position, t1.position);

            if (!isPinchActive)
            {
                isPinchActive = true;
                lastPinchDistance = currentDistance;
            }
            else
            {
                float delta = currentDistance - lastPinchDistance; // >0 = pinch out, <0 = pinch in
                float normalized = 0f;

                if (Mathf.Abs(delta) > 0.01f && pinchReferenceDistance > 0.01f)
                {
                    normalized = Mathf.Clamp(delta / pinchReferenceDistance, -1f, 1f) * pinchMoveSensitivity;
                }

                // y = forward/back movement
                moveInput = new Vector2(moveInput.x, Mathf.Clamp(normalized, -1f, 1f));

                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            isPinchActive = false;
        }

        // --- One-finger horizontal swipe to rotate left/right ---
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (!isSwipeActive)
            {
                isSwipeActive = true;
                swipeStartPosition = t.position;
            }

            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                float deltaX = t.position.x - swipeStartPosition.x;
                float deltaY = t.position.y - swipeStartPosition.y;
                float normalizedX = 0f;
                float normalizedY = 0f;

                if (Mathf.Abs(deltaX) > 0.01f && swipeStrafeReferenceDistance > 0.01f)
                {
                    normalizedX = Mathf.Clamp(deltaX / swipeStrafeReferenceDistance, -1f, 1f) * swipeStrafeSensitivity;
                }

                if (Mathf.Abs(deltaY) > 0.01f && swipeStrafeReferenceDistance > 0.01f)
                {
                    normalizedY = Mathf.Clamp(deltaY / swipeStrafeReferenceDistance, -1f, 1f) * swipeStrafeSensitivity;
                }

                // Add horizontal + vertical look from swipe
                float clampedX = Mathf.Clamp(normalizedX, -1f, 1f);
                float clampedY = Mathf.Clamp(normalizedY, -1f, 1f);
                lookInput.x += clampedX;
                lookInput.y += clampedY;
            }

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isSwipeActive = false;
            }
        }
        else if (Input.touchCount == 0)
        {
            isSwipeActive = false;
        }

    }

    private void HandleClickToMoveInput()
    {
        if (!enableClickToMove || playerCamera == null)
            return;

        // Update hover marker on the floor under the cursor when not currently moving
        if (clickMoveMarker != null)
        {
            if (!isClickMoving)
            {
                // On desktop/web, preview the marker under the cursor before clicking.
                // On mobile/touch platforms, we skip this (no cursor), marker only appears on tap.
                if (!Application.isMobilePlatform)
                {
                    Ray hoverRay = playerCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(hoverRay, out RaycastHit hoverHit, 100f, clickMoveLayerMask))
                    {
                        // Keep marker "stuck" to its original height, only move in X/Z
                        Vector3 markerPos = clickMoveMarker.position;
                        markerPos.x = hoverHit.point.x;
                        markerPos.z = hoverHit.point.z;
                        clickMoveMarker.position = markerPos;

                        if (!clickMoveMarker.gameObject.activeSelf)
                        {
                            clickMoveMarker.gameObject.SetActive(true);
                            clickMoveMarkerTween?.Play();
                        }
                    }
                    else if (clickMoveMarker.gameObject.activeSelf)
                    {
                        clickMoveMarker.gameObject.SetActive(false);
                        clickMoveMarkerTween?.Pause();
                    }
                }
                else if (clickMoveMarker.gameObject.activeSelf)
                {
                    // On mobile, no hover preview; hide marker when not moving.
                    clickMoveMarker.gameObject.SetActive(false);
                    clickMoveMarkerTween?.Pause();
                }
            }
            else
            {
                // While lerping, keep marker at the target position (again only X/Z)
                Vector3 markerPos = clickMoveMarker.position;
                markerPos.x = clickMoveTargetPosition.x;
                markerPos.z = clickMoveTargetPosition.z;
                clickMoveMarker.position = markerPos;

                if (!clickMoveMarker.gameObject.activeSelf)
                {
                    clickMoveMarker.gameObject.SetActive(true);
                    clickMoveMarkerTween?.Play();
                }
            }
        }

        // Record position on mouse down so we can detect drag distance (desktop/web)
        if (Input.GetMouseButtonDown(0))
        {
            clickMoveMouseDownPosition = Input.mousePosition;
        }

        // Only start movement on mouse up if it was effectively a click (not a drag)
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 mouseUpPos = Input.mousePosition;
            float dragDistance = Vector2.Distance(mouseUpPos, clickMoveMouseDownPosition);

            // If dragged beyond the allowed distance, treat as drag and do not move
            if (dragDistance > clickMoveMaxClickDistance)
                return;

            Ray ray = playerCamera.ScreenPointToRay(mouseUpPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickMoveLayerMask))
            {
                Vector3 target = hit.point;
                // Keep current character height so we don't snap vertically if the floor is uneven
                target.y = transform.position.y;

                clickMoveTargetPosition = target;
                isClickMoving = true;
            }
        }

        // Touch tap for click-to-move (mobile/touch devices)
        if (Input.touchSupported && Application.isMobilePlatform && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.phase == TouchPhase.Began && clickMoveTouchId == -1)
                {
                    clickMoveTouchId = touch.fingerId;
                    clickMoveMouseDownPosition = touch.position;
                }
                else if (touch.fingerId == clickMoveTouchId &&
                         (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    Vector2 touchUpPos = touch.position;
                    float dragDistance = Vector2.Distance(touchUpPos, clickMoveMouseDownPosition);
                    clickMoveTouchId = -1;

                    // If dragged beyond the allowed distance, treat as drag and do not move
                    if (dragDistance > clickMoveMaxClickDistance)
                        continue;

                    Ray ray = playerCamera.ScreenPointToRay(touchUpPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickMoveLayerMask))
                    {
                        Vector3 target = hit.point;
                        // Keep current character height so we don't snap vertically if the floor is uneven
                        target.y = transform.position.y;

                        clickMoveTargetPosition = target;
                        isClickMoving = true;
                    }

                    break;
                }
            }
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
