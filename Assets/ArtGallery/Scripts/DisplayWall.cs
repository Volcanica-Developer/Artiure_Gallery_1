using UnityEngine;
using DG.Tweening;

/// <summary>
/// Attach this to a Display Wall. When the wall is clicked via FirstPersonController's
/// click-to-move system, the player will move to the configured standing point and
/// face the wall.
///
/// Usage:
/// - Add this component to your wall (the object with the collider).
/// - Assign a Standing Point transform (usually an empty child placed in front of the wall).
/// - Optional: assign a Look Target (otherwise the wall's own transform position is used).
/// - Make sure the wall's layer is included in either clickMoveLayerMask or
///   clickMoveIgnoreLayers on FirstPersonController so its collider is hit by the ray.
/// </summary>
public class DisplayWall : MonoBehaviour
{
    [Header("Wall Info")]
    [Tooltip("Where the player should stand when focusing this wall.")]
    public int displayWallId;

    [Header("Standing / View Points")]
    [Tooltip("Where the player should stand when focusing this wall.")]
    public Transform standingPoint;

    [Tooltip("Where the player should look. If null, uses this wall's transform position.")]
    public Transform lookTarget;

    [Header("Player Reference (optional)")]
    [Tooltip("Optional explicit reference to the FirstPersonController. If left empty, it will be auto-found at runtime.")]
    public FirstPersonController playerController;

    [Header("Movement Tween")] 
    [SerializeField] private bool useTween = true;
    [SerializeField] private float moveDuration = 0.75f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    private Tween currentTween;

    /// <summary>
    /// Called by FirstPersonController when this wall is clicked.
    /// Smoothly moves the player to the standing point and rotates to face the wall.
    /// </summary>
    public void FocusPlayer()
    {
        if (standingPoint == null)
        {
            Debug.LogWarning($"DisplayWallStandPoint on '{name}' has no Standing Point assigned.");
            return;
        }

        var controller = playerController;
        if (controller == null)
        {
            controller = FindFirstObjectByType<FirstPersonController>();
            if (controller == null)
            {
                Debug.LogWarning("DisplayWallStandPoint: No FirstPersonController found in the scene.");
                return;
            }
        }

        var controllerTransform = controller.transform;
        var characterController = controller.GetComponent<CharacterController>();

        // Clear any residual movement / look input so there is no small jerk
        // from smoothed input when control is returned after the tween.
        controller.ResetInputAndVelocity();

        // Cancel any existing tween
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }

        // Compute desired position and rotation
        Vector3 targetPosition = standingPoint.position;
        // Keep the player's current Y so focusing a wall never changes vertical height.
        targetPosition.y = controllerTransform.position.y;

        Vector3 lookPos = lookTarget != null ? lookTarget.position : transform.position;
        Vector3 flatDir = lookPos - targetPosition;
        flatDir.y = 0f;
        Quaternion targetRotation = flatDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flatDir.normalized, Vector3.up)
            : controllerTransform.rotation;

        // If tweening is disabled, snap instantly to the standing point / look direction.
        if (!useTween)
        {
            // Make sure physics doesn't fight with a teleport.
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

            // Ensure controller is enabled for immediate input
            if (!controller.enabled)
            {
                controller.enabled = true;
            }

            // After snapping position, also orient camera exactly toward the look target
            controller.SnapLookAt(lookPos);

            currentTween = null;
            return;
        }

        // Tween position and rotation together (keep controller enabled so it never appears disabled in Inspector)
        currentTween = DOTween.Sequence()
            .Join(controllerTransform.DOMove(targetPosition, moveDuration).SetEase(moveEase))
            .Join(controllerTransform.DORotateQuaternion(targetRotation, moveDuration).SetEase(moveEase))
            .OnComplete(() =>
            {
                // When the tween finishes, smoothly adjust camera pitch so it looks precisely at the painting center
                if (controller != null)
                {
                    // Use a shorter duration than the main move so the final adjustment feels quick, not "settling".
                    float pitchDuration = Mathf.Max(0.05f, moveDuration * 0.35f);
                    controller.SmoothLookAt(lookPos, pitchDuration, moveEase);
                }

                currentTween = null;
            });
    }
}
