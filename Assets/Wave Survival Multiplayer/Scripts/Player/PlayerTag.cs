using UnityEngine;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Displays text with this Player's name ontop of this Player.

public class PlayerTag : MonoBehaviour
{
    private Transform target = null;

    private TextMesh textMesh = null;

    private bool isLocal = false;

    // Called when the script instance is being loaded.
    private void Awake ()
    {
        textMesh = GetComponent<TextMesh>();
    }

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        isLocal = GetComponentInParent<NetworkUser>().isLocalPlayer;
    }

    // Called every frame after Start().
    private void Update ()
    {
        if (target != null)
        {
            // Rotate towards the local Player instance of this Client.
            transform.rotation = Quaternion.LookRotation(transform.position - target.position);
        }
        else if (!isLocal)
        {
            // Makes all Player GameObjects (That are not mine over the Network) set their
            // <target> to the Player GameObject I own.
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                NetworkUser networkUser = player.GetComponent<NetworkUser>();

                if (networkUser.isLocalPlayer)
                {
                    target = player.transform;
                    return;
                }
            }
        }
    }

    // Sets the this Player tag's display name (text).
    public void SetPlayerName (string playerName)
    {
        textMesh.text = playerName;
    }
}