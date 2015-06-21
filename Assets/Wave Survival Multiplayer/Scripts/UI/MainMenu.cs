using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Takes care of all the functionalities of a Main Menu, such as changing settings (Options), or quitting the game, for example.

public class MainMenu : MonoBehaviour
{
    private string playerName = string.Empty;

    private InputField playerNameInput = null;

    private UIManager UIManagerComponent = null;

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start()
    {
        UIManagerComponent = GameObject.FindGameObjectWithTag("UI Manager").GetComponent<UIManager>();

        // Get the Player name input field.
        playerNameInput = UIManagerComponent.GetUIObject(new string[] { "Options Menu", "Nickname Field" }).GetComponent<InputField>();

        // If there is no key in the registry for the player name, then add one with a default value.
        if (!PlayerPrefs.HasKey("PlayerName"))
        {
            PlayerPrefs.SetString("PlayerName", "Player");
        }

        playerNameInput.text = PlayerPrefs.GetString("PlayerName");
    }

    // Called every frame after Start().
    private void Update ()
    {
        playerName = playerNameInput.text;
    }

    #endregion

    #region UI Functions

    // Called when the Main Menu -> "Multiplayer" UI button is clicked.
    public void MultiplayerClick ()
    {
        UIManagerComponent.ChangeUI("Network Menu");
    }

    // Called when the Main Menu -> "Options" UI button is clicked.
    public void OptionsClick ()
    {
        UIManagerComponent.ChangeUI("Options Menu");
    }

    // Called when the Main Menu -> "Exit" UI button is clicked.
    public void ExitClick ()
    {
        Application.Quit();
    }

    // Called when the Options Menu -> "Back" UI button is clicked.
    public void OptionsBackClick ()
    {
        // Save the changed data to the registry.
        PlayerPrefs.SetString("PlayerName", playerName);

        UIManagerComponent.ChangeUI("Main Menu");
    }

    #endregion
}