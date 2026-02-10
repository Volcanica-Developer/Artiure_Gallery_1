using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbsoluteURLDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text urlText;


    private void Start()
    {
        DisplayAbsoluteURL();
    }

    public void DisplayAbsoluteURL()
    {
        string url = Application.absoluteURL;

        if (string.IsNullOrEmpty(url))
        {
            url = "No URL available (not running in browser)";
        }

        if (urlText != null)
        {
            urlText.text = "Absolute URL: " + url;
        }

        Debug.Log("Application.absoluteURL: " + url);
    }
}
