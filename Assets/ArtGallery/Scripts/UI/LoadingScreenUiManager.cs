using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class LoadingScreenUiManager : MonoBehaviour
{
    [Header("Loader Image Reference")]
    [SerializeField] private Image loaderImage; // Image Type should be set to Filled in the Inspector.

    [Header("References")]
    [SerializeField] private ArtworkManagerNew artworkManager;

    [Tooltip("Optional panel GameObject to hide when loading is complete. If null, this GameObject will be used.")]
    [SerializeField] private GameObject loadingPanel;

    [Tooltip("FirstPersonController to enable when loading is complete.")]
    [SerializeField] private FirstPersonController firstPersonController;

    [Tooltip("This panel will popup when the loading is complete")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Events")]
    [Tooltip("Invoked when all images have finished downloading and the loading UI is complete.")]
    public UnityEvent onLoadingFinished;

    private void Awake()
    {
        if (loadingPanel == null)
        {
            loadingPanel = gameObject;
        }
    }

    private void OnEnable()
    {
        if (artworkManager != null)
        {
            artworkManager.OnImageDownloadStarted += HandleDownloadStarted;
            artworkManager.OnImageDownloadProgress += HandleDownloadProgress;
            artworkManager.OnAllImagesDownloaded += HandleAllImagesDownloaded;
        }

        // Initialize UI state
        SetFillAmount(artworkManager != null ? artworkManager.DownloadProgress : 0f);
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (artworkManager != null)
        {
            artworkManager.OnImageDownloadStarted -= HandleDownloadStarted;
            artworkManager.OnImageDownloadProgress -= HandleDownloadProgress;
            artworkManager.OnAllImagesDownloaded -= HandleAllImagesDownloaded;
        }
    }

    private void HandleDownloadStarted(int total)
    {
        if (total <= 0)
        {
            SetFillAmount(0f);
            return;
        }

        SetFillAmount(0f);
        if (loadingPanel != null && !loadingPanel.activeSelf)
        {
            loadingPanel.SetActive(true);
        }
    }

    private void HandleDownloadProgress(int completed, int total)
    {
        if (total <= 0)
        {
            SetFillAmount(0f);
            return;
        }

        float progress = Mathf.Clamp01((float)completed / total);
        SmoothFillTo(progress);

        if (completed >= total)
        {
            HandleAllImagesDownloaded();
        }
    }

    private void HandleAllImagesDownloaded()
    {
        SmoothFillTo(1f, 0.25f, () =>
        {
            if (loadingPanel != null)
            {
                instructionPanel.SetActive(true);
                menuPanel.SetActive(true);
                loadingPanel.SetActive(false);
            }

            // Enable FirstPersonController when loading is complete
            if (firstPersonController != null)
            {
                firstPersonController.enabled = true;
            }

            // Notify listeners that loading has finished.
            onLoadingFinished?.Invoke();
        });
    }

    private void SetFillAmount(float value)
    {
        if (loaderImage == null)
            return;

        loaderImage.fillAmount = Mathf.Clamp01(value);
    }

    private void SmoothFillTo(float targetValue, float duration = 0.25f, System.Action onComplete = null)
    {
        if (loaderImage == null)
            return;

        float clamped = Mathf.Clamp01(targetValue);
        loaderImage.DOKill();
        loaderImage.DOFillAmount(clamped, duration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }
}
