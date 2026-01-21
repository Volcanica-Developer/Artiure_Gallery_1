using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;

/// <summary>
/// Individual UI panel component that handles open/close animations and state.
/// Can be used standalone or managed by UIPanelManager.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private bool startClosed = true;
    [SerializeField] private bool closeOnBackgroundClick = false;
    
    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationType animationType = AnimationType.Fade;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    [Header("Events")]
    public Action OnPanelOpened;
    public Action OnPanelClosed;
    public Action OnPanelOpening;
    public Action OnPanelClosing;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Tween currentTween;
    private bool isOpen = false;
    private bool isAnimating = false;
    
    // Store original values for animation
    private Vector3 originalScale;
    private Vector2 originalPosition;
    
    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        SlideDown,
        SlideUp,
        SlideLeft,
        SlideRight,
        FadeAndScale,
        FadeAndSlideDown
    }
    
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Store original values
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        
        // Setup initial state
        if (startClosed)
        {
            SetClosedState();
        }
        else
        {
            SetOpenState();
        }
        
        // Setup background click handler if needed
        if (closeOnBackgroundClick)
        {
            SetupBackgroundClickHandler();
        }
    }
    
    /// <summary>
    /// Opens the panel with animation (if enabled).
    /// </summary>
    public void Open()
    {
        if (isOpen && !isAnimating) return;
        
        OnPanelOpening?.Invoke();
        
        gameObject.SetActive(true);
        isOpen = true;
        
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        
        if (useAnimation && animationType != AnimationType.None)
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
            AnimateOpenDOTween();
        }
        else
        {
            SetOpenState();
            OnPanelOpened?.Invoke();
        }
    }
    
    /// <summary>
    /// Closes the panel with animation (if enabled).
    /// </summary>
    public void Close()
    {
        if (!isOpen && !isAnimating) return;
        
        OnPanelClosing?.Invoke();
        
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        
        if (useAnimation && animationType != AnimationType.None)
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
            AnimateCloseDOTween();
        }
        else
        {
            SetClosedState();
            OnPanelClosed?.Invoke();
        }
    }
    
    /// <summary>
    /// Toggles the panel open/closed state.
    /// </summary>
    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }
    
    /// <summary>
    /// Immediately sets the panel to open state without animation.
    /// </summary>
    public void SetOpenImmediate()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        gameObject.SetActive(true);
        SetOpenState();
        isOpen = true;
        isAnimating = false;
    }
    
    /// <summary>
    /// Immediately sets the panel to closed state without animation.
    /// </summary>
    public void SetClosedImmediate()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        SetClosedState();
        isOpen = false;
        isAnimating = false;
        gameObject.SetActive(false);
    }
    
    private void SetOpenState()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
    }
    
    private void SetClosedState()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Set initial closed state based on animation type
        switch (animationType)
        {
            case AnimationType.Scale:
            case AnimationType.FadeAndScale:
                rectTransform.localScale = Vector3.zero;
                break;
            case AnimationType.SlideDown:
            case AnimationType.FadeAndSlideDown:
                rectTransform.anchoredPosition = originalPosition + Vector2.down * rectTransform.rect.height;
                break;
            case AnimationType.SlideUp:
                rectTransform.anchoredPosition = originalPosition + Vector2.up * rectTransform.rect.height;
                break;
            case AnimationType.SlideLeft:
                rectTransform.anchoredPosition = originalPosition + Vector2.left * rectTransform.rect.width;
                break;
            case AnimationType.SlideRight:
                rectTransform.anchoredPosition = originalPosition + Vector2.right * rectTransform.rect.width;
                break;
        }
    }
    
    private void AnimateOpenDOTween()
    {
        isAnimating = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        // Set initial closed state
        switch (animationType)
        {
            case AnimationType.Fade:
                canvasGroup.alpha = 0f;
                break;
            case AnimationType.Scale:
                rectTransform.localScale = Vector3.zero;
                canvasGroup.alpha = 1f;
                break;
            case AnimationType.SlideDown:
                rectTransform.anchoredPosition = originalPosition + Vector2.down * rectTransform.rect.height;
                canvasGroup.alpha = 1f;
                break;
            case AnimationType.SlideUp:
                rectTransform.anchoredPosition = originalPosition + Vector2.up * rectTransform.rect.height;
                canvasGroup.alpha = 1f;
                break;
            case AnimationType.SlideLeft:
                rectTransform.anchoredPosition = originalPosition + Vector2.left * rectTransform.rect.width;
                canvasGroup.alpha = 1f;
                break;
            case AnimationType.SlideRight:
                rectTransform.anchoredPosition = originalPosition + Vector2.right * rectTransform.rect.width;
                canvasGroup.alpha = 1f;
                break;
            case AnimationType.FadeAndScale:
                canvasGroup.alpha = 0f;
                rectTransform.localScale = Vector3.zero;
                break;
            case AnimationType.FadeAndSlideDown:
                canvasGroup.alpha = 0f;
                rectTransform.anchoredPosition = originalPosition + Vector2.down * rectTransform.rect.height;
                break;
        }
        
        Sequence sequence = DOTween.Sequence();
        
        switch (animationType)
        {
            case AnimationType.Fade:
                currentTween = canvasGroup.DOFade(1f, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.Scale:
                currentTween = rectTransform.DOScale(originalScale, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.SlideDown:
                currentTween = rectTransform.DOAnchorPos(originalPosition, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.SlideUp:
                currentTween = rectTransform.DOAnchorPos(originalPosition, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.SlideLeft:
                currentTween = rectTransform.DOAnchorPos(originalPosition, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.SlideRight:
                currentTween = rectTransform.DOAnchorPos(originalPosition, animationDuration)
                    .SetEase(openCurve)
                    .OnComplete(() => {
                        SetOpenState();
                        isAnimating = false;
                        OnPanelOpened?.Invoke();
                    });
                break;
                
            case AnimationType.FadeAndScale:
                sequence.Append(canvasGroup.DOFade(1f, animationDuration));
                sequence.Join(rectTransform.DOScale(originalScale, animationDuration));
                sequence.SetEase(openCurve);
                sequence.OnComplete(() => {
                    SetOpenState();
                    isAnimating = false;
                    OnPanelOpened?.Invoke();
                });
                currentTween = sequence;
                break;
                
            case AnimationType.FadeAndSlideDown:
                sequence.Append(canvasGroup.DOFade(1f, animationDuration));
                sequence.Join(rectTransform.DOAnchorPos(originalPosition, animationDuration));
                sequence.SetEase(openCurve);
                sequence.OnComplete(() => {
                    SetOpenState();
                    isAnimating = false;
                    OnPanelOpened?.Invoke();
                });
                currentTween = sequence;
                break;
        }
    }
    
    private void AnimateCloseDOTween()
    {
        isAnimating = true;
        
        Vector3 startScale = rectTransform.localScale;
        Vector2 startPosition = rectTransform.anchoredPosition;
        float startAlpha = canvasGroup.alpha;
        
        Sequence sequence = DOTween.Sequence();
        
        switch (animationType)
        {
            case AnimationType.Fade:
                currentTween = canvasGroup.DOFade(0f, animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.Scale:
                currentTween = rectTransform.DOScale(Vector3.zero, animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.SlideDown:
                currentTween = rectTransform.DOAnchorPos(
                    originalPosition + Vector2.down * rectTransform.rect.height,
                    animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.SlideUp:
                currentTween = rectTransform.DOAnchorPos(
                    originalPosition + Vector2.up * rectTransform.rect.height,
                    animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.SlideLeft:
                currentTween = rectTransform.DOAnchorPos(
                    originalPosition + Vector2.left * rectTransform.rect.width,
                    animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.SlideRight:
                currentTween = rectTransform.DOAnchorPos(
                    originalPosition + Vector2.right * rectTransform.rect.width,
                    animationDuration)
                    .SetEase(closeCurve)
                    .OnComplete(() => {
                        SetClosedState();
                        isAnimating = false;
                        isOpen = false;
                        OnPanelClosed?.Invoke();
                        gameObject.SetActive(false);
                    });
                break;
                
            case AnimationType.FadeAndScale:
                sequence.Append(canvasGroup.DOFade(0f, animationDuration));
                sequence.Join(rectTransform.DOScale(Vector3.zero, animationDuration));
                sequence.SetEase(closeCurve);
                sequence.OnComplete(() => {
                    SetClosedState();
                    isAnimating = false;
                    isOpen = false;
                    OnPanelClosed?.Invoke();
                    gameObject.SetActive(false);
                });
                currentTween = sequence;
                break;
                
            case AnimationType.FadeAndSlideDown:
                sequence.Append(canvasGroup.DOFade(0f, animationDuration));
                sequence.Join(rectTransform.DOAnchorPos(
                    originalPosition + Vector2.down * rectTransform.rect.height,
                    animationDuration));
                sequence.SetEase(closeCurve);
                sequence.OnComplete(() => {
                    SetClosedState();
                    isAnimating = false;
                    isOpen = false;
                    OnPanelClosed?.Invoke();
                    gameObject.SetActive(false);
                });
                currentTween = sequence;
                break;
        }
    }
    
    private void SetupBackgroundClickHandler()
    {
        // Create a full-screen background image that closes the panel when clicked
        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform.parent, false);
        background.transform.SetSiblingIndex(transform.GetSiblingIndex());
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        
        Button bgButton = background.AddComponent<Button>();
        bgButton.onClick.AddListener(Close);
        
        // Make sure background is behind the panel
        CanvasGroup bgCanvasGroup = background.GetComponent<CanvasGroup>();
        if (bgCanvasGroup == null)
        {
            bgCanvasGroup = background.AddComponent<CanvasGroup>();
        }
    }
    
    private void OnDestroy()
    {
        // Kill any active tweens to prevent errors
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}
