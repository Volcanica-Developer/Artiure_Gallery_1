using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Custom UI Manager for handling custom UI logic and interactions.
/// Extend this class or add your custom methods here to manage UI behavior.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Manager Settings")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool logUIEvents = false;
    
    [Header("UI References")]
    [Tooltip("Reference to UIPanelManager (optional, will find automatically if not set)")]
    [SerializeField] private UIPanelManager panelManager;
    
    [Header("Custom UI Elements")]
    [Tooltip("Custom GameObjects that you want to manage")]
    [SerializeField] private List<GameObject> customUIElements = new List<GameObject>();
    
    [Header("First Person Controller")]
    [Tooltip("Reference to the GameObject with FirstPersonController component (optional, will find automatically if not set)")]
    [SerializeField] private GameObject playerObject;
    
    [Tooltip("Direct reference to FirstPersonController component (optional)")]
    [SerializeField] private FirstPersonController firstPersonController;

    [Header("Display Wall Navigation")]
    [Tooltip("If true, UIManager will automatically find all DisplayWall objects in the scene and sort them by displayWallId.")]
    [SerializeField] private bool autoFindDisplayWalls = true;

    private List<DisplayWall> cachedDisplayWalls = new List<DisplayWall>();
    private bool displayWallsCached = false;
    
    // Events
    public Action OnUIManagerInitialized;
    public Action<string> OnCustomUIEvent;
    
    // Singleton instance
    private static UIManager instance;
    
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // Singleton pattern
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
        
        if (initializeOnAwake)
        {
            Initialize();
        }
    }
    
    /// <summary>
    /// Initializes the UI Manager.
    /// </summary>
    public void Initialize()
    {
        // Find panel manager if not set
        if (panelManager == null)
        {
            panelManager = UIPanelManager.Instance;
        }
        
        // Subscribe to panel events if panel manager exists
        if (panelManager != null)
        {
            panelManager.OnPanelOpened += HandlePanelOpened;
            panelManager.OnPanelClosed += HandlePanelClosed;
        }
        
        if (logUIEvents)
            Debug.Log("UIManager: Initialized");
        
        OnUIManagerInitialized?.Invoke();
    }
    
    /// <summary>
    /// Handles when a panel is opened.
    /// Override or extend this method for custom logic.
    /// </summary>
    protected virtual void HandlePanelOpened(UIPanel panel)
    {
        if (logUIEvents)
            Debug.Log($"UIManager: Panel '{panel.gameObject.name}' opened");
        
        // Add your custom logic here
        OnCustomUIEvent?.Invoke($"PanelOpened_{panel.gameObject.name}");
    }
    
    /// <summary>
    /// Handles when a panel is closed.
    /// Override or extend this method for custom logic.
    /// </summary>
    protected virtual void HandlePanelClosed(UIPanel panel)
    {
        if (logUIEvents)
            Debug.Log($"UIManager: Panel '{panel.gameObject.name}' closed");
        
        // Add your custom logic here
        OnCustomUIEvent?.Invoke($"PanelClosed_{panel.gameObject.name}");
    }
    
    /// <summary>
    /// Shows a custom UI element by name.
    /// </summary>
    public void ShowCustomElement(string elementName)
    {
        GameObject element = GetCustomElement(elementName);
        if (element != null)
        {
            element.SetActive(true);
            if (logUIEvents)
                Debug.Log($"UIManager: Showing element '{elementName}'");
        }
        else
        {
            Debug.LogWarning($"UIManager: Element '{elementName}' not found in custom elements list");
        }
    }
    
    /// <summary>
    /// Hides a custom UI element by name.
    /// </summary>
    public void HideCustomElement(string elementName)
    {
        GameObject element = GetCustomElement(elementName);
        if (element != null)
        {
            element.SetActive(false);
            if (logUIEvents)
                Debug.Log($"UIManager: Hiding element '{elementName}'");
        }
        else
        {
            Debug.LogWarning($"UIManager: Element '{elementName}' not found in custom elements list");
        }
    }
    
    /// <summary>
    /// Toggles a custom UI element by name.
    /// </summary>
    public void ToggleCustomElement(string elementName)
    {
        GameObject element = GetCustomElement(elementName);
        if (element != null)
        {
            element.SetActive(!element.activeSelf);
            if (logUIEvents)
                Debug.Log($"UIManager: Toggled element '{elementName}' to {(element.activeSelf ? "active" : "inactive")}");
        }
        else
        {
            Debug.LogWarning($"UIManager: Element '{elementName}' not found in custom elements list");
        }
    }
    
    /// <summary>
    /// Gets a custom UI element by name.
    /// </summary>
    public GameObject GetCustomElement(string elementName)
    {
        foreach (var element in customUIElements)
        {
            if (element != null && element.name == elementName)
            {
                return element;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Adds a custom UI element to the manager.
    /// </summary>
    public void AddCustomElement(GameObject element)
    {
        if (element != null && !customUIElements.Contains(element))
        {
            customUIElements.Add(element);
            if (logUIEvents)
                Debug.Log($"UIManager: Added custom element '{element.name}'");
        }
    }
    
    /// <summary>
    /// Removes a custom UI element from the manager.
    /// </summary>
    public void RemoveCustomElement(GameObject element)
    {
        if (customUIElements.Remove(element))
        {
            if (logUIEvents)
                Debug.Log($"UIManager: Removed custom element '{element.name}'");
        }
    }
    
    /// <summary>
    /// Shows all custom UI elements.
    /// </summary>
    public void ShowAllCustomElements()
    {
        foreach (var element in customUIElements)
        {
            if (element != null)
            {
                element.SetActive(true);
            }
        }
        
        if (logUIEvents)
            Debug.Log("UIManager: Showing all custom elements");
    }
    
    /// <summary>
    /// Hides all custom UI elements.
    /// </summary>
    public void HideAllCustomElements()
    {
        foreach (var element in customUIElements)
        {
            if (element != null)
            {
                element.SetActive(false);
            }
        }
        
        if (logUIEvents)
            Debug.Log("UIManager: Hiding all custom elements");
    }
    
    /// <summary>
    /// Triggers a custom UI event.
    /// Use this to communicate UI state changes to other systems.
    /// </summary>
    public void TriggerCustomEvent(string eventName)
    {
        if (logUIEvents)
            Debug.Log($"UIManager: Custom event triggered - '{eventName}'");
        
        OnCustomUIEvent?.Invoke(eventName);
    }
    
    /// <summary>
    /// Gets the panel manager reference.
    /// </summary>
    public UIPanelManager GetPanelManager()
    {
        return panelManager;
    }
    
    /// <summary>
    /// Enables the FirstPersonController component on the player GameObject.
    /// </summary>
    public void EnableFirstPersonController()
    {
        FirstPersonController controller = GetFirstPersonController();
        if (controller != null)
        {
            controller.enabled = true;
            if (logUIEvents)
                Debug.Log($"UIManager: Enabled FirstPersonController on '{controller.gameObject.name}'");
        }
        else
        {
            Debug.LogWarning("UIManager: FirstPersonController not found. Make sure player object is set or exists in scene.");
        }
    }
    
    /// <summary>
    /// Disables the FirstPersonController component on the player GameObject.
    /// </summary>
    public void DisableFirstPersonController()
    {
        FirstPersonController controller = GetFirstPersonController();
        if (controller != null)
        {
            controller.enabled = false;
            if (logUIEvents)
                Debug.Log($"UIManager: Disabled FirstPersonController on '{controller.gameObject.name}'");
        }
        else
        {
            Debug.LogWarning("UIManager: FirstPersonController not found. Make sure player object is set or exists in scene.");
        }
    }
    
    /// <summary>
    /// Toggles the FirstPersonController component on the player GameObject.
    /// </summary>
    public void ToggleFirstPersonController()
    {
        FirstPersonController controller = GetFirstPersonController();
        if (controller != null)
        {
            controller.enabled = !controller.enabled;
            if (logUIEvents)
                Debug.Log($"UIManager: Toggled FirstPersonController on '{controller.gameObject.name}' to {(controller.enabled ? "enabled" : "disabled")}");
        }
        else
        {
            Debug.LogWarning("UIManager: FirstPersonController not found. Make sure player object is set or exists in scene.");
        }
    }
    
    /// <summary>
    /// Gets the FirstPersonController component.
    /// Tries direct reference first, then player object, then searches scene.
    /// </summary>
    private FirstPersonController GetFirstPersonController()
    {
        // Try direct reference first
        if (firstPersonController != null)
        {
            return firstPersonController;
        }
        
        // Try to get from player object
        if (playerObject != null)
        {
            FirstPersonController controller = playerObject.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                firstPersonController = controller; // Cache it
                return controller;
            }
        }
        
        // Search scene for FirstPersonController
        FirstPersonController foundController = FindObjectOfType<FirstPersonController>();
        if (foundController != null)
        {
            firstPersonController = foundController; // Cache it
            playerObject = foundController.gameObject; // Cache player object too
            return foundController;
        }
        
        return null;
    }
    
    /// <summary>
    /// Sets the player GameObject reference.
    /// </summary>
    public void SetPlayerObject(GameObject player)
    {
        playerObject = player;
        if (player != null)
        {
            firstPersonController = player.GetComponent<FirstPersonController>();
            if (firstPersonController == null)
            {
                Debug.LogWarning($"UIManager: GameObject '{player.name}' does not have FirstPersonController component.");
            }
        }
    }
    
    /// <summary>
    /// Gets the current FirstPersonController component reference.
    /// </summary>
    public FirstPersonController GetFirstPersonControllerReference()
    {
        return GetFirstPersonController();
    }

    #region Display Wall Navigation API

    /// <summary>
    /// Moves the player to the next DisplayWall (by displayWallId) relative to the current one.
    /// Hook this up to a UI Button's OnClick event for "Next" navigation.
    /// </summary>
    public void FocusNextDisplayWall()
    {
        FocusRelativeDisplayWall(+1);
    }

    /// <summary>
    /// Moves the player to the previous DisplayWall (by displayWallId) relative to the current one.
    /// Hook this up to a UI Button's OnClick event for "Previous" navigation.
    /// </summary>
    public void FocusPreviousDisplayWall()
    {
        FocusRelativeDisplayWall(-1);
    }

    /// <summary>
    /// Finds all DisplayWall components in the scene and caches them sorted by displayWallId.
    /// </summary>
    private void CacheDisplayWallsIfNeeded()
    {
        if (displayWallsCached)
            return;

        cachedDisplayWalls.Clear();

        if (autoFindDisplayWalls)
        {
#if UNITY_2023_1_OR_NEWER
            var found = FindObjectsByType<DisplayWall>(FindObjectsSortMode.None);
#else
            var found = FindObjectsOfType<DisplayWall>();
#endif
            if (found != null)
            {
                cachedDisplayWalls.AddRange(found);
            }
        }

        // Sort by the configured displayWallId so navigation order is deterministic.
        cachedDisplayWalls.Sort((a, b) => a.displayWallId.CompareTo(b.displayWallId));
        displayWallsCached = true;
    }

    /// <summary>
    /// Returns the DisplayWall that is currently "selected" based on the player's position.
    /// Assumes the current wall is the one whose standing point is closest to the player.
    /// </summary>
    private DisplayWall GetCurrentDisplayWall()
    {
        CacheDisplayWallsIfNeeded();

        if (cachedDisplayWalls == null || cachedDisplayWalls.Count == 0)
            return null;

        FirstPersonController controller = GetFirstPersonController();
        if (controller == null)
        {
            // If we don't have a player reference, just return the first wall.
            return cachedDisplayWalls[0];
        }

        Transform playerTransform = controller.transform;
        float bestDistSq = float.MaxValue;
        DisplayWall bestWall = null;

        foreach (var wall in cachedDisplayWalls)
        {
            if (wall == null)
                continue;

            Transform standPoint = wall.standingPoint != null ? wall.standingPoint : wall.transform;
            float distSq = (standPoint.position - playerTransform.position).sqrMagnitude;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestWall = wall;
            }
        }

        return bestWall ?? cachedDisplayWalls[0];
    }

    /// <summary>
    /// Shared helper that moves to the next/previous DisplayWall in the cached list.
    /// </summary>
    private void FocusRelativeDisplayWall(int direction)
    {
        CacheDisplayWallsIfNeeded();

        if (cachedDisplayWalls == null || cachedDisplayWalls.Count == 0)
        {
            Debug.LogWarning("UIManager: No DisplayWall objects found in the scene for navigation.");
            return;
        }

        DisplayWall current = GetCurrentDisplayWall();
        if (current == null)
        {
            Debug.LogWarning("UIManager: Could not determine current DisplayWall; falling back to first in list.");
            current = cachedDisplayWalls[0];
        }

        int currentIndex = cachedDisplayWalls.IndexOf(current);
        if (currentIndex < 0)
        {
            // If current wall isn't in the list for some reason, default to first.
            currentIndex = 0;
        }

        int targetIndex = currentIndex + direction;

        if (targetIndex < 0)
        {
            targetIndex = cachedDisplayWalls.Count - 1; // wrap to last
        }
        else if (targetIndex >= cachedDisplayWalls.Count)
        {
            targetIndex = 0; // wrap to first
        }

        DisplayWall targetWall = cachedDisplayWalls[targetIndex];
        if (targetWall == null)
        {
            Debug.LogWarning("UIManager: Target DisplayWall for navigation is null.");
            return;
        }

        if (logUIEvents)
        {
            Debug.Log($"UIManager: Focusing DisplayWall ID {targetWall.displayWallId} (index {targetIndex}).");
        }

        targetWall.FocusPlayer();
    }

    #endregion
    
    private void OnDestroy()
    {
        // Unsubscribe from panel events
        if (panelManager != null)
        {
            panelManager.OnPanelOpened -= HandlePanelOpened;
            panelManager.OnPanelClosed -= HandlePanelClosed;
        }
    }
}
