using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

/// <summary>
/// Attach this to a Display Wall. When the wall (or one of its frames) is clicked via
/// FirstPersonController's click-to-move system, the player will move to a standing
/// point in front of the wall and face the artwork.
/// </summary>
public class DisplayWall : MonoBehaviour
{
    public enum WallFacingDirection
    {
        Right,
        Left,
        Back,
        Front
    }

    [Header("Wall Info")]
    [Tooltip("Logical ID of this wall (e.g. 1, 2, 3 ...). Used by ArtworkManagerNew.")]
    public int displayWallId;

    [Tooltip("If true, use DisplayWallSlot children to position paintings based on slotIndex and slotSpan from JSON.")]
    [SerializeField] private bool useSlots;

    /// <summary>
    /// Returns true if this wall is configured to use slot-based positioning.
    /// </summary>
    public bool UseSlots => useSlots;

    [Header("Standing / View Points (Legacy)")]
    [Tooltip("Optional fixed standing point in front of this wall (legacy flow). If null, frame-based standing positions are used.")]
    public Transform standingPoint;

    [Tooltip("Optional explicit look-at target. If null, the clicked frame position is used.")]
    public Transform lookTarget;

    [Header("Wall Orientation for Frame Standing Point")]
    [Tooltip("Orientation of this wall. Used to compute where the player stands when clicking a frame.")]
    public WallFacingDirection facingDirection = WallFacingDirection.Front;

    [Tooltip("Distance in meters from the frame where the player should stand.")]
    [SerializeField] private float standingOffsetDistance = 1.85f;

    [Header("Player Reference (optional)")]
    [Tooltip("Optional explicit reference to the FirstPersonController. If null, it will be found at runtime.")]
    public FirstPersonController playerController;

    [Header("Focus Movement")]
    [SerializeField] private bool useTween = true;
    [SerializeField] private float moveDuration = 0.75f;
    [SerializeField] private Ease moveEase = Ease.OutSine;

    private Tween currentTween;
    private Coroutine navMeshFocusCoroutine;

    /// <summary>
    /// Legacy: focus this wall at its predefined standing point.
    /// </summary>
    public void FocusPlayer()
    {
        if (standingPoint == null)
        {
            Debug.LogWarning($"DisplayWall '{name}': No standingPoint assigned.");
            return;
        }

        Vector3 lookPos = (lookTarget != null) ? lookTarget.position : transform.position;
        FocusInternal(standingPoint.position, lookPos);
    }

    /// <summary>
    /// Focus the player on a specific ArtworkFrame that belongs to this wall.
    /// Uses the wall's facingDirection and standingOffsetDistance to compute a
    /// world-space standing position relative to the frame.
    ///
    /// For the new JSON pipeline: as soon as this focus is triggered by clicking a frame,
    /// we immediately push its PaintingConfigNew into InformationScreenUiManager so the
    /// info UI updates right away, without waiting for the player to reach the standing
    /// position.
    /// </summary>
    public void FocusPlayerOnFrame(ArtworkFrame frame)
    {
        if (frame == null)
        {
            Debug.LogWarning($"DisplayWall '{name}': FocusPlayerOnFrame called with null frame.");
            return;
        }

        // Immediately push JSON painting info for this frame to the info UI if possible.
        if (frame.DebugPaintingData != null)
        {
            if (InformationScreenRouter.Instance != null)
            {
                InformationScreenRouter.Instance.SetPaintingOnActive(frame.DebugPaintingData);
            }
            else if (InformationScreenUiManager.Instance != null)
            {
                // Fallback: direct singleton if router is not configured.
                InformationScreenUiManager.Instance.SetPainting(frame.DebugPaintingData);
            }
        }

        // Compute target standing position in world space
        Vector3 targetPos = ComputeStandingPositionForFrame(frame);

        // Get player
        var controller = playerController ?? FindObjectOfType<FirstPersonController>();
        if (controller == null)
        {
            Debug.LogWarning($"DisplayWall '{name}': No FirstPersonController found in scene.");
            return;
        }

        // Keep current player height so we don't pop up/down
        var controllerTransform = controller.transform;
        targetPos.y = controllerTransform.position.y;

        // Default look position is the frame's center unless a custom lookTarget is provided
        Vector3 lookPos = (lookTarget != null) ? lookTarget.position : frame.transform.position;

#if UNITY_EDITOR
        Debug.DrawLine(controllerTransform.position, targetPos, Color.cyan, 1.0f);
        Debug.DrawLine(targetPos, lookPos, Color.yellow, 1.0f);
        Debug.Log($"[DisplayWall] FocusPlayerOnFrame -> Frame={frame.transform.position}, Stand={targetPos}, Facing={facingDirection}");
#endif

        FocusInternal(targetPos, lookPos);
    }

    /// <summary>
    /// Core movement logic that moves/rotates the player to <paramref name="targetPosition"/>
    /// and makes the camera look at <paramref name="lookPosition"/>.
    /// </summary>
    private void FocusInternal(Vector3 targetPosition, Vector3 lookPosition)
    {
        var controller = playerController ?? FindObjectOfType<FirstPersonController>();
        if (controller == null)
        {
            Debug.LogWarning("DisplayWall: No FirstPersonController found in scene.");
            return;
        }

        var controllerTransform = controller.transform;
        var characterController = controller.GetComponent<CharacterController>();
        var navMeshAgent = controller.NavMeshAgent;
        bool useNavMesh = controller.UseNavMeshAgent && navMeshAgent != null && navMeshAgent.enabled;

        // Stop any existing focus tween or coroutine
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        if (navMeshFocusCoroutine != null)
        {
            StopCoroutine(navMeshFocusCoroutine);
            navMeshFocusCoroutine = null;
        }

        // Clear any residual movement/rotation input
        controller.ResetInputAndVelocity();

        // Keep current height
        targetPosition.y = controllerTransform.position.y;

        // Compute yaw-only rotation so that from the *standing point* we face the look position
        Vector3 toTarget = lookPosition - targetPosition;
        toTarget.y = 0f;
        Quaternion targetRotation;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }
        else
        {
            // Fallback: preserve only current yaw, zero out X and Z
            float currentYaw = controllerTransform.eulerAngles.y;
            targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        // If using NavMeshAgent, use pathfinding instead of tween
        if (useNavMesh)
        {
            navMeshFocusCoroutine = StartCoroutine(NavMeshFocusCoroutine(controller, navMeshAgent, targetPosition, targetRotation, lookPosition));
            return;
        }

        if (!useTween)
        {
            // Instant snap: disable movement controllers to avoid physics interference
            bool ccWasEnabled = characterController != null && characterController.enabled;

            if (characterController != null) characterController.enabled = false;

            controllerTransform.SetPositionAndRotation(targetPosition, targetRotation);

            if (characterController != null) characterController.enabled = ccWasEnabled;

            if (!controller.enabled)
            {
                controller.enabled = true;
            }

            controller.SmoothLookAt(lookPosition, 0.1f, Ease.OutSine);
            return;
        }

        // Tweened movement: if we're already at the target X/Z (within a small tolerance),
        // skip the move tween entirely and just rotate + adjust camera immediately.
        Vector3 currentPos = controllerTransform.position;
        Vector3 currentXZ = new Vector3(currentPos.x, 0f, currentPos.z);
        Vector3 targetXZ = new Vector3(targetPosition.x, 0f, targetPosition.z);
        bool alreadyAtXZ = Vector3.SqrMagnitude(currentXZ - targetXZ) <= 0.0001f; // ~1 cm tolerance

        if (alreadyAtXZ)
        {
            // No need to move; just snap yaw and smooth the pitch.
            controllerTransform.rotation = targetRotation;
            float pitchDuration = Mathf.Max(0.05f, moveDuration * 0.35f);
            controller.SmoothLookAt(lookPosition, pitchDuration, moveEase);
            return;
        }

        // We need to move: disable CharacterController while tweening
        bool restoreCharacterController = false;
        if (characterController != null && characterController.enabled)
        {
            restoreCharacterController = true;
            characterController.enabled = false;
        }

        // Compute yaw-only rotation for the tween (no pitch on the player)
        Vector3 tweenLookDir = lookPosition - targetPosition;
        tweenLookDir.y = 0f;
        Quaternion tweenTargetRotation = tweenLookDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(tweenLookDir.normalized, Vector3.up)
            : Quaternion.Euler(0f, controllerTransform.eulerAngles.y, 0f);

        currentTween = DOTween.Sequence()
            .Join(controllerTransform.DOMove(targetPosition, moveDuration).SetEase(moveEase))
            .Join(controllerTransform.DORotateQuaternion(tweenTargetRotation, moveDuration).SetEase(moveEase))
            .OnComplete(() =>
            {
                if (restoreCharacterController && characterController != null)
                {
                    characterController.enabled = true;
                }

                if (controller != null)
                {
                    float pitchDuration = Mathf.Max(0.05f, moveDuration * 0.35f);
                    controller.SmoothLookAt(lookPosition, pitchDuration, moveEase);
                }

                currentTween = null;
            })
            .OnKill(() =>
            {
                if (restoreCharacterController && characterController != null)
                {
                    characterController.enabled = true;
                }
            });
    }

    /// <summary>
    /// Coroutine that handles focus movement using NavMeshAgent pathfinding.
    /// Waits for the agent to reach the destination, then rotates and adjusts camera.
    /// </summary>
    private System.Collections.IEnumerator NavMeshFocusCoroutine(
        FirstPersonController controller,
        NavMeshAgent navMeshAgent,
        Vector3 targetPosition,
        Quaternion targetRotation,
        Vector3 lookPosition)
    {
        // Set destination for pathfinding
        navMeshAgent.SetDestination(targetPosition);

        // Wait until path is calculated
        while (navMeshAgent.pathPending)
        {
            yield return null;
        }

        // Wait until agent reaches destination
        while (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + 0.1f)
        {
            yield return null;
        }

        // Stop the agent
        navMeshAgent.ResetPath();

        // Smoothly rotate to face the look position
        var controllerTransform = controller.transform;
        float rotationDuration = moveDuration * 0.5f;
        
        currentTween = controllerTransform.DORotateQuaternion(targetRotation, rotationDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                // Adjust camera pitch to look at the artwork
                float pitchDuration = Mathf.Max(0.05f, moveDuration * 0.35f);
                controller.SmoothLookAt(lookPosition, pitchDuration, moveEase);
                currentTween = null;
            });

        navMeshFocusCoroutine = null;
    }

    /// <summary>
    /// Computes the world-space standing position for a given frame using the wall's
    /// facingDirection and standingOffsetDistance. Offsets are in WORLD space:
    ///
    /// - Right  wall  => stand at (frame.x - distance, frame.y, frame.z)
    /// - Left   wall  => stand at (frame.x + distance, frame.y, frame.z)
    /// - Back   wall  => stand at (frame.x, frame.y, frame.z + distance)
    /// - Front  wall  => stand at (frame.x, frame.y, frame.z - distance)
    /// </summary>
    public Vector3 ComputeStandingPositionForFrame(ArtworkFrame frame)
    {
        Vector3 framePos = frame.transform.position;
        Vector3 offset = Vector3.zero;

        switch (facingDirection)
        {
            case WallFacingDirection.Right:
                offset = new Vector3(-standingOffsetDistance, 0f, 0f);
                break;
            case WallFacingDirection.Left:
                offset = new Vector3(standingOffsetDistance, 0f, 0f);
                break;
            case WallFacingDirection.Back:
                offset = new Vector3(0f, 0f, standingOffsetDistance);
                break;
            case WallFacingDirection.Front:
                offset = new Vector3(0f, 0f, -standingOffsetDistance);
                break;
        }

        return framePos + offset;
    }

    /// <summary>
    /// Finds all DisplayWallSlot children of this wall and returns them sorted by slotId.
    /// </summary>
    public List<DisplayWallSlot> GetAllSlots()
    {
        var slots = GetComponentsInChildren<DisplayWallSlot>();
        var sortedList = new List<DisplayWallSlot>(slots);
        sortedList.Sort((a, b) => a.SlotId.CompareTo(b.SlotId));
        return sortedList;
    }

    /// <summary>
    /// Finds a DisplayWallSlot child with the given slotId.
    /// Returns null if not found.
    /// </summary>
    public DisplayWallSlot GetSlotById(int slotId)
    {
        var slots = GetComponentsInChildren<DisplayWallSlot>();
        foreach (var slot in slots)
        {
            if (slot.SlotId == slotId)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Calculates the center position between multiple slots based on startSlot and slotSpan.
    /// If slotSpan is 2 and startSlot is 0, this finds slots 0 and 1 and returns the center.
    /// Returns Vector3.zero if slots are not found.
    /// </summary>
    public Vector3 CalculateSlotCenterPosition(int startSlot, int slotSpan)
    {
        if (slotSpan <= 0)
        {
            Debug.LogWarning($"DisplayWall '{name}': Invalid slotSpan {slotSpan}. Must be greater than 0.");
            return Vector3.zero;
        }

        // Collect all slots in the span range
        List<Transform> slotTransforms = new List<Transform>();
        for (int i = startSlot; i < startSlot + slotSpan; i++)
        {
            var slot = GetSlotById(i);
            if (slot != null)
            {
                slotTransforms.Add(slot.transform);
            }
            else
            {
                Debug.LogWarning($"DisplayWall '{name}': Slot with ID {i} not found when calculating position for startSlot={startSlot}, slotSpan={slotSpan}.");
            }
        }

        if (slotTransforms.Count == 0)
        {
            Debug.LogError($"DisplayWall '{name}': No slots found in range [{startSlot}, {startSlot + slotSpan - 1}].");
            return Vector3.zero;
        }

        // Calculate the average position (center) of all slot transforms
        Vector3 sum = Vector3.zero;
        foreach (var slotTransform in slotTransforms)
        {
            sum += slotTransform.position;
        }

        return sum / slotTransforms.Count;
    }
}
