using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles animations for buttons and elements inside buttons.
/// Supports hover, click, active, inactive, and custom animations using DOTween.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAnimator : MonoBehaviour
{
    [System.Serializable]
    public class ElementAnimation
    {
        [Header("Target Element")]
        public RectTransform targetElement;
        public string elementName; // For finding by name if target is null
        
        [Header("Animation Settings")]
        public bool animateOnHover = false;
        public bool animateOnClick = false;
        public bool animateOnActive = false;
        public bool animateOnInactive = false;
        
        [Header("Hover Animation")]
        public Vector3 hoverScale = Vector3.one * 1.1f;
        public Vector3 hoverRotation = Vector3.zero;
        public Vector2 hoverPosition = Vector2.zero;
        public float hoverDuration = 0.2f;
        public Ease hoverEase = Ease.OutQuad;
        
        [Header("Click Animation")]
        public Vector3 clickScale = Vector3.one * 0.95f;
        public float clickDuration = 0.1f;
        public Ease clickEase = Ease.OutQuad;
        
        [Header("Active Animation")]
        public Vector3 activeScale = Vector3.one;
        public Vector3 activeRotation = Vector3.zero;
        public Vector2 activePosition = Vector2.zero;
        public float activeDuration = 0.3f;
        public Ease activeEase = Ease.OutQuad;
        
        [Header("Inactive Animation")]
        public Vector3 inactiveScale = Vector3.one;
        public Vector3 inactiveRotation = Vector3.zero;
        public Vector2 inactivePosition = Vector2.zero;
        public float inactiveDuration = 0.3f;
        public Ease inactiveEase = Ease.OutQuad;
        
        // Store original values
        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public Vector3 originalRotation;
        
        // Current tweens
        [HideInInspector] public Tween currentTween;
    }
    
    [Header("Button Animation Settings")]
    [SerializeField] private bool animateButton = true;
    [SerializeField] private bool animateOnHover = true;
    [SerializeField] private bool animateOnClick = true;
    
    [Header("Button Hover Animation")]
    [SerializeField] private Vector3 hoverScale = Vector3.one * 1.05f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private Ease hoverEase = Ease.OutQuad;
    
    [Header("Button Click Animation")]
    [SerializeField] private Vector3 clickScale = Vector3.one * 0.95f;
    [SerializeField] private float clickDuration = 0.1f;
    [SerializeField] private Ease clickEase = Ease.OutQuad;
    
    [Header("Element Animations")]
    [SerializeField] private List<ElementAnimation> elementAnimations = new List<ElementAnimation>();
    
    [Header("Active/Inactive States")]
    [SerializeField] private bool useActiveState = false;
    [SerializeField] private bool startActive = false;
    
    [Header("Events")]
    public Action OnAnimationStart;
    public Action OnAnimationComplete;
    public Action OnHoverEnter;
    public Action OnHoverExit;
    public Action OnClickAnimation;
    
    private Button button;
    private RectTransform buttonRect;
    private Vector3 originalButtonScale;
    private bool isHovered = false;
    private bool isActive = false;
    private Tween buttonHoverTween;
    private Tween buttonClickTween;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        buttonRect = GetComponent<RectTransform>();
        originalButtonScale = buttonRect.localScale;
        
        // Initialize active state
        isActive = startActive;
        
        // Setup button events
        SetupButtonEvents();
        
        // Initialize element animations
        InitializeElementAnimations();
        
        // Apply initial active/inactive state
        if (useActiveState)
        {
            if (isActive)
                SetActiveState();
            else
                SetInactiveState();
        }
    }
    
    private void SetupButtonEvents()
    {
        // Create event triggers for hover
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }
        
        // Pointer Enter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnPointerEnter(); });
        trigger.triggers.Add(entryEnter);
        
        // Pointer Exit
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnPointerExit(); });
        trigger.triggers.Add(entryExit);
        
        // Click
        button.onClick.AddListener(OnButtonClick);
    }
    
    private void InitializeElementAnimations()
    {
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.targetElement == null && !string.IsNullOrEmpty(elementAnim.elementName))
            {
                // Try to find element by name
                Transform found = transform.Find(elementAnim.elementName);
                if (found != null)
                {
                    elementAnim.targetElement = found.GetComponent<RectTransform>();
                }
            }
            
            if (elementAnim.targetElement != null)
            {
                // Store original values
                elementAnim.originalScale = elementAnim.targetElement.localScale;
                elementAnim.originalPosition = elementAnim.targetElement.anchoredPosition;
                elementAnim.originalRotation = elementAnim.targetElement.localEulerAngles;
            }
        }
    }
    
    private void OnPointerEnter()
    {
        if (!button.interactable) return;
        
        isHovered = true;
        OnHoverEnter?.Invoke();
        
        if (animateButton && animateOnHover)
        {
            AnimateButtonHover();
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.animateOnHover && elementAnim.targetElement != null)
            {
                AnimateElementHover(elementAnim);
            }
        }
    }
    
    private void OnPointerExit()
    {
        if (!button.interactable) return;
        
        isHovered = false;
        OnHoverExit?.Invoke();
        
        if (animateButton && animateOnHover)
        {
            AnimateButtonHoverExit();
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.animateOnHover && elementAnim.targetElement != null)
            {
                AnimateElementHoverExit(elementAnim);
            }
        }
    }
    
    private void OnButtonClick()
    {
        if (!button.interactable) return;
        
        OnClickAnimation?.Invoke();
        OnAnimationStart?.Invoke();
        
        if (animateButton && animateOnClick)
        {
            AnimateButtonClick();
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.animateOnClick && elementAnim.targetElement != null)
            {
                AnimateElementClick(elementAnim);
            }
        }
    }
    
    private void AnimateButtonHover()
    {
        if (buttonHoverTween != null && buttonHoverTween.IsActive())
        {
            buttonHoverTween.Kill();
        }
        
        buttonHoverTween = buttonRect.DOScale(hoverScale, hoverDuration)
            .SetEase(hoverEase);
    }
    
    private void AnimateButtonHoverExit()
    {
        if (buttonHoverTween != null && buttonHoverTween.IsActive())
        {
            buttonHoverTween.Kill();
        }
        
        buttonHoverTween = buttonRect.DOScale(originalButtonScale, hoverDuration)
            .SetEase(hoverEase);
    }
    
    private void AnimateButtonClick()
    {
        if (buttonClickTween != null && buttonClickTween.IsActive())
        {
            buttonClickTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        sequence.Append(buttonRect.DOScale(clickScale, clickDuration * 0.5f).SetEase(clickEase));
        sequence.Append(buttonRect.DOScale(originalButtonScale, clickDuration * 0.5f).SetEase(clickEase));
        sequence.OnComplete(() => OnAnimationComplete?.Invoke());
        
        buttonClickTween = sequence;
    }
    
    private void AnimateElementHover(ElementAnimation elementAnim)
    {
        if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
        {
            elementAnim.currentTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        
        if (elementAnim.hoverScale != Vector3.one)
        {
            sequence.Join(elementAnim.targetElement.DOScale(elementAnim.hoverScale, elementAnim.hoverDuration));
        }
        
        if (elementAnim.hoverRotation != Vector3.zero)
        {
            sequence.Join(elementAnim.targetElement.DORotate(elementAnim.hoverRotation, elementAnim.hoverDuration));
        }
        
        if (elementAnim.hoverPosition != Vector2.zero)
        {
            sequence.Join(elementAnim.targetElement.DOAnchorPos(
                elementAnim.originalPosition + elementAnim.hoverPosition,
                elementAnim.hoverDuration));
        }
        
        sequence.SetEase(elementAnim.hoverEase);
        elementAnim.currentTween = sequence;
    }
    
    private void AnimateElementHoverExit(ElementAnimation elementAnim)
    {
        if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
        {
            elementAnim.currentTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        
        sequence.Join(elementAnim.targetElement.DOScale(elementAnim.originalScale, elementAnim.hoverDuration));
        sequence.Join(elementAnim.targetElement.DORotate(elementAnim.originalRotation, elementAnim.hoverDuration));
        sequence.Join(elementAnim.targetElement.DOAnchorPos(elementAnim.originalPosition, elementAnim.hoverDuration));
        sequence.SetEase(elementAnim.hoverEase);
        
        elementAnim.currentTween = sequence;
    }
    
    private void AnimateElementClick(ElementAnimation elementAnim)
    {
        if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
        {
            elementAnim.currentTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        sequence.Append(elementAnim.targetElement.DOScale(elementAnim.clickScale, elementAnim.clickDuration * 0.5f));
        sequence.Append(elementAnim.targetElement.DOScale(elementAnim.originalScale, elementAnim.clickDuration * 0.5f));
        sequence.SetEase(elementAnim.clickEase);
        
        elementAnim.currentTween = sequence;
    }
    
    /// <summary>
    /// Sets the button to active state and animates if configured.
    /// </summary>
    public void SetActiveState()
    {
        if (!useActiveState) return;
        
        isActive = true;
        
        if (animateButton)
        {
            // Button active animation could be added here
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.animateOnActive && elementAnim.targetElement != null)
            {
                AnimateElementActive(elementAnim);
            }
        }
    }
    
    /// <summary>
    /// Sets the button to inactive state and animates if configured.
    /// </summary>
    public void SetInactiveState()
    {
        if (!useActiveState) return;
        
        isActive = false;
        
        if (animateButton)
        {
            // Button inactive animation could be added here
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.animateOnInactive && elementAnim.targetElement != null)
            {
                AnimateElementInactive(elementAnim);
            }
        }
    }
    
    /// <summary>
    /// Toggles the active state.
    /// </summary>
    public void ToggleActiveState()
    {
        if (isActive)
            SetInactiveState();
        else
            SetActiveState();
    }
    
    private void AnimateElementActive(ElementAnimation elementAnim)
    {
        if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
        {
            elementAnim.currentTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        
        if (elementAnim.activeScale != Vector3.one)
        {
            sequence.Join(elementAnim.targetElement.DOScale(elementAnim.activeScale, elementAnim.activeDuration));
        }
        
        if (elementAnim.activeRotation != Vector3.zero)
        {
            sequence.Join(elementAnim.targetElement.DORotate(elementAnim.activeRotation, elementAnim.activeDuration));
        }
        
        if (elementAnim.activePosition != Vector2.zero)
        {
            sequence.Join(elementAnim.targetElement.DOAnchorPos(
                elementAnim.originalPosition + elementAnim.activePosition,
                elementAnim.activeDuration));
        }
        
        sequence.SetEase(elementAnim.activeEase);
        elementAnim.currentTween = sequence;
    }
    
    private void AnimateElementInactive(ElementAnimation elementAnim)
    {
        if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
        {
            elementAnim.currentTween.Kill();
        }
        
        Sequence sequence = DOTween.Sequence();
        
        sequence.Join(elementAnim.targetElement.DOScale(elementAnim.inactiveScale, elementAnim.inactiveDuration));
        sequence.Join(elementAnim.targetElement.DORotate(elementAnim.inactiveRotation, elementAnim.inactiveDuration));
        sequence.Join(elementAnim.targetElement.DOAnchorPos(
            elementAnim.originalPosition + elementAnim.inactivePosition,
            elementAnim.inactiveDuration));
        
        sequence.SetEase(elementAnim.inactiveEase);
        elementAnim.currentTween = sequence;
    }
    
    /// <summary>
    /// Adds a custom animation to an element.
    /// </summary>
    public void AddCustomElementAnimation(RectTransform element, Vector3 targetScale, Vector3 targetRotation, 
        Vector2 targetPosition, float duration, Ease ease, Action onComplete = null)
    {
        if (element == null) return;
        
        Sequence sequence = DOTween.Sequence();
        sequence.Join(element.DOScale(targetScale, duration));
        sequence.Join(element.DORotate(targetRotation, duration));
        sequence.Join(element.DOAnchorPos(targetPosition, duration));
        sequence.SetEase(ease);
        
        if (onComplete != null)
        {
            sequence.OnComplete(() => onComplete());
        }
    }
    
    private void OnDestroy()
    {
        // Kill all active tweens
        if (buttonHoverTween != null && buttonHoverTween.IsActive())
        {
            buttonHoverTween.Kill();
        }
        
        if (buttonClickTween != null && buttonClickTween.IsActive())
        {
            buttonClickTween.Kill();
        }
        
        foreach (var elementAnim in elementAnimations)
        {
            if (elementAnim.currentTween != null && elementAnim.currentTween.IsActive())
            {
                elementAnim.currentTween.Kill();
            }
        }
    }
}
