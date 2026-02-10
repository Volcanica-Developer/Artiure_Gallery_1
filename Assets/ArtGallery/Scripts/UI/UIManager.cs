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

    [Header("Frame Navigation")]
    [Tooltip("If true, UIManager will automatically find all ArtworkFrame objects in the scene.")]
    [SerializeField] private bool autoFindFrames = true;

    [Header("Debug - Cached Frames (Read Only)")]
    [Tooltip("List of all ArtworkFrames cached for navigation, sorted by DisplayWall ID -> Slot -> Frame name.")]
    [SerializeField] private List<ArtworkFrame> cachedFrames = new List<ArtworkFrame>();
    
    [SerializeField] private int currentFrameIndex = -1;
    [SerializeField] private bool framesCached = false;
    
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

        // Subscribe to ArtworkManagerNew events to auto-populate frame cache when API data loads
        SubscribeToArtworkManagerEvents();
        
        if (logUIEvents)
            Debug.Log("UIManager: Initialized");
        
        OnUIManagerInitialized?.Invoke();
    }

    /// <summary>
    /// Subscribes to ArtworkManagerNew events for automatic frame cache population.
    /// </summary>
    private void SubscribeToArtworkManagerEvents()
    {
        ArtworkManagerNew artworkManager = FindObjectOfType<ArtworkManagerNew>();
        if (artworkManager != null)
        {
            artworkManager.OnAllImagesDownloaded += OnArtworkImagesLoaded;
            if (logUIEvents)
                Debug.Log("UIManager: Subscribed to ArtworkManagerNew.OnAllImagesDownloaded");
        }
        else
        {
            if (logUIEvents)
                Debug.LogWarning("UIManager: ArtworkManagerNew not found in scene. Frame cache will not auto-populate.");
        }
    }

    /// <summary>
    /// Called when ArtworkManagerNew finishes downloading all images.
    /// Refreshes the frame cache so navigation is ready.
    /// </summary>
    private void OnArtworkImagesLoaded()
    {
        if (logUIEvents)
            Debug.Log("UIManager: API images loaded, refreshing frame cache...");
        
        RefreshFrameCache();
        CacheFramesIfNeeded();
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

    #region Frame Navigation API

    /// <summary>
    /// Moves the player to focus on the next ArtworkFrame.
    /// Hook this up to a UI Button's OnClick event for "Next" navigation.
    /// </summary>
    public void FocusNextFrame()
    {
        FocusRelativeFrame(+1);
    }

    /// <summary>
    /// Moves the player to focus on the previous ArtworkFrame.
    /// Hook this up to a UI Button's OnClick event for "Previous" navigation.
    /// </summary>
    public void FocusPreviousFrame()
    {
        FocusRelativeFrame(-1);
    }

    /// <summary>
    /// Legacy method names for backward compatibility with existing button bindings.
    /// </summary>
    public void FocusNextDisplayWall() => FocusNextFrame();
    public void FocusPreviousDisplayWall() => FocusPreviousFrame();

    /// <summary>
    /// Finds all ArtworkFrame components in the scene and caches them.
    /// Frames are sorted by their parent DisplayWall's displayWallId first,
    /// then by the frame's sibling index within each wall.
    /// </summary>
    private void CacheFramesIfNeeded()
    {
        if (framesCached)
            return;

        cachedFrames.Clear();

        if (autoFindFrames)
        {
#if UNITY_2023_1_OR_NEWER
            var allFrames = FindObjectsByType<ArtworkFrame>(FindObjectsSortMode.None);
#else
            var allFrames = FindObjectsOfType<ArtworkFrame>();
#endif
            if (allFrames != null)
            {
                cachedFrames.AddRange(allFrames);
            }
        }

        // Sort frames by: 1) DisplayWall ID, 2) Slot index, 3) Frame name within layout
        cachedFrames.Sort((a, b) =>
        {
            DisplayWall wallA = GetParentDisplayWall(a);
            DisplayWall wallB = GetParentDisplayWall(b);

            // 1. Sort by DisplayWall ID (ascending)
            int wallIdA = wallA != null ? wallA.displayWallId : int.MaxValue;
            int wallIdB = wallB != null ? wallB.displayWallId : int.MaxValue;

            if (wallIdA != wallIdB)
                return wallIdA.CompareTo(wallIdB);

            // 2. Sort by slot index (ascending) - get from parent FrameLayout
            FrameLayout layoutA = GetParentFrameLayout(a);
            FrameLayout layoutB = GetParentFrameLayout(b);

            int slotA = layoutA != null ? layoutA.StartSlot : int.MaxValue;
            int slotB = layoutB != null ? layoutB.StartSlot : int.MaxValue;

            if (slotA != slotB)
                return slotA.CompareTo(slotB);

            // 3. Sort by frame name within the same layout (ascending)
            return string.Compare(a.name, b.name, StringComparison.Ordinal);
        });

        framesCached = true;

        if (logUIEvents)
        {
            Debug.Log($"UIManager: Cached {cachedFrames.Count} ArtworkFrame(s) for navigation.");
            
            // Log detailed sorting info for debugging
            for (int i = 0; i < cachedFrames.Count; i++)
            {
                var frame = cachedFrames[i];
                if (frame == null) continue;
                
                DisplayWall wall = GetParentDisplayWall(frame);
                FrameLayout layout = GetParentFrameLayout(frame);
                
                int wallId = wall != null ? wall.displayWallId : -1;
                int slot = layout != null ? layout.StartSlot : -1;
                
                Debug.Log($"  [{i}] Frame: '{frame.name}' | WallID: {wallId} | Slot: {slot}");
            }
        }
    }

    /// <summary>
    /// Finds the DisplayWall that is a parent of the given frame.
    /// </summary>
    private DisplayWall GetParentDisplayWall(ArtworkFrame frame)
    {
        if (frame == null)
            return null;

        return frame.GetComponentInParent<DisplayWall>();
    }

    /// <summary>
    /// Finds the FrameLayout that is a parent of the given frame.
    /// </summary>
    private FrameLayout GetParentFrameLayout(ArtworkFrame frame)
    {
        if (frame == null)
            return null;

        return frame.GetComponentInParent<FrameLayout>();
    }

    /// <summary>
    /// Returns the ArtworkFrame that is currently closest to the player.
    /// Updates currentFrameIndex as a side effect.
    /// </summary>
    private ArtworkFrame GetCurrentFrame()
    {
        CacheFramesIfNeeded();

        if (cachedFrames == null || cachedFrames.Count == 0)
            return null;

        FirstPersonController controller = GetFirstPersonController();
        if (controller == null)
        {
            currentFrameIndex = 0;
            return cachedFrames[0];
        }

        Transform playerTransform = controller.transform;
        float bestDistSq = float.MaxValue;
        ArtworkFrame bestFrame = null;
        int bestIndex = 0;

        for (int i = 0; i < cachedFrames.Count; i++)
        {
            ArtworkFrame frame = cachedFrames[i];
            if (frame == null)
                continue;

            // Use the computed standing position for this frame
            DisplayWall parentWall = GetParentDisplayWall(frame);
            Vector3 standPos;
            if (parentWall != null)
            {
                standPos = parentWall.ComputeStandingPositionForFrame(frame);
            }
            else
            {
                standPos = frame.transform.position;
            }

            float distSq = (standPos - playerTransform.position).sqrMagnitude;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestFrame = frame;
                bestIndex = i;
            }
        }

        currentFrameIndex = bestIndex;
        return bestFrame ?? cachedFrames[0];
    }

    /// <summary>
    /// Shared helper that moves to the next/previous ArtworkFrame in the cached list.
    /// </summary>
    private void FocusRelativeFrame(int direction)
    {
        CacheFramesIfNeeded();

        if (cachedFrames == null || cachedFrames.Count == 0)
        {
            Debug.LogWarning("UIManager: No ArtworkFrame objects found in the scene for navigation.");
            return;
        }

        // Only determine current frame from player position on FIRST navigation
        // After that, we track the index explicitly to avoid issues with player still moving
        if (currentFrameIndex < 0 || currentFrameIndex >= cachedFrames.Count)
        {
            GetCurrentFrame();
        }

        // Fallback if still invalid
        if (currentFrameIndex < 0 || currentFrameIndex >= cachedFrames.Count)
        {
            currentFrameIndex = 0;
        }

        int targetIndex = currentFrameIndex + direction;

        // Wrap around
        if (targetIndex < 0)
        {
            targetIndex = cachedFrames.Count - 1;
        }
        else if (targetIndex >= cachedFrames.Count)
        {
            targetIndex = 0;
        }

        ArtworkFrame targetFrame = cachedFrames[targetIndex];
        if (targetFrame == null)
        {
            Debug.LogWarning("UIManager: Target ArtworkFrame for navigation is null.");
            return;
        }

        // Find the parent DisplayWall to use its focus method
        DisplayWall parentWall = GetParentDisplayWall(targetFrame);
        if (parentWall == null)
        {
            Debug.LogWarning($"UIManager: ArtworkFrame '{targetFrame.name}' has no parent DisplayWall.");
            return;
        }

        currentFrameIndex = targetIndex;

        if (logUIEvents)
        {
            Debug.Log($"UIManager: Focusing ArtworkFrame '{targetFrame.name}' (index {targetIndex}) on DisplayWall ID {parentWall.displayWallId}.");
        }

        parentWall.FocusPlayerOnFrame(targetFrame);
    }

    /// <summary>
    /// Clears the cached frames list, forcing a refresh on next navigation.
    /// Call this if frames are dynamically added/removed at runtime.
    /// </summary>
    public void RefreshFrameCache()
    {
        framesCached = false;
        cachedFrames.Clear();
        currentFrameIndex = -1;
    }

    /// <summary>
    /// Gets the total number of cached frames available for navigation.
    /// </summary>
    public int GetFrameCount()
    {
        CacheFramesIfNeeded();
        return cachedFrames.Count;
    }

    /// <summary>
    /// Gets the current frame index (0-based). Returns -1 if not yet determined.
    /// </summary>
    public int GetCurrentFrameIndex()
    {
        return currentFrameIndex;
    }

    /// <summary>
    /// Recalculates the current frame index based on player position.
    /// Call this if the player manually walks to a different area and you want
    /// navigation to resume from their new position.
    /// </summary>
    public void RecalculateCurrentFrameFromPosition()
    {
        currentFrameIndex = -1;
        GetCurrentFrame();
        
        if (logUIEvents)
        {
            Debug.Log($"UIManager: Recalculated current frame index to {currentFrameIndex}");
        }
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

        // Unsubscribe from ArtworkManagerNew events
        ArtworkManagerNew artworkManager = FindObjectOfType<ArtworkManagerNew>();
        if (artworkManager != null)
        {
            artworkManager.OnAllImagesDownloaded -= OnArtworkImagesLoaded;
        }
    }
}
