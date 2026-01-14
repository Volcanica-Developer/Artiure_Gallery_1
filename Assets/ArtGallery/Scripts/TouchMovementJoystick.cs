using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple on-screen joystick for touch movement.
/// Outputs a normalized direction vector in 2D space.
/// </summary>
public class TouchMovementJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background; // Joystick background
    [SerializeField] private RectTransform handle;     // Joystick handle/knob
    [SerializeField] private float handleRange = 75f;  // Max handle distance in pixels

    /// <summary>
    /// Direction of the joystick in local space, from -1..1 on each axis.
    /// x = left/right, y = forward/back.
    /// </summary>
    public Vector2 Direction { get; private set; }

    /// <summary>
    /// True when the joystick is being moved.
    /// </summary>
    public bool HasInput => Direction.sqrMagnitude > 0.01f;

    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (background == null)
        {
            background = (RectTransform)transform;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || _canvas == null)
            return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
        {
            // Clamp to a circle of radius handleRange
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
            Direction = clamped / handleRange; // -1..1

            if (handle != null)
            {
                handle.anchoredPosition = clamped;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
