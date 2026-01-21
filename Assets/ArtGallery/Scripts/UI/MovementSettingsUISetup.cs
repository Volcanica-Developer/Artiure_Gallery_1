using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Helper to auto-generate a simple "landscape" movement settings UI for FirstPersonController.
/// Attach this to any GameObject in your scene and use the context menu
///   "Create Movement Settings UI (Landscape)"
/// in the inspector. It will:
/// - Find or create a Canvas + EventSystem
/// - Create a left-side (landscape style) panel with sliders/toggles
/// - Wire everything into a MovementSettingsUI component
/// </summary>
public class MovementSettingsUISetup : MonoBehaviour
{
    [Header("Optional Target Override")]
    [SerializeField] private FirstPersonController controllerOverride;

    [ContextMenu("Create Movement Settings UI (Landscape)")]
    public void CreateMovementSettingsUILandscape()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        // Create panel anchored to the left side (better for landscape aspect)
        GameObject panelGO = new GameObject("MovementSettingsPanel");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0.45f, 1f); // left ~45% of the screen
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(10f, 0f);
        panelRect.sizeDelta = new Vector2(-20f, -40f);

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        VerticalLayoutGroup layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        MovementSettingsUI ui = panelGO.AddComponent<MovementSettingsUI>();
        ui.controller = controllerOverride != null ? controllerOverride : FindObjectOfType<FirstPersonController>();

        // --- Build rows ---
        CreateHeader(panelGO.transform, "Movement Settings");

        // Basic movement
        CreateSubHeader(panelGO.transform, "Basic Movement");
        CreateSliderRow(panelGO.transform, "Walk Speed", 0f, 15f,
            ui.controller != null ? ui.controller.WalkSpeed : 5f,
            out ui.walkSpeedSlider, out ui.walkSpeedValueText);
        CreateSliderRow(panelGO.transform, "Run Speed", 0f, 20f,
            ui.controller != null ? ui.controller.RunSpeed : 8f,
            out ui.runSpeedSlider, out ui.runSpeedValueText);
        CreateSliderRow(panelGO.transform, "Gravity", -25f, 0f,
            ui.controller != null ? ui.controller.GravityValue : -9.81f,
            out ui.gravitySlider, out ui.gravityValueText);

        // Look
        CreateSubHeader(panelGO.transform, "Look");
        CreateSliderRow(panelGO.transform, "Mouse Sensitivity", 0.1f, 10f,
            ui.controller != null ? ui.controller.MouseSensitivity : 2f,
            out ui.mouseSensitivitySlider, out ui.mouseSensitivityValueText);
        CreateSliderRow(panelGO.transform, "Vertical Look Limit", 30f, 89f,
            ui.controller != null ? ui.controller.VerticalLookLimit : 80f,
            out ui.verticalLookLimitSlider, out ui.verticalLookLimitValueText);
        CreateToggleRow(panelGO.transform, "Invert Y", ui.controller != null && ui.controller.InvertY,
            out ui.invertYToggle);
        CreateToggleRow(panelGO.transform, "Invert Rotation", ui.controller != null && ui.controller.InvertRotation,
            out ui.invertRotationToggle);

        // Smoothing
        CreateSubHeader(panelGO.transform, "Smoothing");
        CreateSliderRow(panelGO.transform, "Movement Smoothing", 0f, 20f,
            ui.controller != null ? ui.controller.MovementSmoothing : 10f,
            out ui.movementSmoothingSlider, out ui.movementSmoothingValueText);
        CreateSliderRow(panelGO.transform, "Look Smoothing", 0f, 30f,
            ui.controller != null ? ui.controller.LookSmoothing : 15f,
            out ui.lookSmoothingSlider, out ui.lookSmoothingValueText);

        // Touch / mobile
        CreateSubHeader(panelGO.transform, "Touch / Mobile");
        CreateSliderRow(panelGO.transform, "Touch Sensitivity", 0.1f, 5f,
            ui.controller != null ? ui.controller.TouchSensitivity : 0.5f,
            out ui.touchSensitivitySlider, out ui.touchSensitivityValueText);
        CreateToggleRow(panelGO.transform, "Enable Game Controls", ui.controller != null && ui.controller.EnableGameControls,
            out ui.enableGameControlsToggle);
        CreateToggleRow(panelGO.transform, "Use New Mobile Control", ui.controller != null && ui.controller.UseNewMobileControl,
            out ui.useNewMobileControlToggle);

        // Click-to-move
        CreateSubHeader(panelGO.transform, "Click To Move");
        CreateToggleRow(panelGO.transform, "Enable Click To Move", ui.controller != null && ui.controller.EnableClickToMove,
            out ui.enableClickToMoveToggle);
        CreateSliderRow(panelGO.transform, "Click Move Lerp Speed", 1f, 15f,
            ui.controller != null ? ui.controller.ClickMoveLerpSpeed : 5f,
            out ui.clickMoveLerpSpeedSlider, out ui.clickMoveLerpSpeedValueText);
        CreateSliderRow(panelGO.transform, "Click Move Stop Distance", 0.01f, 0.5f,
            ui.controller != null ? ui.controller.ClickMoveStopDistance : 0.05f,
            out ui.clickMoveStopDistanceSlider, out ui.clickMoveStopDistanceValueText);
        CreateSliderRow(panelGO.transform, "Click Max Click Distance", 5f, 200f,
            ui.controller != null ? ui.controller.ClickMoveMaxClickDistance : 25f,
            out ui.clickMoveMaxClickDistanceSlider, out ui.clickMoveMaxClickDistanceValueText);

        // Mouse drag
        CreateSubHeader(panelGO.transform, "Mouse Drag");
        CreateToggleRow(panelGO.transform, "Require Click To Rotate", ui.controller != null && ui.controller.RequireClickToRotate,
            out ui.requireClickToRotateToggle);

        Debug.Log("MovementSettingsUISetup: Movement settings UI created.");
    }

    private Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
            return canvas;

        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    private void CreateHeader(Transform parent, string text)
    {
        GameObject go = new GameObject("Header");
        go.transform.SetParent(parent, false);
        Text label = go.AddComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = 20;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 30f;
    }

    private void CreateSubHeader(Transform parent, string text)
    {
        GameObject go = new GameObject(text + " Header");
        go.transform.SetParent(parent, false);
        Text label = go.AddComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = 16;
        label.color = new Color(1f, 1f, 1f, 0.85f);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;
    }

    private void CreateSliderRow(Transform parent, string labelText, float min, float max, float defaultValue,
        out Slider slider, out Text valueText)
    {
        GameObject row = new GameObject(labelText + " Row");
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(row.transform, false);
        Text label = labelGO.AddComponent<Text>();
        label.text = labelText;
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = 14;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 160f;

        // Slider
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(200f, 20f);

        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(defaultValue, min, max);
        slider.direction = Slider.Direction.LeftToRight;

        // Simple visuals: background + handle
        Image bg = sliderGO.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.15f);

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 16f);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        LayoutElement sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 220f;

        // Value text
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(row.transform, false);
        valueText = valueGO.AddComponent<Text>();
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.fontSize = 14;
        valueText.color = Color.white;
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        LayoutElement valueLE = valueGO.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 60f;
    }

    private void CreateToggleRow(Transform parent, string labelText, bool defaultValue, out Toggle toggle)
    {
        GameObject row = new GameObject(labelText + " Row");
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        // Toggle
        GameObject toggleGO = new GameObject("Toggle");
        toggleGO.transform.SetParent(row.transform, false);
        toggle = toggleGO.AddComponent<Toggle>();

        // Background + checkmark
        Image bg = toggleGO.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.15f);

        GameObject checkmarkGO = new GameObject("Checkmark");
        checkmarkGO.transform.SetParent(toggleGO.transform, false);
        Image checkmarkImg = checkmarkGO.AddComponent<Image>();
        checkmarkImg.color = Color.white;

        toggle.graphic = checkmarkImg;
        toggle.targetGraphic = bg;
        toggle.isOn = defaultValue;

        LayoutElement toggleLE = toggleGO.AddComponent<LayoutElement>();
        toggleLE.preferredWidth = 24f;
        toggleLE.preferredHeight = 24f;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(row.transform, false);
        Text label = labelGO.AddComponent<Text>();
        label.text = labelText;
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = 14;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
