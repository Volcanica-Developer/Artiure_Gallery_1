using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuPopUpUiManager : MonoBehaviour
{

    [SerializeField] private GameObject inputPopUp;
    [SerializeField] private GameObject menuPopUp;

    [SerializeField] private GameObject inputDropDown;

    [SerializeField] private Button fullscreenToggle;
    [SerializeField] private Button musicToggle;

    [SerializeField] private TMP_Text selectionText;

    [Header("Audio")]
    [SerializeField] private ArtworkManagerNew artworkManagerNew; // Provides ToggleExhibitionMusic()

    private bool isFullscreen;
    private bool isPlayed; // true = music playing, false = music stopped

    private void Awake()
    {
        // If not wired in the Inspector, try to find one in the scene.
        if (artworkManagerNew == null)
        {
            artworkManagerNew = FindFirstObjectByType<ArtworkManagerNew>();
        }
    }

    private void Start()
    {
        isFullscreen = false;

        // Music is ON by default (ArtworkManagerNew starts it after loading),
        // so keep isPlayed = true and show the "Active" state initially.
        isPlayed = true;
        if (musicToggle != null)
        {
            var activeIcon = musicToggle.gameObject.transform.Find("Active");
            var inactiveIcon = musicToggle.gameObject.transform.Find("Inactive");
            if (activeIcon != null)   activeIcon.gameObject.SetActive(true);
            if (inactiveIcon != null) inactiveIcon.gameObject.SetActive(false);
        }

        fullscreenToggle.onClick.AddListener(() => 
        {
            isFullscreen = !isFullscreen;

            if(isFullscreen)
            {
                fullscreenToggle.gameObject.transform.Find("Active").gameObject.SetActive(true);
                fullscreenToggle.gameObject.transform.Find("Inactive").gameObject.SetActive(false);
            }
            else
            {
                fullscreenToggle.gameObject.transform.Find("Active").gameObject.SetActive(false);
                fullscreenToggle.gameObject.transform.Find("Inactive").gameObject.SetActive(true);

            }
        });

        musicToggle.onClick.AddListener(() =>
        {
            // Toggle desired play state.
            isPlayed = !isPlayed;

            // Ask ArtworkManagerNew to toggle the actual AudioSource / music clip.
            if (artworkManagerNew != null)
            {
                artworkManagerNew.ToggleExhibitionMusic();
            }

            if (isPlayed)
            {
                musicToggle.gameObject.transform.Find("Active").gameObject.SetActive(true);
                musicToggle.gameObject.transform.Find("Inactive").gameObject.SetActive(false);
            }
            else
            {
                musicToggle.gameObject.transform.Find("Active").gameObject.SetActive(false);
                musicToggle.gameObject.transform.Find("Inactive").gameObject.SetActive(true);
            }
        });
    }

    public void OnClickOnBackground()
    {
        if (inputPopUp.activeInHierarchy)
        {
            inputPopUp.SetActive(false);
            return;
        }

        if (menuPopUp.activeInHierarchy)
        {
            menuPopUp.SetActive(false);
        }
    }

    // Toggle Menu Popup when click on the same btn
    public void OnClickMenuBtn()
    {
        menuPopUp.SetActive(!menuPopUp.activeInHierarchy);
    }


    public void OnClickOnInputBtn()
    {
        inputDropDown.SetActive(true);
    }

    public void OnInputSelection(string selection)
    {
        inputDropDown.SetActive(false);

        if (selection == "Desktop")
        {
            selectionText.text = selection;
        }

        if(selection == "Mobile")
        {
            selectionText.text = selection;
        }
    }
}
