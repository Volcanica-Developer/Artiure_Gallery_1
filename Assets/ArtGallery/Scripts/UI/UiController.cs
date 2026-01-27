using System;
using System.Collections.Generic;
using UnityEngine;

public class UiController : MonoBehaviour
{
    public static UiController Instance { get; private set; }

    [Tooltip("Assign all UI manager components here (e.g. LoadingScreenUiManager, StartPanelUiManager, etc.")]
    [SerializeField] private MonoBehaviour[] uiManagers;

    private readonly Dictionary<Type, MonoBehaviour> _managers = new Dictionary<Type, MonoBehaviour>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple UiController instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _managers.Clear();
        foreach (var manager in uiManagers)
        {
            if (manager == null) continue;

            var type = manager.GetType();
            if (_managers.ContainsKey(type))
            {
                Debug.LogWarning($"UiController: Manager of type {type.Name} is already registered. Skipping duplicate.", manager);
                continue;
            }

            _managers[type] = manager;
        }
    }

    /// <summary>
    /// Get a UI manager by its type.
    /// </summary>
    public T GetManager<T>() where T : MonoBehaviour
    {
        if (_managers.TryGetValue(typeof(T), out var manager))
        {
            return manager as T;
        }

        Debug.LogWarning($"UiController: No manager of type {typeof(T).Name} registered.");
        return null;
    }

    /// <summary>
    /// Send a command/action to a specific UI manager type, if it exists.
    /// </summary>
    public void SendTo<T>(Action<T> action) where T : MonoBehaviour
    {
        if (action == null) return;

        var manager = GetManager<T>();
        if (manager == null) return;

        action(manager);
    }

    /// <summary>
    /// Query a specific UI manager type for some result.
    /// </summary>
    public TResult Query<T, TResult>(Func<T, TResult> query) where T : MonoBehaviour
    {
        if (query == null) return default;

        var manager = GetManager<T>();
        if (manager == null) return default;

        return query(manager);
    }

    // Convenience strongly-typed accessors for your known managers
    public LoadingScreenUiManager LoadingScreen => GetManager<LoadingScreenUiManager>();
    public InstructionsPanelUiManager InstructionsPanel => GetManager<InstructionsPanelUiManager>();
    public StartPanelUiManager StartPanel => GetManager<StartPanelUiManager>();
    public MenuPopUpUiManager MenuPopUp => GetManager<MenuPopUpUiManager>();
    public InformationScreenUiManager InformationScreen => GetManager<InformationScreenUiManager>();
}
