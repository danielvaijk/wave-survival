using UnityEngine;
using UnityEngine.Networking;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Takes care of synchorizing this Transforms position and rotation over the Network. (Smooth alternative to NetworkTransform).

public class NetworkSync : NetworkBehaviour
{
    public float syncSmooth = 15f;

    [SyncVar]
    private Vector3 syncPosition = Vector3.zero;

    [SyncVar]
    private Quaternion syncRotation = Quaternion.identity;

    [SyncVar]
    private Quaternion syncCameraRotation = Quaternion.identity;

    private Transform playerCamera = null;

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        // Initialize this GameObjects position and rotation, so we don't try and Lerp to (0,0,0) (0,0,0,0).
        syncPosition = transform.position;
        syncRotation = transform.rotation;

        playerCamera = transform.Find("Player Camera");
    }

    // Called every frame after Start().
    private void Update ()
    {
        if (playerCamera != null)
        {
            if (isLocalPlayer)
            {
                // If we are the local Player, then send our information over the Network.
                CmdUpdateSyncData(transform.position, transform.rotation, playerCamera.rotation);
            }
            else
            {
                // If this is a clone of the original instance, then receive information from the original instance.
                transform.position = Vector3.Lerp(transform.position, syncPosition, syncSmooth * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, syncRotation, syncSmooth * Time.deltaTime);

                playerCamera.rotation = Quaternion.Lerp(playerCamera.rotation, syncCameraRotation, syncSmooth * Time.deltaTime);
            }
        }
        else
        {
            if (isServer)
            {
                // If this object does not have a PlayerCamera, then it is a Zombie, which is Server managed.
                // If wea re the local Player, then send our information over the Network.
                CmdUpdateSyncData(transform.position, transform.rotation, Quaternion.identity);
            }
            else
            {
                // If this is a clone of the original instance, then receive information from the original instance.
                transform.position = Vector3.Lerp(transform.position, syncPosition, syncSmooth * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, syncRotation, syncSmooth * Time.deltaTime);
            }
        }
    }

    // Called on the Server. Updates the syncVars on the Server with the Client (sender)'s information.
    [Command(channel = 3)]
    private void CmdUpdateSyncData (Vector3 updatedPosition, Quaternion updatedRotation, Quaternion updatedCameraRotation)
    {
        syncPosition = updatedPosition;
        syncRotation = updatedRotation;

        if (playerCamera != null)
        {
            syncCameraRotation = updatedCameraRotation;
        }
    }
}