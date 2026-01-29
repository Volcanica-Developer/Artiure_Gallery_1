using UnityEngine;

/// <summary>
/// Manages the information screen for a selected painting from the new JSON pipeline.
/// Stores the currently selected PaintingConfigNew so it can be reused by other UI flows.
///
/// You can have separate instances for desktop and mobile (for example, one on each
/// canvas). Use the <see cref="PlatformMode"/> setting to control which instance is
/// considered active on which platform. Gameplay code continues to use the static
/// <see cref="Instance"/> property and does not need to know about the variants.
/// </summary>
public class InformationScreenUiManager : MonoBehaviour
{
    public enum PlatformMode
    {
        Auto,       // Valid on all platforms (default)
        DesktopOnly,
        MobileOnly
    }

    /// <summary>
    /// Simple singleton-style accessor so gameplay code can easily send a PaintingConfigNew here.
    /// It will point to the best-matching instance for the current platform.
    /// </summary>
    public static InformationScreenUiManager Instance { get; private set; }

    [Header("Platform Variant")]
    [Tooltip("If you have separate info UIs for desktop and mobile, set one instance to DesktopOnly and one to MobileOnly. Leave as Auto if this instance is valid everywhere.")]
    [SerializeField] private PlatformMode _platformMode = PlatformMode.Auto;

    [Header("Current Selection (New JSON)")]
    [Tooltip("The PaintingConfigNew currently selected by the player.")]
    [SerializeField] private PaintingConfigNew _currentPainting;

    private void Awake()
    {
        RegisterInstanceForPlatform();
    }

    private void OnEnable()
    {
        // In case this GameObject is re-enabled at runtime, ensure the correct instance is registered.
        RegisterInstanceForPlatform();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Register this component as the active Instance if it is a better match for the
    /// current platform than whatever is currently registered.
    /// </summary>
    private void RegisterInstanceForPlatform()
    {
        bool isMobile = Application.isMobilePlatform;

        if (!IsValidForCurrentPlatform(this, isMobile))
        {
            // This instance is not intended for the current platform; do not claim Instance.
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            return;
        }

        // If there is an existing instance but it is not valid for this platform,
        // or this instance is more specific (e.g. MobileOnly vs Auto on mobile),
        // prefer this one.
        bool existingValid = IsValidForCurrentPlatform(Instance, isMobile);

        if (!existingValid)
        {
            Instance = this;
            return;
        }

        // Both are valid for the current platform. Prefer the more specific mode:
        // DesktopOnly/MobileOnly beats Auto.
        int thisPriority = GetPlatformPriority(_platformMode, isMobile);
        int existingPriority = GetPlatformPriority(Instance._platformMode, isMobile);

        if (thisPriority > existingPriority)
        {
            Instance = this;
        }
    }

    private static bool IsValidForCurrentPlatform(InformationScreenUiManager mgr, bool isMobile)
    {
        if (mgr == null) return false;

        switch (mgr._platformMode)
        {
            case PlatformMode.DesktopOnly:
                return !isMobile;
            case PlatformMode.MobileOnly:
                return isMobile;
            default:
                return true;
        }
    }

    private static int GetPlatformPriority(PlatformMode mode, bool isMobile)
    {
        // Higher value means more specific/preferred for this platform.
        switch (mode)
        {
            case PlatformMode.DesktopOnly:
                return isMobile ? 0 : 2;
            case PlatformMode.MobileOnly:
                return isMobile ? 2 : 0;
            default:
                return 1; // Auto
        }
    }

    /// <summary>
    /// Called from ArtworkFrame when the player clicks a frame while standing at its target position.
    /// Stores the painting data and refreshes the UI.
    /// </summary>
    public void SetPainting(PaintingConfigNew painting)
    {
        _currentPainting = painting;
        UpdateUI();
    }

    /// <summary>
    /// Returns the last painting that was sent to this manager.
    /// </summary>
    public PaintingConfigNew GetCurrentPainting()
    {
        return _currentPainting;
    }

    /// <summary>
    /// Apply _currentPainting to your actual UI widgets (texts, images, etc.).
    /// Wire up concrete references here as needed.
    /// </summary>
    private void UpdateUI()
    {
        if (_currentPainting == null)
            return;

        // Example wiring (uncomment and assign references in the Inspector):
        // titleText.text = _currentPainting.name;
        // descriptionText.text = _currentPainting.description;
        // mainImage.sprite = ...; // from _currentPainting.mainImage or images list
    }
}
