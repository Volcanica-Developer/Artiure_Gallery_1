using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Central manager for all UI panels. Handles panel registration, opening/closing,
/// and ensures only one panel is open at a time (optional).
/// </summary>
public class UIPanelManager : MonoBehaviour
{
    [Header("Panel Management")]
    [SerializeField] private bool singlePanelMode = true; // Only one panel open at a time
    [SerializeField] private bool closeOnEscape = true;
    
    [Header("Registered Panels")]
    [SerializeField] private List<UIPanel> registeredPanels = new List<UIPanel>();
    
    [Header("Events")]
    public Action<UIPanel> OnPanelOpened;
    public Action<UIPanel> OnPanelClosed;
    
    private Dictionary<string, UIPanel> panelDictionary = new Dictionary<string, UIPanel>();
    private UIPanel currentOpenPanel;
    private static UIPanelManager instance;
    
    public static UIPanelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIPanelManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UIPanelManager");
                    instance = go.AddComponent<UIPanelManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        RegisterAllPanels();
    }
    
    private void Update()
    {
        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCurrentPanel();
        }
    }
    
    /// <summary>
    /// Registers all panels found in the registeredPanels list.
    /// </summary>
    private void RegisterAllPanels()
    {
        panelDictionary.Clear();
        
        foreach (var panel in registeredPanels)
        {
            if (panel != null)
            {
                RegisterPanel(panel);
            }
        }
    }
    
    /// <summary>
    /// Registers a panel with the manager.
    /// </summary>
    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        
        string panelName = panel.gameObject.name;
        
        if (panelDictionary.ContainsKey(panelName))
        {
            Debug.LogWarning($"UIPanelManager: Panel '{panelName}' is already registered.");
            return;
        }
        
        panelDictionary[panelName] = panel;
        
        // Subscribe to panel events
        panel.OnPanelOpened += () => HandlePanelOpened(panel);
        panel.OnPanelClosed += () => HandlePanelClosed(panel);
    }
    
    /// <summary>
    /// Unregisters a panel from the manager.
    /// </summary>
    public void UnregisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        
        string panelName = panel.gameObject.name;
        
        if (panelDictionary.ContainsKey(panelName))
        {
            panel.OnPanelOpened -= () => HandlePanelOpened(panel);
            panel.OnPanelClosed -= () => HandlePanelClosed(panel);
            panelDictionary.Remove(panelName);
        }
    }
    
    /// <summary>
    /// Opens a panel by name.
    /// </summary>
    public void OpenPanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out UIPanel panel))
        {
            OpenPanel(panel);
        }
        else
        {
            Debug.LogWarning($"UIPanelManager: Panel '{panelName}' not found.");
        }
    }
    
    /// <summary>
    /// Opens a panel directly.
    /// </summary>
    public void OpenPanel(UIPanel panel)
    {
        if (panel == null) return;
        
        if (singlePanelMode && currentOpenPanel != null && currentOpenPanel != panel && currentOpenPanel.IsOpen)
        {
            currentOpenPanel.Close();
        }
        
        panel.Open();
    }
    
    /// <summary>
    /// Closes a panel by name.
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out UIPanel panel))
        {
            ClosePanel(panel);
        }
        else
        {
            Debug.LogWarning($"UIPanelManager: Panel '{panelName}' not found.");
        }
    }
    
    /// <summary>
    /// Closes a panel directly.
    /// </summary>
    public void ClosePanel(UIPanel panel)
    {
        if (panel != null)
        {
            panel.Close();
        }
    }
    
    /// <summary>
    /// Closes the currently open panel.
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentOpenPanel != null && currentOpenPanel.IsOpen)
        {
            currentOpenPanel.Close();
        }
    }
    
    /// <summary>
    /// Closes all open panels.
    /// </summary>
    public void CloseAllPanels()
    {
        foreach (var panel in panelDictionary.Values)
        {
            if (panel.IsOpen)
            {
                panel.Close();
            }
        }
    }
    
    /// <summary>
    /// Toggles a panel by name.
    /// </summary>
    public void TogglePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out UIPanel panel))
        {
            TogglePanel(panel);
        }
    }
    
    /// <summary>
    /// Toggles a panel directly.
    /// </summary>
    public void TogglePanel(UIPanel panel)
    {
        if (panel == null) return;
        
        if (panel.IsOpen)
            ClosePanel(panel);
        else
            OpenPanel(panel);
    }
    
    /// <summary>
    /// Gets a panel by name.
    /// </summary>
    public UIPanel GetPanel(string panelName)
    {
        panelDictionary.TryGetValue(panelName, out UIPanel panel);
        return panel;
    }
    
    /// <summary>
    /// Checks if a panel is currently open.
    /// </summary>
    public bool IsPanelOpen(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out UIPanel panel))
        {
            return panel.IsOpen;
        }
        return false;
    }
    
    private void HandlePanelOpened(UIPanel panel)
    {
        currentOpenPanel = panel;
        OnPanelOpened?.Invoke(panel);
    }
    
    private void HandlePanelClosed(UIPanel panel)
    {
        if (currentOpenPanel == panel)
        {
            currentOpenPanel = null;
        }
        OnPanelClosed?.Invoke(panel);
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        foreach (var panel in panelDictionary.Values)
        {
            if (panel != null)
            {
                panel.OnPanelOpened -= () => HandlePanelOpened(panel);
                panel.OnPanelClosed -= () => HandlePanelClosed(panel);
            }
        }
    }
}
