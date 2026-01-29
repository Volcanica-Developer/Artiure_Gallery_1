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

    /// <summary>
    /// Applies the platform-specific canvas visibility.
    /// On WebGL, Application.isMobilePlatform is true on mobile browsers
    /// (Android / iOS) and false on desktop browsers.
    /// </summary>
    private void ApplyPlatformCanvases()
    {
        bool isMobile = Application.isMobilePlatform;

        if (desktopCanvas != null)
        {
            desktopCanvas.SetActive(!isMobile);
        }

        if (mobileCanvas != null)
        {
            mobileCanvas.SetActive(isMobile);
        }
    }
}
