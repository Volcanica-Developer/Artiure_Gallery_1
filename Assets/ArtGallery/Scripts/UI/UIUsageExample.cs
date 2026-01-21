using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example script showing how to use the UI Panel System.
/// This demonstrates various ways to interact with panels and buttons.
/// </summary>
public class UIUsageExample : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private UIPanel mainMenuPanel;
    [SerializeField] private UIPanel settingsPanel;
    [SerializeField] private UIPanel inventoryPanel;
    
    [Header("Button References")]
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button toggleInventoryButton;
    
    private void Start()
    {
        // Example 1: Using UIPanelManager to open/close panels
        SetupPanelManagerExample();
        
        // Example 2: Direct panel control
        SetupDirectPanelExample();
        
        // Example 3: Using UIButtonHandler (if you have one in scene)
        SetupButtonHandlerExample();
    }
    
    /// <summary>
    /// Example: Using UIPanelManager to control panels
    /// </summary>
    private void SetupPanelManagerExample()
    {
        // Get the panel manager instance
        UIPanelManager panelManager = UIPanelManager.Instance;
        
        // Open a panel by name
        panelManager.OpenPanel("SettingsPanel");
        
        // Close a panel by name
        panelManager.ClosePanel("MainMenuPanel");
        
        // Toggle a panel
        panelManager.TogglePanel("InventoryPanel");
        
        // Check if a panel is open
        if (panelManager.IsPanelOpen("SettingsPanel"))
        {
            Debug.Log("Settings panel is open!");
        }
        
        // Close all panels
        panelManager.CloseAllPanels();
        
        // Subscribe to events
        panelManager.OnPanelOpened += (panel) => Debug.Log($"Panel {panel.name} opened!");
        panelManager.OnPanelClosed += (panel) => Debug.Log($"Panel {panel.name} closed!");
    }
    
    /// <summary>
    /// Example: Direct panel control without manager
    /// </summary>
    private void SetupDirectPanelExample()
    {
        if (mainMenuPanel != null)
        {
            // Open panel
            mainMenuPanel.Open();
            
            // Close panel
            mainMenuPanel.Close();
            
            // Toggle panel
            mainMenuPanel.Toggle();
            
            // Subscribe to panel events
            mainMenuPanel.OnPanelOpened += () => Debug.Log("Main menu opened!");
            mainMenuPanel.OnPanelClosed += () => Debug.Log("Main menu closed!");
            
            // Check panel state
            if (mainMenuPanel.IsOpen)
            {
                Debug.Log("Main menu is currently open");
            }
        }
    }
    
    /// <summary>
    /// Example: Using UIButtonHandler for button events
    /// </summary>
    private void SetupButtonHandlerExample()
    {
        // Find or get UIButtonHandler component
        UIButtonHandler buttonHandler = FindObjectOfType<UIButtonHandler>();
        
        if (buttonHandler != null)
        {
            // Add button actions programmatically
            if (openSettingsButton != null)
            {
                buttonHandler.AddButtonAction(
                    openSettingsButton,
                    UIButtonHandler.ButtonActionType.OpenPanel,
                    "SettingsPanel",
                    "OpenSettings"
                );
            }
            
            if (closeSettingsButton != null)
            {
                buttonHandler.AddButtonAction(
                    closeSettingsButton,
                    UIButtonHandler.ButtonActionType.ClosePanel,
                    "SettingsPanel",
                    "CloseSettings"
                );
            }
            
            if (toggleInventoryButton != null)
            {
                buttonHandler.AddButtonAction(
                    toggleInventoryButton,
                    UIButtonHandler.ButtonActionType.TogglePanel,
                    "InventoryPanel",
                    "ToggleInventory"
                );
            }
            
            // Subscribe to button click events
            buttonHandler.OnButtonClicked += (actionName) =>
            {
                Debug.Log($"Button clicked: {actionName}");
            };
        }
    }
    
    /// <summary>
    /// Example: Opening a panel from code
    /// </summary>
    public void OpenMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Open();
        }
        else
        {
            UIPanelManager.Instance?.OpenPanel("MainMenuPanel");
        }
    }
    
    /// <summary>
    /// Example: Closing a panel from code
    /// </summary>
    public void CloseMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Close();
        }
        else
        {
            UIPanelManager.Instance?.ClosePanel("MainMenuPanel");
        }
    }
}
