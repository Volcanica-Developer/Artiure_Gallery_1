using UnityEngine;

public class StartPanelUiManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArtworkManagerNew artworkManager;
    [SerializeField] private GameObject startBtn;
    [SerializeField] private GameObject loadingBar;

    /// <summary>
    /// Called from the Start button OnClick. Triggers the API load on ArtworkManagerNew
    /// and hides the start panel.
    /// </summary>
    public void OnStartButtonClicked()
    {

        if (loadingBar != null)
        {
            loadingBar.SetActive(true);
        }

        if (artworkManager != null)
        {
            // Force API-based loading regardless of inspector toggle.
            artworkManager.LoadFromAPIButton();
        }
        else
        {
            Debug.LogWarning("StartPanelUiManager: ArtworkManagerNew reference is not set.");
        }

        if (startBtn != null)
        {
            startBtn.SetActive(false);
        }
    }
}
