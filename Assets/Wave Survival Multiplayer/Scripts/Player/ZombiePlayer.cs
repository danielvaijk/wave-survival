using UnityEngine;
using UnityEngine.Networking;

public class ZombiePlayer : NetworkBehaviour
{
    // Called when the local player object has been set up.
    public override void OnStartLocalPlayer ()
    {
        GetComponentInChildren<Camera>().enabled = true;
        GetComponentInChildren<AudioListener>().enabled = true;
    }
}