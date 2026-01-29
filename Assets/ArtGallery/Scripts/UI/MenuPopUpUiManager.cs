using UnityEngine;
using UnityEngine.UI;

public class MenuPopUpUiManager : MonoBehaviour
{

    [SerializeField] private GameObject inputPopUp;
    [SerializeField] private GameObject menuPopUp;

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
}
