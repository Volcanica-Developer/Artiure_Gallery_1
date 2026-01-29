using UnityEngine;

/// <summary>
/// Routes painting information updates to the correct InformationScreenUiManager
/// (desktop vs mobile) based on the current platform. Assign both references in
/// the inspector.
///
/// Gameplay code should prefer using InformationScreenRouter.Instance or the
/// static helper methods instead of talking directly to a specific
/// InformationScreenUiManager.
/// </summary>
public class InformationScreenRouter : MonoBehaviour
{
    public static InformationScreenRouter Instance { get; private set; }

    [Header("Information Screen Targets")]
    [Tooltip("Information UI manager used on desktop / non-mobile platforms.")]
    [SerializeField] private InformationScreenUiManager desktopManager;

    [Tooltip("Information UI manager used on mobile platforms.")]
    [SerializeField] private InformationScreenUiManager mobileManager;

    [Header("Override (Optional)")]
    [Tooltip("If true, override Application.isMobilePlatform for testing.")]
    [SerializeField] private bool overridePlatform = false;

    [Tooltip("When overridePlatform is true, treat runtime as mobile when this is true.")]
    [SerializeField] private bool forceMobile = false;

    [Header("Runtime Behaviour")]
    [Tooltip("If true, logs when the active information screen manager changes due to orientation/platform.")]
    [SerializeField] private bool logActiveManagerChanges = false;

    private InformationScreenUiManager _lastActiveManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple InformationScreenRouter instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Initialize cached active manager.
        _lastActiveManager = GetActiveManager();
    }

    private void Update()
    {
        // Detect changes in platform/orientation at runtime and react.
        var current = GetActiveManager();
        if (current != _lastActiveManager)
        {
            if (logActiveManagerChanges)
            {
                Debug.Log($"[InformationScreenRouter] Active manager changed from '{FormatManagerName(_lastActiveManager)}' to '{FormatManagerName(current)}'.", this);
            }

            // If we switch managers and the previous one had a painting, copy it over.
            if (_lastActiveManager != null && current != null)
            {
                var painting = _lastActiveManager.GetCurrentPainting();
                if (painting != null)
                {
                    current.SetPainting(painting);
                }
            }

            _lastActiveManager = current;
        }
    }

    private static string FormatManagerName(InformationScreenUiManager mgr)
    {
        return mgr == null ? "null" : mgr.name;
    }

    /// <summary>
    /// Returns the active InformationScreenUiManager for the current platform,
    /// or null if none is configured.
    /// </summary>
    public InformationScreenUiManager GetActiveManager()
    {
        bool isMobile = overridePlatform ? forceMobile : Application.isMobilePlatform;

        // If we're on a mobile device but currently in landscape, treat it like desktop
        // for the purposes of the information screen.
        bool isLandscapeOnMobile = isMobile &&
                                   (Screen.orientation == ScreenOrientation.LandscapeLeft ||
                                    Screen.orientation == ScreenOrientation.LandscapeRight);

        if (isLandscapeOnMobile)
        {
            // Force use of the desktop manager when in landscape on mobile.
            return desktopManager != null ? desktopManager : mobileManager;
        }

        if (isMobile)
        {
            return mobileManager != null ? mobileManager : desktopManager;
        }

        // Desktop / non-mobile
        return desktopManager != null ? desktopManager : mobileManager;
    }

    /// <summary>
    /// Convenience helper to set the painting on the active info UI.
    /// Safe to call even if router or managers are missing.
    /// </summary>
    public void SetPaintingOnActive(PaintingConfigNew painting)
    {
        if (painting == null)
            return;

        var mgr = GetActiveManager();
        if (mgr != null)
        {
            mgr.SetPainting(painting);
            return;
        }

        // Fallback: try direct singleton if no suitable manager was found.
        if (InformationScreenUiManager.Instance != null)
        {
            InformationScreenUiManager.Instance.SetPainting(painting);
        }
    }
}
