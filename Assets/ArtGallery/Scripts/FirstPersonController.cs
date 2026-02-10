using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DG.Tweening;

/// <summary>
/// First-person camera controller with keyboard/mouse and touch support.
/// Optimized for WebGL and desktop builds.
/// Supports both CharacterController and NavMeshAgent for movement.
/// </summary>
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("NavMesh Settings")]
    [Tooltip("When enabled, uses NavMeshAgent for movement instead of CharacterController.")]
    [SerializeField] private bool useNavMeshAgent = false;
    [Tooltip("Stopping distance for NavMeshAgent when using click-to-move or input movement.")]
    [SerializeField] private float navMeshStoppingDistance = 0.1f;
    [Tooltip("When enabled, prevents NavMeshAgent from changing the player's Y position (height).")]
    [SerializeField] private bool lockNavMeshYPosition = true;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool invertRotation = false;

    [Header("Vertical Camera Rotation (Camera rotates instead of player)")]
    [Tooltip("When enabled, vertical drag rotates the camera's pitch. Horizontal drag still rotates the player.")]
    [SerializeField] private bool useVerticalCameraRotation = true;

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
    [SerializeField] private LayerMask clickMoveIgnoreLayers; // Layers that should NOT trigger click-to-move (e.g. Display Wall)
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

    [Header("Debug")]
    [SerializeField] private bool debugClickToMoveLayers = false;
    
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;
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

    // Tween used to smoothly adjust camera pitch when snapping look direction (e.g. after focusing a wall)
    private Tween lookPitchTween;

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
        navMeshAgent = GetComponent<NavMeshAgent>();
        playerCamera = GetComponentInChildren<Camera>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Configure based on movement mode
        if (useNavMeshAgent && navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false; // We handle rotation manually
            navMeshAgent.updatePosition = false; // We handle position manually to control Y
            navMeshAgent.stoppingDistance = navMeshStoppingDistance;
            if (characterController != null)
            {
                characterController.enabled = false; // Disable CharacterController when using NavMesh
            }
        }
        else if (characterController != null)
        {
            useNavMeshAgent = false; // Fallback to CharacterController if no NavMeshAgent
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }
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
        // Make Q/E independent of the InvertRotation setting by compensating for it here
        float keyboardSign = invertRotation ? -1f : 1f;
        if (Input.GetKey(KeyCode.Q))
        {
            keyboardLookX += 1f * keyboardSign; // rotate left
        }
        if (Input.GetKey(KeyCode.E))
        {
            keyboardLookX -= 1f * keyboardSign; // rotate right
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
        if (useNavMeshAgent)
        {
            HandleNavMeshMovement();
        }
        else
        {
            HandleCharacterControllerMovement();
        }
    }

    private void HandleCharacterControllerMovement()
    {
        // If the CharacterController is disabled (e.g. while tweening to a wall stand point), skip movement.
        if (characterController == null || !characterController.enabled)
            return;

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

    private void HandleNavMeshMovement()
    {
        // If the NavMeshAgent is disabled (e.g. while tweening to a wall stand point), skip movement.
        if (navMeshAgent == null || !navMeshAgent.enabled)
            return;

        // Store original Y position to preserve height
        float originalY = transform.position.y;

        // If player starts providing manual movement input, cancel click-to-move
        bool hasManualInput = moveInput.sqrMagnitude > 0.0001f;
        if (isClickMoving && hasManualInput)
        {
            isClickMoving = false;
            navMeshAgent.ResetPath();
        }

        // Check if NavMeshAgent has reached its destination for click-to-move
        if (isClickMoving)
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                isClickMoving = false;
            }
        }

        // Manual input movement (WASD / joystick)
        if (!isClickMoving && hasManualInput)
        {
            // Calculate movement direction (using smoothed input for easing)
            Vector3 moveDirection = transform.right * smoothedMoveInput.x + transform.forward * smoothedMoveInput.y;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            
            // Use NavMeshAgent.Move for direct velocity-based movement
            navMeshAgent.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        // Manually sync transform position from NavMeshAgent (since updatePosition is false)
        // Use NavMeshAgent's X/Z but keep our original Y
        Vector3 agentPos = navMeshAgent.nextPosition;
        if (lockNavMeshYPosition)
        {
            transform.position = new Vector3(agentPos.x, originalY, agentPos.z);
        }
        else
        {
            transform.position = agentPos;
        }
    }

    
    private void HandleMouseDragInput()
    {
        if (!requireClickToRotate)
        {
            // Use continuous mouse movement (old behavior)
            return;
        }

        // When the pointer is over any UI, do not treat mouse drags as world interaction.
        if (IsPointerOverUI())
        {
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
        
        // Horizontal rotation (Y-axis) - always rotate the player
        transform.Rotate(Vector3.up * smoothedLookInput.x * mouseSensitivity * (invertRotation ? 1f : -1f));
        
        // Vertical rotation (X-axis) - camera pitch, clamped
        if (useVerticalCameraRotation)
        {
            // Ensure player only rotates on Y axis (no tilt/roll)
            Vector3 playerEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, playerEuler.y, 0f);
            
            float yRotation = smoothedLookInput.y * mouseSensitivity * (invertY ? 1f : -1f);
            verticalRotation += yRotation;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
    
    private void HandleTouchInput()
    {
        // If the pointer/touch is over UI, skip world touch handling.
        if (IsPointerOverUI())
        {
            return;
        }

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
        // Do not interpret gestures that start over UI as world movement/look.
        if (IsPointerOverUI())
        {
            return;
        }

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

        // --- One-finger swipe handling removed: no swipe-to-rotate on mobile ---
        // We intentionally do not use a single-finger swipe to drive look/rotation anymore.
        // Pinch still controls forward/back movement via moveInput.y above.
        isSwipeActive = false;

        // (Old) no-gyro path: only pinch affects movement/look here.
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    /// <summary>
    /// Returns true if the current mouse or any active touch is over a UI element.
    /// Used to prevent mouse/touch from interacting with the 3D world while using UI.
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

    private void HandleClickToMoveInput()
    {
        if (!enableClickToMove || playerCamera == null)
            return;

        // Ignore click/tap-to-move when interacting with UI.
        if (IsPointerOverUI())
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
                    int combinedMask = clickMoveLayerMask | clickMoveIgnoreLayers;
                    RaycastHit[] hits = Physics.RaycastAll(hoverRay, 100f, combinedMask);

                    if (hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        bool blockedByIgnored = false;
                        bool placedMarker = false;

                        foreach (var hoverHit in hits)
                        {
                            int layer = hoverHit.collider.gameObject.layer;

                            // If the closest thing in the way is an ignored layer (e.g. Display Wall), block marker
                            if (IsLayerInMask(layer, clickMoveIgnoreLayers))
                            {
                                blockedByIgnored = true;
                                break;
                            }

                            // If it is a valid click-move layer, place the marker there
                            if (IsLayerInMask(layer, clickMoveLayerMask))
                            {
                                Vector3 markerPos = clickMoveMarker.position;
                                markerPos.x = hoverHit.point.x;
                                markerPos.z = hoverHit.point.z;
                                clickMoveMarker.position = markerPos;

                                if (!clickMoveMarker.gameObject.activeSelf)
                                {
                                    clickMoveMarker.gameObject.SetActive(true);
                                    clickMoveMarkerTween?.Play();
                                }

                                placedMarker = true;
                                break;
                            }
                        }

                        if (!placedMarker || blockedByIgnored)
                        {
                            if (clickMoveMarker.gameObject.activeSelf)
                            {
                                clickMoveMarker.gameObject.SetActive(false);
                                clickMoveMarkerTween?.Pause();
                            }
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
            int combinedMask = clickMoveLayerMask | clickMoveIgnoreLayers;
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, combinedMask);
            if (hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    int layer = hit.collider.gameObject.layer;
                    string layerName = LayerMask.LayerToName(layer);

                    if (debugClickToMoveLayers)
                    {
                        Debug.Log($"[ClickToMove] Mouse click candidate hit '{hit.collider.gameObject.name}' on layer {layer} ('{layerName}') at {hit.point}");
                    }

                    // First priority: if we clicked an ArtworkFrame, focus that specific frame.
                    ArtworkFrame clickedFrame = hit.collider.GetComponentInParent<ArtworkFrame>();
                    if (clickedFrame != null)
                    {
                        DisplayWall owningWall = clickedFrame.GetComponentInParent<DisplayWall>();
                        if (owningWall != null)
                        {
                            if (debugClickToMoveLayers)
                            {
                                Debug.Log($"[ClickToMove] Mouse click focusing frame '{clickedFrame.gameObject.name}' on wall '{owningWall.gameObject.name}'");
                            }

                            owningWall.FocusPlayerOnFrame(clickedFrame);
                            return;
                        }
                    }

                    // If the first thing we run into is an ignored layer (e.g. Display Wall), block movement completely
                    if (IsLayerInMask(layer, clickMoveIgnoreLayers))
                    {
                        if (debugClickToMoveLayers)
                        {
                            Debug.Log($"[ClickToMove] Mouse click blocked by ignored layer {layer} ('{layerName}')");
                        }
                        return;
                    }

                    // If it is a valid click-move layer, move there
                    if (IsLayerInMask(layer, clickMoveLayerMask))
                    {
                        Vector3 target = hit.point;
                        // Keep current character height so we don't snap vertically if the floor is uneven
                        target.y = transform.position.y;

                        clickMoveTargetPosition = target;
                        isClickMoving = true;

                        // Use NavMeshAgent.SetDestination for pathfinding
                        if (useNavMeshAgent && navMeshAgent != null && navMeshAgent.enabled)
                        {
                            navMeshAgent.SetDestination(target);
                        }
                        return;
                    }
                }
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
                    int combinedMask = clickMoveLayerMask | clickMoveIgnoreLayers;
                    RaycastHit[] hits = Physics.RaycastAll(ray, 100f, combinedMask);
                    if (hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        foreach (var hit in hits)
                        {
                            int layer = hit.collider.gameObject.layer;
                            string layerName = LayerMask.LayerToName(layer);

                            if (debugClickToMoveLayers)
                            {
                                Debug.Log($"[ClickToMove] Touch tap candidate hit '{hit.collider.gameObject.name}' on layer {layer} ('{layerName}') at {hit.point}");
                            }

                            // First priority: if we tapped an ArtworkFrame, focus that specific frame.
                            ArtworkFrame tappedFrame = hit.collider.GetComponentInParent<ArtworkFrame>();
                            if (tappedFrame != null)
                            {
                                DisplayWall owningWall = tappedFrame.GetComponentInParent<DisplayWall>();
                                if (owningWall != null)
                                {
                                    if (debugClickToMoveLayers)
                                    {
                                        Debug.Log($"[ClickToMove] Touch tap focusing frame '{tappedFrame.gameObject.name}' on wall '{owningWall.gameObject.name}'");
                                    }

                                    owningWall.FocusPlayerOnFrame(tappedFrame);
                                    return;
                                }
                            }

                            // If the first thing we run into is an ignored layer (e.g. Display Wall), block movement completely
                            if (IsLayerInMask(layer, clickMoveIgnoreLayers))
                            {
                                if (debugClickToMoveLayers)
                                {
                                    Debug.Log($"[ClickToMove] Touch tap blocked by ignored layer {layer} ('{layerName}')");
                                }
                                return;
                            }

                            // If it is a valid click-move layer, move there
                            if (IsLayerInMask(layer, clickMoveLayerMask))
                            {
                                Vector3 target = hit.point;
                                // Keep current character height so we don't snap vertically if the floor is uneven
                                target.y = transform.position.y;

                                clickMoveTargetPosition = target;
                                isClickMoving = true;

                                // Use NavMeshAgent.SetDestination for pathfinding
                                if (useNavMeshAgent && navMeshAgent != null && navMeshAgent.enabled)
                                {
                                    navMeshAgent.SetDestination(target);
                                }
                                break;
                            }
                        }
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

    #region Public API for UI

    // Basic movement
    public float WalkSpeed { get => walkSpeed; set => walkSpeed = value; }
    public float RunSpeed { get => runSpeed; set => runSpeed = value; }
    public float GravityValue { get => gravity; set => gravity = value; }

    // NavMesh
    public bool UseNavMeshAgent { get => useNavMeshAgent; }
    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }
    public float NavMeshStoppingDistance { get => navMeshStoppingDistance; set { navMeshStoppingDistance = value; if (navMeshAgent != null) navMeshAgent.stoppingDistance = value; } }
    public bool LockNavMeshYPosition { get => lockNavMeshYPosition; set => lockNavMeshYPosition = value; }

    // Mouse / touch look
    public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = value; }
    public float VerticalLookLimit { get => verticalLookLimit; set => verticalLookLimit = value; }
    public bool InvertY { get => invertY; set => invertY = value; }
    public bool InvertRotation { get => invertRotation; set => invertRotation = value; }
    public float TouchSensitivity { get => touchSensitivity; set => touchSensitivity = value; }

    // Vertical camera rotation
    public bool UseVerticalCameraRotation { get => useVerticalCameraRotation; set => useVerticalCameraRotation = value; }

    // Game controls / mobile
    public bool EnableGameControls { get => enableGameControls; set => enableGameControls = value; }
    public bool UseNewMobileControl { get => useNewMobileControl; set => useNewMobileControl = value; }
    public float PinchMoveSensitivity { get => pinchMoveSensitivity; set => pinchMoveSensitivity = value; }
    public float PinchReferenceDistance { get => pinchReferenceDistance; set => pinchReferenceDistance = value; }
    public float SwipeStrafeSensitivity { get => swipeStrafeSensitivity; set => swipeStrafeSensitivity = value; }
    public float SwipeStrafeReferenceDistance { get => swipeStrafeReferenceDistance; set => swipeStrafeReferenceDistance = value; }

    // Click-to-move
    public bool EnableClickToMove { get => enableClickToMove; set => enableClickToMove = value; }
    public float ClickMoveLerpSpeed { get => clickMoveLerpSpeed; set => clickMoveLerpSpeed = value; }
    public float ClickMoveStopDistance { get => clickMoveStopDistance; set => clickMoveStopDistance = value; }
    public float ClickMoveMaxClickDistance { get => clickMoveMaxClickDistance; set => clickMoveMaxClickDistance = value; }

    // Mouse drag / smoothing
    public bool RequireClickToRotate { get => requireClickToRotate; set => requireClickToRotate = value; }
    public int MouseButtonForRotation { get => mouseButtonForRotation; set => mouseButtonForRotation = value; }
    public float MovementSmoothing { get => movementSmoothing; set => movementSmoothing = value; }
    public float LookSmoothing { get => lookSmoothing; set => lookSmoothing = value; }


    /// <summary>
    /// Clears movement/look inputs and velocities.
    /// Call this before externally tweening the player (e.g., DisplayWall.FocusPlayer)
    /// so there is no residual smoothed input that causes a small jerk when control resumes.
    /// </summary>
    public void ResetInputAndVelocity()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        smoothedMoveInput = Vector2.zero;
        smoothedLookInput = Vector2.zero;
        velocity = Vector3.zero;

        // Clear drag/touch state so no stale flags drive input on re-enable
        isMouseDragging = false;
        isMouseMovementDragging = false;
        isTouching = false;
        leftTouchId = -1;
        rightTouchId = -1;
        isPinchActive = false;
        isSwipeActive = false;

        // Also stop click-to-move if it happened to be active
        isClickMoving = false;

        // Stop NavMeshAgent movement if using NavMesh
        if (useNavMeshAgent && navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Instantly orients the player + camera so the camera looks directly at a world-space point.
    /// Used after focusing a DisplayWall so the painting center is exactly in view.
    /// When useVerticalCameraRotation is enabled, rotates the camera to look at the target.
    /// </summary>
    public void SnapLookAt(Vector3 worldPoint)
    {
        if (playerCamera == null)
            return;

        Vector3 camPos = playerCamera.transform.position;
        Vector3 toTarget = worldPoint - camPos;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        if (useVerticalCameraRotation)
        {
            // Full look rotation from camera to target
            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            Vector3 euler = lookRot.eulerAngles;

            // Convert to signed pitch (-180..180) for clamping. We let yaw be handled elsewhere (e.g. DisplayWall tween).
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;

            // Apply pitch to camera (X axis), respecting vertical clamp
            verticalRotation = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    /// <summary>
    /// Smoothly adjusts the camera pitch so it looks at a world-space point over a short duration.
    /// Used after a focus tween so the final adjustment is not a hard snap.
    /// When useVerticalCameraRotation is enabled, smoothly rotates camera pitch to look at target.
    /// </summary>
    public void SmoothLookAt(Vector3 worldPoint, float duration, Ease ease)
    {
        if (playerCamera == null)
            return;

        Vector3 camPos = playerCamera.transform.position;
        Vector3 toTarget = worldPoint - camPos;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        // Kill any previous pitch tween so we don't stack tweens
        if (lookPitchTween != null && lookPitchTween.IsActive())
        {
            lookPitchTween.Kill();
        }

        if (useVerticalCameraRotation)
        {
            // Desired look rotation from camera to target
            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            Vector3 euler = lookRot.eulerAngles;

            // Target pitch in signed form (-180..180), then clamped to verticalLookLimit
            float rawPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            float targetPitch = Mathf.Clamp(rawPitch, -verticalLookLimit, verticalLookLimit);

            float startPitch = verticalRotation;

            lookPitchTween = DOTween.To(
                    () => startPitch,
                    v =>
                    {
                        startPitch = v;
                        verticalRotation = startPitch;
                        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
                    },
                    targetPitch,
                    duration)
                .SetEase(ease);
        }
    }

    #endregion
}
