using UnityEngine;

/// <summary>
/// Helper script to quickly setup a complete gallery scene.
/// Attach to an empty GameObject and click "Setup Complete Gallery" in inspector.
/// </summary>
public class GallerySetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    [SerializeField] private bool setupOnStart = false;
    
    [ContextMenu("Setup Complete Gallery")]
    public void SetupCompleteGallery()
    {
        Debug.Log("Setting up complete gallery scene...");
        
        // Create player
        SetupPlayer();
        
        // Create gallery room
        SetupGalleryRoom();
        
        // Create UI
        SetupUI();
        
        // Create artwork manager
        SetupArtworkManager();
        
        Debug.Log("Gallery setup complete! Don't forget to assign artwork data and configure materials.");
    }
    
    private void SetupPlayer()
    {
        // Check if player already exists
        if (GameObject.Find("Player") != null)
        {
            Debug.Log("Player already exists, skipping...");
            return;
        }
        
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1.6f, 0);
        
        // Add Character Controller
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0, 0.9f, 0);
        
        // Add First Person Controller
        player.AddComponent<FirstPersonController>();
        
        // Add Camera
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 75f;
        
        // Add Audio Listener
        cameraObject.AddComponent<AudioListener>();
        
        // Add Artwork Raycast Interactor
        cameraObject.AddComponent<ArtworkRaycastInteractor>();
        
        Debug.Log("Player created successfully.");
    }
    
    private void SetupGalleryRoom()
    {
        // Check if gallery already exists
        if (GameObject.Find("Gallery Room") != null)
        {
            Debug.Log("Gallery room already exists, skipping...");
            return;
        }
        
        GameObject galleryBuilderObject = new GameObject("Gallery Builder");
        GalleryBuilder builder = galleryBuilderObject.AddComponent<GalleryBuilder>();
        
        // Build the room
        builder.BuildRoom();
        
        Debug.Log("Gallery room created successfully.");
    }
    
    private void SetupUI()
    {
        // Check if canvas already exists
        if (GameObject.Find("Canvas") != null)
        {
            Debug.Log("Canvas already exists, skipping...");
            return;
        }
        
        // Create Canvas
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create Event System if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Create Artwork Panel
        GameObject panel = new GameObject("ArtworkPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 800);
        panelRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Add ArtworkUI component
        ArtworkUI artworkUI = panel.AddComponent<ArtworkUI>();
        
        // Create Close Button
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(panel.transform, false);
        RectTransform closeButtonRect = closeButton.AddComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 1f);
        closeButtonRect.anchorMax = new Vector2(1f, 1f);
        closeButtonRect.sizeDelta = new Vector2(40, 40);
        closeButtonRect.anchoredPosition = new Vector2(-10, -10);
        
        UnityEngine.UI.Image buttonImage = closeButton.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = Color.red;
        
        UnityEngine.UI.Button button = closeButton.AddComponent<UnityEngine.UI.Button>();
        
        // Create button text
        GameObject buttonText = new GameObject("Text");
        buttonText.transform.SetParent(closeButton.transform, false);
        TMPro.TextMeshProUGUI text = buttonText.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "X";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        // Create Artwork Image
        GameObject artworkImage = new GameObject("ArtworkImage");
        artworkImage.transform.SetParent(panel.transform, false);
        RectTransform imageRect = artworkImage.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.7f);
        imageRect.anchorMax = new Vector2(0.5f, 0.95f);
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image img = artworkImage.AddComponent<UnityEngine.UI.Image>();
        img.preserveAspect = true;
        
        // Create Title Text
        GameObject titleText = new GameObject("TitleText");
        titleText.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleText.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.55f);
        titleRect.anchorMax = new Vector2(0.9f, 0.65f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;
        TMPro.TextMeshProUGUI title = titleText.AddComponent<TMPro.TextMeshProUGUI>();
        title.text = "Title";
        title.fontSize = 32;
        title.fontStyle = TMPro.FontStyles.Bold;
        title.color = Color.white;
        
        // Create Artist Text
        GameObject artistText = new GameObject("ArtistText");
        artistText.transform.SetParent(panel.transform, false);
        RectTransform artistRect = artistText.AddComponent<RectTransform>();
        artistRect.anchorMin = new Vector2(0.1f, 0.48f);
        artistRect.anchorMax = new Vector2(0.9f, 0.54f);
        artistRect.sizeDelta = Vector2.zero;
        artistRect.anchoredPosition = Vector2.zero;
        TMPro.TextMeshProUGUI artist = artistText.AddComponent<TMPro.TextMeshProUGUI>();
        artist.text = "Artist";
        artist.fontSize = 24;
        artist.color = new Color(0.8f, 0.8f, 0.8f);
        
        // Create Description Text
        GameObject descText = new GameObject("DescriptionText");
        descText.transform.SetParent(panel.transform, false);
        RectTransform descRect = descText.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.1f, 0.15f);
        descRect.anchorMax = new Vector2(0.9f, 0.45f);
        descRect.sizeDelta = Vector2.zero;
        descRect.anchoredPosition = Vector2.zero;
        TMPro.TextMeshProUGUI desc = descText.AddComponent<TMPro.TextMeshProUGUI>();
        desc.text = "Description";
        desc.fontSize = 18;
        desc.color = Color.white;
        desc.enableWordWrapping = true;
        
        // Create Year Text
        GameObject yearText = new GameObject("YearText");
        yearText.transform.SetParent(panel.transform, false);
        RectTransform yearRect = yearText.AddComponent<RectTransform>();
        yearRect.anchorMin = new Vector2(0.1f, 0.1f);
        yearRect.anchorMax = new Vector2(0.5f, 0.14f);
        yearRect.sizeDelta = Vector2.zero;
        yearRect.anchoredPosition = Vector2.zero;
        TMPro.TextMeshProUGUI year = yearText.AddComponent<TMPro.TextMeshProUGUI>();
        year.text = "2024";
        year.fontSize = 16;
        year.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Create Medium Text
        GameObject mediumText = new GameObject("MediumText");
        mediumText.transform.SetParent(panel.transform, false);
        RectTransform mediumRect = mediumText.AddComponent<RectTransform>();
        mediumRect.anchorMin = new Vector2(0.5f, 0.1f);
        mediumRect.anchorMax = new Vector2(0.9f, 0.14f);
        mediumRect.sizeDelta = Vector2.zero;
        mediumRect.anchoredPosition = Vector2.zero;
        TMPro.TextMeshProUGUI medium = mediumText.AddComponent<TMPro.TextMeshProUGUI>();
        medium.text = "Medium";
        medium.fontSize = 16;
        medium.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Assign references to ArtworkUI
        artworkUI.panel = panel;
        artworkUI.artworkImage = img;
        artworkUI.titleText = title;
        artworkUI.artistText = artist;
        artworkUI.descriptionText = desc;
        artworkUI.yearText = year;
        artworkUI.mediumText = medium;
        artworkUI.closeButton = button;
        
        // Start with panel hidden
        panel.SetActive(false);
        
        Debug.Log("UI created successfully.");
    }
    
    private void SetupArtworkManager()
    {
        // Check if manager already exists
        if (FindObjectOfType<ArtworkManager>() != null)
        {
            Debug.Log("ArtworkManager already exists, skipping...");
            return;
        }
        
        GameObject managerObject = new GameObject("Artwork Manager");
        ArtworkManager manager = managerObject.AddComponent<ArtworkManager>();
        
        // Try to find ArtworkUI
        ArtworkUI artworkUI = FindObjectOfType<ArtworkUI>();
        if (artworkUI != null)
        {
            manager.artworkUI = artworkUI;
        }
        
        Debug.Log("ArtworkManager created successfully.");
    }
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupCompleteGallery();
        }
    }
}

