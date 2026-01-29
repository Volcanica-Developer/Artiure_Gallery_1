using UnityEngine;
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

        // Stop any existing focus tween
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }

        // Clear any residual movement/rotation input
        controller.ResetInputAndVelocity();

        // Keep current height
        targetPosition.y = controllerTransform.position.y;

        // Compute yaw-only rotation so that from the *standing point* we face the look position
        Vector3 toTarget = lookPosition - targetPosition;
        toTarget.y = 0f;
        Quaternion targetRotation = toTarget.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(toTarget.normalized, Vector3.up)
            : controllerTransform.rotation;

        if (!useTween)
        {
            // Instant snap: disable CharacterController to avoid physics interference
            if (characterController != null)
            {
                bool wasEnabled = characterController.enabled;
                characterController.enabled = false;
                controllerTransform.SetPositionAndRotation(targetPosition, targetRotation);
                characterController.enabled = wasEnabled;
            }
            else
            {
                controllerTransform.SetPositionAndRotation(targetPosition, targetRotation);
            }

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
        bool restoreController = false;
        if (characterController != null && characterController.enabled)
        {
            restoreController = true;
            characterController.enabled = false;
        }

        currentTween = DOTween.Sequence()
            .Join(controllerTransform.DOMove(targetPosition, moveDuration).SetEase(moveEase))
            .Join(controllerTransform.DORotateQuaternion(targetPosition == controllerTransform.position
                ? controllerTransform.rotation
                : Quaternion.LookRotation((lookPosition - targetPosition).normalized, Vector3.up),
                moveDuration).SetEase(moveEase))
            .OnComplete(() =>
            {
                if (restoreController && characterController != null)
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
                if (restoreController && characterController != null)
                {
                    characterController.enabled = true;
                }
            });
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
}
