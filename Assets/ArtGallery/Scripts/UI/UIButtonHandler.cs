using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// Efficient button event handler that manages multiple button click events
/// in a centralized way. Supports both direct method calls and UnityEvents.
/// </summary>
public class UIButtonHandler : MonoBehaviour
{
    [System.Serializable]
    public class ButtonAction
    {
        public string actionName;
        public Button button;
        public ButtonActionType actionType;
        
        [Header("Panel Reference (Only needed for panel actions)")]
        [Tooltip("Option 1: Reference panel directly (recommended for panel actions)")]
        public UIPanel panelReference;
        
        [Tooltip("Option 2: Use panel name (must match GameObject name, only for panel actions)")]
        public string panelName; // For panel actions - must match the panel GameObject's name
        
        [Header("Secondary Elements")]
        [Tooltip("Additional GameObjects to toggle on/off (works with any action type)")]
        public List<GameObject> secondaryElements = new List<GameObject>();
        
        [Header("Custom Actions")]
        [Tooltip("UnityEvent that fires when button is clicked (works with any action type)")]
        public UnityEngine.Events.UnityEvent onButtonClick;
    }
    
    public enum ButtonActionType
    {
        OpenPanel,
        ClosePanel,
        TogglePanel,
        CloseAllPanels,
        CloseCurrentPanel,
        EnableFirstPersonController,
        DisableFirstPersonController,
        ToggleFirstPersonController,
        CustomEvent
    }
    
    [Header("Button Actions")]
    [SerializeField] private List<ButtonAction> buttonActions = new List<ButtonAction>();
    
    [Header("Settings")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool logButtonClicks = false;
    
    // Events
    public Action<string> OnButtonClicked; // Action name, for external listeners
    
    private UIPanelManager panelManager;
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupButtons();
        }
        
        // Get or find panel manager
        panelManager = UIPanelManager.Instance;
    }
    
    /// <summary>
    /// Sets up all button click listeners.
    /// </summary>
    public void SetupButtons()
    {
        foreach (var action in buttonActions)
        {
            if (action.button == null)
            {
                Debug.LogWarning($"UIButtonHandler: Button is null for action '{action.actionName}'");
                continue;
            }
            
            // Remove existing listeners to avoid duplicates
            action.button.onClick.RemoveAllListeners();
            
            // Add listener based on action type
            if (action.actionType == ButtonActionType.CustomEvent)
            {
                action.button.onClick.AddListener(() => HandleCustomEvent(action));
            }
            else
            {
                SetupButtonAction(action);
            }
        }
    }
    
    /// <summary>
    /// Adds a button action programmatically using panel reference.
    /// </summary>
    public void AddButtonAction(Button button, ButtonActionType actionType, UIPanel panel, string actionName = "")
    {
        if (button == null) return;
        
        ButtonAction newAction = new ButtonAction
        {
            button = button,
            actionType = actionType,
            panelReference = panel,
            panelName = panel != null ? panel.gameObject.name : "",
            actionName = string.IsNullOrEmpty(actionName) ? button.name : actionName
        };
        
        buttonActions.Add(newAction);
        SetupButtonAction(newAction);
    }
    
    /// <summary>
    /// Adds a button action programmatically using panel name.
    /// </summary>
    public void AddButtonAction(Button button, ButtonActionType actionType, string panelName, string actionName = "")
    {
        if (button == null) return;
        
        ButtonAction newAction = new ButtonAction
        {
            button = button,
            actionType = actionType,
            panelName = panelName,
            actionName = string.IsNullOrEmpty(actionName) ? button.name : actionName
        };
        
        buttonActions.Add(newAction);
        SetupButtonAction(newAction);
    }
    
    private void SetupButtonAction(ButtonAction action)
    {
        // Setup the button immediately
        switch (action.actionType)
        {
            case ButtonActionType.OpenPanel:
                action.button.onClick.AddListener(() => HandleOpenPanel(action));
                break;
            case ButtonActionType.ClosePanel:
                action.button.onClick.AddListener(() => HandleClosePanel(action));
                break;
            case ButtonActionType.TogglePanel:
                action.button.onClick.AddListener(() => HandleTogglePanel(action));
                break;
            case ButtonActionType.CloseAllPanels:
                action.button.onClick.AddListener(() => HandleCloseAllPanels(action));
                break;
            case ButtonActionType.CloseCurrentPanel:
                action.button.onClick.AddListener(() => HandleCloseCurrentPanel(action));
                break;
            case ButtonActionType.EnableFirstPersonController:
                action.button.onClick.AddListener(() => HandleEnableFirstPersonController(action));
                break;
            case ButtonActionType.DisableFirstPersonController:
                action.button.onClick.AddListener(() => HandleDisableFirstPersonController(action));
                break;
            case ButtonActionType.ToggleFirstPersonController:
                action.button.onClick.AddListener(() => HandleToggleFirstPersonController(action));
                break;
        }
    }
    
    /// <summary>
    /// Removes a button action.
    /// </summary>
    public void RemoveButtonAction(Button button)
    {
        for (int i = buttonActions.Count - 1; i >= 0; i--)
        {
            if (buttonActions[i].button == button)
            {
                buttonActions[i].button.onClick.RemoveAllListeners();
                buttonActions.RemoveAt(i);
            }
        }
    }
    
    private void HandleOpenPanel(ButtonAction action)
    {
        // Try panel reference first, then fall back to panel name
        if (action.panelReference != null)
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Opening panel '{action.panelReference.gameObject.name}'");
            
            if (panelManager != null)
            {
                panelManager.OpenPanel(action.panelReference);
            }
            else
            {
                action.panelReference.Open();
            }
            
            // Toggle secondary elements based on their own state
            ToggleSecondaryElements(action);
        }
        else if (panelManager != null && !string.IsNullOrEmpty(action.panelName))
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Opening panel '{action.panelName}'");
            
            panelManager.OpenPanel(action.panelName);
            
            // Toggle secondary elements based on their own state
            ToggleSecondaryElements(action);
        }
        else
        {
            Debug.LogWarning($"UIButtonHandler: Cannot open panel - no panel reference or panel name specified for action '{action.actionName}'");
        }
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleClosePanel(ButtonAction action)
    {
        // Try panel reference first, then fall back to panel name
        if (action.panelReference != null)
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Closing panel '{action.panelReference.gameObject.name}'");
            
            if (panelManager != null)
            {
                panelManager.ClosePanel(action.panelReference);
            }
            else
            {
                action.panelReference.Close();
            }
            
            // Toggle secondary elements based on their own state
            ToggleSecondaryElements(action);
        }
        else if (panelManager != null && !string.IsNullOrEmpty(action.panelName))
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Closing panel '{action.panelName}'");
            
            panelManager.ClosePanel(action.panelName);
            
            // Toggle secondary elements based on their own state
            ToggleSecondaryElements(action);
        }
        else
        {
            Debug.LogWarning($"UIButtonHandler: Cannot close panel - no panel reference or panel name specified for action '{action.actionName}'");
        }
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleTogglePanel(ButtonAction action)
    {
        UIPanel targetPanel = null;
        bool wasOpen = false;
        
        // Get the target panel and check its current state
        if (action.panelReference != null)
        {
            targetPanel = action.panelReference;
            wasOpen = targetPanel.IsOpen;
        }
        else if (panelManager != null && !string.IsNullOrEmpty(action.panelName))
        {
            // Use reflection or a public method to get the panel
            // For now, we'll find it by name
            targetPanel = FindPanelByName(action.panelName);
            if (targetPanel != null)
            {
                wasOpen = targetPanel.IsOpen;
            }
        }
        
        // Toggle the panel
        if (action.panelReference != null)
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Toggling panel '{action.panelReference.gameObject.name}'");
            
            if (panelManager != null)
            {
                panelManager.TogglePanel(action.panelReference);
            }
            else
            {
                action.panelReference.Toggle();
            }
        }
        else if (panelManager != null && !string.IsNullOrEmpty(action.panelName))
        {
            if (logButtonClicks)
                Debug.Log($"UIButtonHandler: Toggling panel '{action.panelName}'");
            
            panelManager.TogglePanel(action.panelName);
        }
        else
        {
            Debug.LogWarning($"UIButtonHandler: Cannot toggle panel - no panel reference or panel name specified for action '{action.actionName}'");
        }
        
        // Toggle secondary elements based on their own state
        ToggleSecondaryElements(action);
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    /// <summary>
    /// Helper method to find a panel by name.
    /// </summary>
    private UIPanel FindPanelByName(string panelName)
    {
        if (panelManager == null) return null;
        
        // Try to find the panel using UIPanelManager's public methods
        UIPanel[] allPanels = FindObjectsOfType<UIPanel>();
        foreach (var panel in allPanels)
        {
            if (panel.gameObject.name == panelName)
            {
                return panel;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Toggles secondary elements based on their own current state.
    /// </summary>
    private void ToggleSecondaryElements(ButtonAction action)
    {
        if (action.secondaryElements == null || action.secondaryElements.Count == 0)
            return;
        
        foreach (var element in action.secondaryElements)
        {
            if (element != null)
            {
                // Toggle based on element's own current state
                bool newState = !element.activeSelf;
                element.SetActive(newState);
                
                if (logButtonClicks)
                    Debug.Log($"UIButtonHandler: {(newState ? "Enabled" : "Disabled")} secondary element '{element.name}' (toggled from {!newState})");
            }
        }
    }
    
    private void HandleCloseAllPanels(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log("UIButtonHandler: Closing all panels");
        
        if (panelManager != null)
        {
            panelManager.CloseAllPanels();
        }
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleCloseCurrentPanel(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log("UIButtonHandler: Closing current panel");
        
        if (panelManager != null)
        {
            panelManager.CloseCurrentPanel();
        }
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleEnableFirstPersonController(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log($"UIButtonHandler: Enabling FirstPersonController");
        
        UIManager.Instance?.EnableFirstPersonController();
        
        // Toggle secondary elements if any
        ToggleSecondaryElements(action);
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleDisableFirstPersonController(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log($"UIButtonHandler: Disabling FirstPersonController");
        
        UIManager.Instance?.DisableFirstPersonController();
        
        // Toggle secondary elements if any
        ToggleSecondaryElements(action);
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleToggleFirstPersonController(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log($"UIButtonHandler: Toggling FirstPersonController");
        
        UIManager.Instance?.ToggleFirstPersonController();
        
        // Toggle secondary elements if any
        ToggleSecondaryElements(action);
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void HandleCustomEvent(ButtonAction action)
    {
        if (logButtonClicks)
            Debug.Log($"UIButtonHandler: Custom event '{action.actionName}'");
        
        // Toggle secondary elements if any
        ToggleSecondaryElements(action);
        
        OnButtonClicked?.Invoke(action.actionName);
        action.onButtonClick?.Invoke();
    }
    
    private void OnDestroy()
    {
        // Clean up button listeners
        foreach (var action in buttonActions)
        {
            if (action.button != null)
            {
                action.button.onClick.RemoveAllListeners();
            }
        }
    }
}
