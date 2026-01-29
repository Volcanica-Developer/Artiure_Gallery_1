using UnityEngine;

/// <summary>
/// Enables the appropriate canvas for WebGL builds depending on whether
/// the app is running in a mobile or desktop browser.
/// Attach this to any GameObject in the scene and assign the canvases.
/// </summary>
public class CanvasPlatformSwitcher : MonoBehaviour
{
    [Header("Canvas References")]
    [Tooltip("Canvas (or root GameObject) used for desktop browsers")] 
    [SerializeField] private GameObject desktopCanvas;

    [Tooltip("Canvas (or root GameObject) used for mobile browsers")] 
    [SerializeField] private GameObject mobileCanvas;

    private void Awake()
    {
        ApplyPlatformCanvases();
    }

    private void OnEnable()
    {
        ApplyPlatformCanvases();
    }

    private void Update()
    {
        // Re-apply whenever orientation might have changed at runtime.
        ApplyPlatformCanvases();
    }

    /// <summary>
    /// Applies the platform-specific canvas visibility.
    /// On WebGL, Application.isMobilePlatform is true on mobile browsers
    /// (Android / iOS) and false on desktop browsers.
    /// In mobile landscape we intentionally show the desktop canvas.
    /// </summary>
    private void ApplyPlatformCanvases()
    {
        bool isMobile = Application.isMobilePlatform;
        bool isLandscapeOnMobile = isMobile &&
                                   (Screen.orientation == ScreenOrientation.LandscapeLeft ||
                                    Screen.orientation == ScreenOrientation.LandscapeRight);

        // In landscape on mobile, treat as desktop UI.
        bool useDesktop = !isMobile || isLandscapeOnMobile;
        bool useMobile = isMobile && !isLandscapeOnMobile;

        if (desktopCanvas != null)
        {
            desktopCanvas.SetActive(useDesktop);
        }

        if (mobileCanvas != null)
        {
            mobileCanvas.SetActive(useMobile);
        }
    }
}
