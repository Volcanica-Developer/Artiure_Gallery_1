using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple runtime UI for tweaking FirstPersonController movement and look settings.
/// Attach this to a UI panel and wire up the sliders/toggles created by MovementSettingsUISetup
/// (or your own custom UI) and it will push values into the controller at runtime.
/// </summary>
public class MovementSettingsUI : MonoBehaviour
{
    [Header("Target")]
    public FirstPersonController controller;

    [Header("Basic Movement")]
    public Slider walkSpeedSlider;
    public Text walkSpeedValueText;
    public Slider runSpeedSlider;
    public Text runSpeedValueText;
    public Slider gravitySlider;
    public Text gravityValueText;

    [Header("Look Settings")]
    public Slider mouseSensitivitySlider;
    public Text mouseSensitivityValueText;
    public Slider verticalLookLimitSlider;
    public Text verticalLookLimitValueText;
    public Toggle invertYToggle;
    public Toggle invertRotationToggle;

    [Header("Smoothing")]
    public Slider movementSmoothingSlider;
    public Text movementSmoothingValueText;
    public Slider lookSmoothingSlider;
    public Text lookSmoothingValueText;

    [Header("Touch / Mobile")]
    public Slider touchSensitivitySlider;
    public Text touchSensitivityValueText;
    public Toggle enableGameControlsToggle;
    public Toggle useNewMobileControlToggle;

    [Header("Click To Move")]
    public Toggle enableClickToMoveToggle;
    public Slider clickMoveLerpSpeedSlider;
    public Text clickMoveLerpSpeedValueText;
    public Slider clickMoveStopDistanceSlider;
    public Text clickMoveStopDistanceValueText;
    public Slider clickMoveMaxClickDistanceSlider;
    public Text clickMoveMaxClickDistanceValueText;

    [Header("Mouse Drag Settings")]
    public Toggle requireClickToRotateToggle;
    public Dropdown mouseButtonForRotationDropdown; // 0 = Left, 1 = Right, 2 = Middle

    private bool _initializing;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<FirstPersonController>();
        }
    }

    private void Start()
    {
        if (controller == null)
        {
            Debug.LogWarning("MovementSettingsUI: No FirstPersonController found in scene.");
            return;
        }

        _initializing = true;

        // Basic movement
        if (walkSpeedSlider != null)
        {
            walkSpeedSlider.value = controller.WalkSpeed;
            walkSpeedSlider.onValueChanged.AddListener(OnWalkSpeedChanged);
            UpdateText(walkSpeedValueText, controller.WalkSpeed);
        }

        if (runSpeedSlider != null)
        {
            runSpeedSlider.value = controller.RunSpeed;
            runSpeedSlider.onValueChanged.AddListener(OnRunSpeedChanged);
            UpdateText(runSpeedValueText, controller.RunSpeed);
        }

        if (gravitySlider != null)
        {
            gravitySlider.value = controller.GravityValue;
            gravitySlider.onValueChanged.AddListener(OnGravityChanged);
            UpdateText(gravityValueText, controller.GravityValue);
        }

        // Look
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = controller.MouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            UpdateText(mouseSensitivityValueText, controller.MouseSensitivity);
        }

        if (verticalLookLimitSlider != null)
        {
            verticalLookLimitSlider.value = controller.VerticalLookLimit;
            verticalLookLimitSlider.onValueChanged.AddListener(OnVerticalLookLimitChanged);
            UpdateText(verticalLookLimitValueText, controller.VerticalLookLimit);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = controller.InvertY;
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        }

        if (invertRotationToggle != null)
        {
            invertRotationToggle.isOn = controller.InvertRotation;
            invertRotationToggle.onValueChanged.AddListener(OnInvertRotationChanged);
        }

        // Smoothing
        if (movementSmoothingSlider != null)
        {
            movementSmoothingSlider.value = controller.MovementSmoothing;
            movementSmoothingSlider.onValueChanged.AddListener(OnMovementSmoothingChanged);
            UpdateText(movementSmoothingValueText, controller.MovementSmoothing);
        }

        if (lookSmoothingSlider != null)
        {
            lookSmoothingSlider.value = controller.LookSmoothing;
            lookSmoothingSlider.onValueChanged.AddListener(OnLookSmoothingChanged);
            UpdateText(lookSmoothingValueText, controller.LookSmoothing);
        }

        // Touch / mobile
        if (touchSensitivitySlider != null)
        {
            touchSensitivitySlider.value = controller.TouchSensitivity;
            touchSensitivitySlider.onValueChanged.AddListener(OnTouchSensitivityChanged);
            UpdateText(touchSensitivityValueText, controller.TouchSensitivity);
        }

        if (enableGameControlsToggle != null)
        {
            enableGameControlsToggle.isOn = controller.EnableGameControls;
            enableGameControlsToggle.onValueChanged.AddListener(OnEnableGameControlsChanged);
        }

        if (useNewMobileControlToggle != null)
        {
            useNewMobileControlToggle.isOn = controller.UseNewMobileControl;
            useNewMobileControlToggle.onValueChanged.AddListener(OnUseNewMobileControlChanged);
        }

        // Click-to-move
        if (enableClickToMoveToggle != null)
        {
            enableClickToMoveToggle.isOn = controller.EnableClickToMove;
            enableClickToMoveToggle.onValueChanged.AddListener(OnEnableClickToMoveChanged);
        }

        if (clickMoveLerpSpeedSlider != null)
        {
            clickMoveLerpSpeedSlider.value = controller.ClickMoveLerpSpeed;
            clickMoveLerpSpeedSlider.onValueChanged.AddListener(OnClickMoveLerpSpeedChanged);
            UpdateText(clickMoveLerpSpeedValueText, controller.ClickMoveLerpSpeed);
        }

        if (clickMoveStopDistanceSlider != null)
        {
            clickMoveStopDistanceSlider.value = controller.ClickMoveStopDistance;
            clickMoveStopDistanceSlider.onValueChanged.AddListener(OnClickMoveStopDistanceChanged);
            UpdateText(clickMoveStopDistanceValueText, controller.ClickMoveStopDistance);
        }

        if (clickMoveMaxClickDistanceSlider != null)
        {
            clickMoveMaxClickDistanceSlider.value = controller.ClickMoveMaxClickDistance;
            clickMoveMaxClickDistanceSlider.onValueChanged.AddListener(OnClickMoveMaxClickDistanceChanged);
            UpdateText(clickMoveMaxClickDistanceValueText, controller.ClickMoveMaxClickDistance);
        }

        // Mouse drag settings
        if (requireClickToRotateToggle != null)
        {
            requireClickToRotateToggle.isOn = controller.RequireClickToRotate;
            requireClickToRotateToggle.onValueChanged.AddListener(OnRequireClickToRotateChanged);
        }

        if (mouseButtonForRotationDropdown != null)
        {
            mouseButtonForRotationDropdown.value = Mathf.Clamp(controller.MouseButtonForRotation, 0, 2);
            mouseButtonForRotationDropdown.onValueChanged.AddListener(OnMouseButtonForRotationChanged);
        }

        _initializing = false;
    }

    #region Slider callbacks

    private void OnWalkSpeedChanged(float value)
    {
        if (controller == null) return;
        controller.WalkSpeed = value;
        UpdateText(walkSpeedValueText, value);
    }

    private void OnRunSpeedChanged(float value)
    {
        if (controller == null) return;
        controller.RunSpeed = value;
        UpdateText(runSpeedValueText, value);
    }

    private void OnGravityChanged(float value)
    {
        if (controller == null) return;
        controller.GravityValue = value;
        UpdateText(gravityValueText, value);
    }

    private void OnMouseSensitivityChanged(float value)
    {
        if (controller == null) return;
        controller.MouseSensitivity = value;
        UpdateText(mouseSensitivityValueText, value);
    }

    private void OnVerticalLookLimitChanged(float value)
    {
        if (controller == null) return;
        controller.VerticalLookLimit = value;
        UpdateText(verticalLookLimitValueText, value);
    }

    private void OnMovementSmoothingChanged(float value)
    {
        if (controller == null) return;
        controller.MovementSmoothing = value;
        UpdateText(movementSmoothingValueText, value);
    }

    private void OnLookSmoothingChanged(float value)
    {
        if (controller == null) return;
        controller.LookSmoothing = value;
        UpdateText(lookSmoothingValueText, value);
    }

    private void OnTouchSensitivityChanged(float value)
    {
        if (controller == null) return;
        controller.TouchSensitivity = value;
        UpdateText(touchSensitivityValueText, value);
    }

    private void OnClickMoveLerpSpeedChanged(float value)
    {
        if (controller == null) return;
        controller.ClickMoveLerpSpeed = value;
        UpdateText(clickMoveLerpSpeedValueText, value);
    }

    private void OnClickMoveStopDistanceChanged(float value)
    {
        if (controller == null) return;
        controller.ClickMoveStopDistance = value;
        UpdateText(clickMoveStopDistanceValueText, value);
    }

    private void OnClickMoveMaxClickDistanceChanged(float value)
    {
        if (controller == null) return;
        controller.ClickMoveMaxClickDistance = value;
        UpdateText(clickMoveMaxClickDistanceValueText, value);
    }

    #endregion

    #region Toggle / dropdown callbacks

    private void OnInvertYChanged(bool value)
    {
        if (controller == null) return;
        controller.InvertY = value;
    }

    private void OnInvertRotationChanged(bool value)
    {
        if (controller == null) return;
        controller.InvertRotation = value;
    }

    private void OnEnableGameControlsChanged(bool value)
    {
        if (controller == null) return;
        controller.EnableGameControls = value;
    }

    private void OnUseNewMobileControlChanged(bool value)
    {
        if (controller == null) return;
        controller.UseNewMobileControl = value;
    }

    private void OnEnableClickToMoveChanged(bool value)
    {
        if (controller == null) return;
        controller.EnableClickToMove = value;
    }

    private void OnRequireClickToRotateChanged(bool value)
    {
        if (controller == null) return;
        controller.RequireClickToRotate = value;
    }

    private void OnMouseButtonForRotationChanged(int value)
    {
        if (controller == null) return;
        controller.MouseButtonForRotation = Mathf.Clamp(value, 0, 2);
    }

    #endregion

    private void UpdateText(Text textComponent, float value)
    {
        if (textComponent == null) return;
        textComponent.text = value.ToString("0.00");
    }
}
