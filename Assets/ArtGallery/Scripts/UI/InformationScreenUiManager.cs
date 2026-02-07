using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the information screen for a selected painting from the new JSON pipeline.
/// Stores the currently selected PaintingConfigNew so it can be reused by other UI flows.
///
/// You can have separate instances for desktop and mobile (for example, one on each
/// canvas). Use the <see cref="PlatformMode"/> setting to control which instance is
/// considered active on which platform. Gameplay code continues to use the static
/// <see cref="Instance"/> property and does not need to know about the variants.
/// </summary>
public class InformationScreenUiManager : MonoBehaviour
{
    public enum PlatformMode
    {
        Auto,       // Valid on all platforms (default)
        DesktopOnly,
        MobileOnly
    }

    /// <summary>
    /// Simple singleton-style accessor so gameplay code can easily send a PaintingConfigNew here.
    /// It will point to the best-matching instance for the current platform.
    /// </summary>
    public static InformationScreenUiManager Instance { get; private set; }

    [Header("Platform Variant")]
    [Tooltip("If you have separate info UIs for desktop and mobile, set one instance to DesktopOnly and one to MobileOnly. Leave as Auto if this instance is valid everywhere.")]
    [SerializeField] private PlatformMode _platformMode = PlatformMode.Auto;

    [Header("Current Selection (New JSON)")]
    [Tooltip("The PaintingConfigNew currently selected by the player.")]
    [SerializeField] private PaintingConfigNew _currentPainting;

    [Header("Cart / Favourite State")]
    [Tooltip("True when the current painting is marked as added to the user's cart.")]
    [SerializeField] private bool isAddedToCart = false;

    [Tooltip("True when the current painting is marked as added to the user's favourites.")]
    [SerializeField] private bool isAddedToFavorite = false;

    [Header("Cart / Favourite Buttons")]
    [Tooltip("Button used to toggle add/remove cart for the current painting.")]
    [SerializeField] private Button addToCart;

    [Tooltip("Button used to toggle add/remove favourite for the current painting.")]
    [SerializeField] private Button addToFavorite;

    [Header("API Manager")]
    [Tooltip("Reference to the APIManager that handles cart and favourite API calls.")]
    [SerializeField] private APIManager apiManager;

    [Header("UI Bindings")]
    [Tooltip("Main artwork image UI element (e.g., on the information panel).")]
    [SerializeField] private Image mainImage;

    [Tooltip("Text element used to display the painting description (TextMeshProUGUI).")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("Optional text element used to display the painting title.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Dropdown listing available sizes/prices for the current painting.")]
    [SerializeField] private TMP_Dropdown sizesDropdown;

    [Tooltip("If true, fall back to shortDescription when description is empty.")]
    [SerializeField] private bool useShortDescriptionFallback = true;

    [Header("Secondary Info Panel")]
    [Tooltip("Optional panel that should open when the same painting is clicked a second time (e.g., detailed info panel).")]
    [SerializeField] private GameObject secondaryInfoPanel;

    [Header("Info Panel Visibility")]
    [Tooltip("GameObjects that should be shown when a painting is selected and hidden when the player moves (WASD/arrow keys).")]
    [SerializeField] private GameObject[] infoObjects;

    // Internal flag so we only process movement-hide logic when needed.
    private bool _infoObjectsVisible = false;

    private void Awake()
    {
        RegisterInstanceForPlatform();

        // Resolve APIManager automatically if not wired in the Inspector.
        if (apiManager == null)
        {
            apiManager = FindFirstObjectByType<APIManager>();
        }

        // Wire up button listeners for cart / favourite toggles.
        if (addToCart != null)
        {
            addToCart.onClick.AddListener(OnAddToCartButtonClicked);
        }

        if (addToFavorite != null)
        {
            addToFavorite.onClick.AddListener(OnAddToFavoriteButtonClicked);
        }
    }

    private void OnEnable()
    {
        // In case this GameObject is re-enabled at runtime, ensure the correct instance is registered.
        RegisterInstanceForPlatform();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Clean up button listeners
        if (addToCart != null)
        {
            addToCart.onClick.RemoveListener(OnAddToCartButtonClicked);
        }

        if (addToFavorite != null)
        {
            addToFavorite.onClick.RemoveListener(OnAddToFavoriteButtonClicked);
        }
    }

    private void Update()
    {
        // If the info objects are visible, hide them as soon as the user provides movement input.
        if (!_infoObjectsVisible)
            return;

        if (IsMovementKeyPressed())
        {
            HideInfoObjects();
        }
    }

    /// <summary>
    /// Register this component as the active Instance if it is a better match for the
    /// current platform than whatever is currently registered.
    /// </summary>
    private void RegisterInstanceForPlatform()
    {
        bool isMobile = Application.isMobilePlatform;

        if (!IsValidForCurrentPlatform(this, isMobile))
        {
            // This instance is not intended for the current platform; do not claim Instance.
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            return;
        }

        // If there is an existing instance but it is not valid for this platform,
        // or this instance is more specific (e.g. MobileOnly vs Auto on mobile),
        // prefer this one.
        bool existingValid = IsValidForCurrentPlatform(Instance, isMobile);

        if (!existingValid)
        {
            Instance = this;
            return;
        }

        // Both are valid for the current platform. Prefer the more specific mode:
        // DesktopOnly/MobileOnly beats Auto.
        int thisPriority = GetPlatformPriority(_platformMode, isMobile);
        int existingPriority = GetPlatformPriority(Instance._platformMode, isMobile);

        if (thisPriority > existingPriority)
        {
            Instance = this;
        }
    }

    private static bool IsValidForCurrentPlatform(InformationScreenUiManager mgr, bool isMobile)
    {
        if (mgr == null) return false;

        switch (mgr._platformMode)
        {
            case PlatformMode.DesktopOnly:
                return !isMobile;
            case PlatformMode.MobileOnly:
                return isMobile;
            default:
                return true;
        }
    }

    private static int GetPlatformPriority(PlatformMode mode, bool isMobile)
    {
        // Higher value means more specific/preferred for this platform.
        switch (mode)
        {
            case PlatformMode.DesktopOnly:
                return isMobile ? 0 : 2;
            case PlatformMode.MobileOnly:
                return isMobile ? 2 : 0;
            default:
                return 1; // Auto
        }
    }

    /// <summary>
    /// Called from ArtworkFrame when the player clicks a frame while standing at its target position.
    /// Stores the painting data and refreshes the UI.
    /// </summary>
    public void SetPainting(PaintingConfigNew painting)
    {
        // Determine if this is a "second click" on the same painting.
        bool isSecondClickOnSamePainting = false;

        if (painting != null && _currentPainting != null)
        {
            // Prefer stable ID comparison when available (uuid), fall back to reference.
            if (!string.IsNullOrEmpty(painting.uuid) && !string.IsNullOrEmpty(_currentPainting.uuid))
            {
                isSecondClickOnSamePainting = painting.uuid == _currentPainting.uuid;
            }
            else
            {
                isSecondClickOnSamePainting = ReferenceEquals(painting, _currentPainting);
            }
        }

        _currentPainting = painting;
        UpdateUI();

        if (isSecondClickOnSamePainting)
        {
            OpenSecondaryInfoPanel();
        }
    }

    /// <summary>
    /// Returns the last painting that was sent to this manager.
    /// </summary>
    public PaintingConfigNew GetCurrentPainting()
    {
        return _currentPainting;
    }

    /// <summary>
    /// Called when the Add to Cart button is pressed. Toggles add/remove cart
    /// based on the current isAddedToCart flag.
    /// </summary>
    private void OnAddToCartButtonClicked()
    {
        if (apiManager == null)
        {
            Debug.LogWarning("InformationScreenUiManager: APIManager reference is not set; cannot modify cart.");
            return;
        }

        if (!isAddedToCart)
        {
            apiManager.AddToCart();
            isAddedToCart = true;
        }
        else
        {
            apiManager.RemoveFromCart();
            isAddedToCart = false;
        }
    }

    /// <summary>
    /// Called when the Add to Favourite button is pressed. Toggles add/remove
    /// favourite based on the current isAddedToFavorite flag.
    /// </summary>
    private void OnAddToFavoriteButtonClicked()
    {
        if (apiManager == null)
        {
            Debug.LogWarning("InformationScreenUiManager: APIManager reference is not set; cannot modify favourite.");
            return;
        }

        if (!isAddedToFavorite)
        {
            apiManager.AddToFavourite();
            isAddedToFavorite = true;
        }
        else
        {
            apiManager.RemoveFromFavourite();
            isAddedToFavorite = false;
        }
    }

    /// <summary>
    /// Apply _currentPainting to the bound UI widgets (texts, images, dropdowns).
    /// Uses WebGLMediaCache for the main image so we don't re-download it.
    /// </summary>
    private void UpdateUI()
    {
        if (_currentPainting == null)
        {
            ClearUI();
            return;
        }

        // Ensure info objects are visible whenever a painting is selected.
        ShowInfoObjects();

        // Title
        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(_currentPainting.name)
                ? _currentPainting.name
                : (_currentPainting.slug ?? string.Empty);
        }

        // Description (with optional shortDescription fallback)
        if (descriptionText != null)
        {
            string desc = _currentPainting.description;
            if (useShortDescriptionFallback && string.IsNullOrWhiteSpace(desc))
            {
                desc = _currentPainting.shortDescription;
            }

            descriptionText.text = desc ?? string.Empty;
        }

        // Main image via cached Texture2D -> Sprite
        if (mainImage != null)
        {
            string imageUrl = _currentPainting.mainImage != null ? _currentPainting.mainImage.src : null;
            if (string.IsNullOrEmpty(imageUrl))
            {
                mainImage.sprite = null;
            }
            else if (WebGLMediaCache.TryGetTexture(imageUrl, out var tex))
            {
                // Create a sprite view onto the cached texture.
                // Note: textures coming from ArtworkManagerNew have already been flipped
                // vertically to render correctly on 3D quads. To show them upright in the
                // UI Image, we create the sprite with a vertically inverted rect.
                var rect = new Rect(0, tex.height, tex.width, -tex.height);
                var pivot = new Vector2(0.5f, 0.5f);
                mainImage.sprite = Sprite.Create(tex, rect, pivot);
            }
            else
            {
                // If not yet cached (e.g., opened info screen before layout downloads finished),
                // we can either clear the sprite or kick off a download. For now, just clear.
                mainImage.sprite = null;
            }
        }

        // Sizes dropdown from price list
        if (sizesDropdown != null)
        {
            sizesDropdown.ClearOptions();

            var priceList = _currentPainting.price;
            if (priceList != null && priceList.Count > 0)
            {
                var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();

                foreach (var p in priceList)
                {
                    if (p == null)
                        continue;

                    // Only show the size string in the dropdown.
                    string sizeLabel = string.IsNullOrEmpty(p.size) ? string.Empty : p.size;
                    options.Add(new TMP_Dropdown.OptionData(sizeLabel));
                }

                sizesDropdown.AddOptions(options);
                sizesDropdown.value = 0;
                sizesDropdown.RefreshShownValue();
            }
        }
    }

    /// <summary>
    /// Clears bound UI widgets when there is no current painting.
    /// </summary>
    private void ClearUI()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (mainImage != null)
            mainImage.sprite = null;

        if (sizesDropdown != null)
        {
            sizesDropdown.ClearOptions();
        }

        HideInfoObjects();
    }

    /// <summary>
    /// Returns true if any of the common movement keys were pressed this frame.
    /// </summary>
    private bool IsMovementKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) ||
               Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.RightArrow);
    }

    /// <summary>
    /// Shows all configured info GameObjects.
    /// </summary>
    private void ShowInfoObjects()
    {
        if (infoObjects == null)
            return;

        foreach (var go in infoObjects)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        _infoObjectsVisible = true;
    }

    /// <summary>
    /// Hides all configured info GameObjects.
    /// </summary>
    private void HideInfoObjects()
    {
        if (infoObjects == null)
            return;

        foreach (var go in infoObjects)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }

        _infoObjectsVisible = false;
    }

    /// <summary>
    /// Opens the secondary info panel (if assigned). Intended to be called when the
    /// same painting is clicked a second time.
    /// </summary>
    private void OpenSecondaryInfoPanel()
    {
        if (secondaryInfoPanel == null)
            return;

        secondaryInfoPanel.SetActive(true);
    }
}
