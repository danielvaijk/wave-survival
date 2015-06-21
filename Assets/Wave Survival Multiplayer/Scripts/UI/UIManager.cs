using UnityEngine;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Manages the UI elements throught the game.

public class UIManager : MonoBehaviour
{
    [HideInInspector]
    public NetworkUser localNetworkUser = null;

    private string currentUI = "Main Menu";

    // Called when the script instance is being loaded.
    private void Awake ()
    {
        // Prevent the UI GameObject from being destroyed between scene changes.
        DontDestroyOnLoad(gameObject);

        // If there already is another UI instance, destroy this one.
        if (FindObjectsOfType<UIManager>().Length > 1)
        {
            DestroyImmediate(gameObject);
        }
    }

    // Will change from the current active UI to another one and return the Transform
    // of the newly changed UI element.
    public Transform ChangeUI (string newUI)
    {
        transform.Find(currentUI).gameObject.SetActive(false);
        transform.Find(newUI).gameObject.SetActive(true);

        currentUI = newUI;

        return transform.Find(currentUI);
    }

    // Will iterate through all the UI targets in <targets> and return the last
    // child of the list, if any.
    public GameObject GetUIObject (string[] targets)
    {
        Transform nextTransform = transform;

        for (int i = 0; i < targets.Length; i++)
        {
            nextTransform = nextTransform.Find(targets[i]);
        }

        return nextTransform.gameObject;
    }

    // Called when the Escape Menu -> "Resume" UI button is clicked.
    public void MenuResumeClick ()
    {
        localNetworkUser.ToggleEscapeMenu();
    }

    // Called when the Escape Menu -> "Options" UI button is clicked.
    public void MenuOptionsClick ()
    {
        Debug.Log("Currently there is no options menu, but if you wish to add one, feel free. - Vaijk");
    }
}