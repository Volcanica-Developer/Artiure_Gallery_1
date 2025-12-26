using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI panel that displays artwork information when clicked.
/// Handles smooth open/close animations.
/// </summary>
public class ArtworkUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Image artworkImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI yearText;
    public TextMeshProUGUI mediumText;
    public Button closeButton;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Coroutine animationCoroutine;
    private bool isOpen = false;
    
    private void Awake()
    {
        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            
            panelRect = panel.GetComponent<RectTransform>();
        }
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        
        // Start with panel closed
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Opens the panel and displays artwork information.
    /// </summary>
    public void ShowArtwork(ArtworkData artwork)
    {
        if (artwork == null)
        {
            Debug.LogWarning("ArtworkUI: Cannot show null artwork.");
            return;
        }
        
        if (panel == null)
        {
            Debug.LogError("ArtworkUI: Panel GameObject is not assigned! Cannot show artwork.");
            return;
        }
        
        Debug.Log($"ArtworkUI: Showing artwork '{artwork.title}'");
        
        // Update UI elements
        if (titleText != null)
        {
            titleText.text = artwork.title;
        }
        
        if (artistText != null)
        {
            artistText.text = artwork.artist;
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = artwork.description;
        }
        
        if (yearText != null)
        {
            yearText.text = artwork.year > 0 ? artwork.year.ToString() : "";
        }
        
        if (mediumText != null)
        {
            mediumText.text = artwork.medium;
        }
        
        // Set artwork image
        if (artworkImage != null)
        {
            if (artwork.image != null)
            {
                artworkImage.sprite = Sprite.Create(
                    artwork.image,
                    new Rect(0, 0, artwork.image.width, artwork.image.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            else if (artwork.sprite != null)
            {
                artworkImage.sprite = artwork.sprite;
            }
        }
        
        // Open panel with animation
        OpenPanel();
    }
    
    private void OpenPanel()
    {
        if (panel == null)
        {
            Debug.LogError("ArtworkUI: Cannot open panel - panel GameObject is null!");
            return;
        }
        
        Debug.Log("ArtworkUI: Opening panel...");
        
        // Stop any ongoing animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        panel.SetActive(true);
        isOpen = true;
        
        // Play open sound
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        
        // Start animation
        animationCoroutine = StartCoroutine(AnimatePanel(true));
    }
    
    public void ClosePanel()
    {
        if (panel == null || !isOpen) return;
        
        // Stop any ongoing animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        isOpen = false;
        
        // Play close sound
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        
        // Start close animation
        animationCoroutine = StartCoroutine(AnimatePanel(false));
    }
    
    private IEnumerator AnimatePanel(bool opening)
    {
        float elapsed = 0f;
        AnimationCurve curve = opening ? openCurve : closeCurve;
        
        Vector3 startScale = opening ? Vector3.zero : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : Vector3.zero;
        
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = curve.Evaluate(t);
            
            // Scale animation
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            }
            
            // Fade animation
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (panelRect != null)
        {
            panelRect.localScale = endScale;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = endAlpha;
        }
        
        // Deactivate panel if closing
        if (!opening)
        {
            panel.SetActive(false);
        }
        
        animationCoroutine = null;
    }
    
    private void Update()
    {
        // Close on Escape key
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }
}

