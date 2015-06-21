using UnityEngine;
using UnityEngine.Networking;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Handles Networking for child components in the Player, since there can only be 1 NetworkIdentity component
// per GameObject root and also authority function handling.

public class NetworkCallback : NetworkBehaviour
{
    // Called on the Player instance on the Server. Damages the Player instance on the Server, 
    // thus synchronizing the health over the Network via SyncVar.
    [Command(channel = 3)]
    public void CmdTakeDamage (float amount)
    {
        GetComponent<NetworkUser>().currentHealth -= amount;
    }

    // Called on the Player instance on the Server. Used as a callback function to call this
    // function on the Clients (including the Host-Client).
    [Command(channel = 4)]
    public void CmdFire (NetworkInstanceId shooterID, int weaponIndex)
    {
        RpcFire(shooterID, weaponIndex);
    }

    // Called on the Player instance on the Clients. Will fire the weapon over the Network, depending
    // if it's a Throwable weapon or not.
    [ClientRpc(channel = 4)]
    private void RpcFire (NetworkInstanceId shooterID, int weaponIndex)
    {
        PlayerInventory playerInventory = GetComponent<PlayerInventory>();
        Weapon targetWeapon = playerInventory.inventoryParent.GetChild(weaponIndex).GetComponentInChildren<Weapon>();

        if (targetWeapon.fireType == Weapon.FireType.Throwable)
        {
            targetWeapon.Throw(shooterID);
        }
        else
        {
            targetWeapon.Shoot(shooterID);
        }
    }

    // Called on the Weapon instance on the Server. Used as a callback function to call this
    // function on the Clients (including the Host-Client).
    [Command(channel = 1)]
    public void CmdReload (int weaponIndex, string reloadAnimationTrigger)
    {
        RpcReload(weaponIndex, reloadAnimationTrigger);
    }

    // Called on the Weapon instance on the Clients. Will trigger a reload for the weapon with <weaponIndex> over the Network.
    [ClientRpc(channel = 1)]
    private void RpcReload (int weaponIndex, string reloadAnimationTrigger)
    {
        PlayerInventory playerInventory = GetComponent<PlayerInventory>();
        Weapon targetWeapon = playerInventory.inventoryParent.GetChild(weaponIndex).GetComponentInChildren<Weapon>();

        // If there is a reloadSound, play it.
        if (targetWeapon.reloadSound != null)
        {
            targetWeapon.GetComponent<AudioSource>().PlayOneShot(targetWeapon.reloadSound);
        }

        // Trigger the reload animation over the Network.
        GetComponent<Animator>().SetTrigger(reloadAnimationTrigger);
    }

    // Called on the Server. Repair a plank on the <barricade>.
    [Command(channel = 1)]
    public void CmdRepairPlank (GameObject barricade)
    {
        barricade.GetComponent<Barricade>().RpcRepairPlank();
    }

    // Called on the Server. Play the repair sound on the <barricade>.
    [Command(channel = 1)]
    public void CmdPlayRepairSound (GameObject barricade)
    {
        barricade.GetComponent<Barricade>().RpcPlayRepairSound();
    }

    // Called on the Server. Stop playing the repair sound on the <barricade>.
    [Command(channel = 1)]
    public void CmdStopRepairSound (GameObject barricade)
    {
        barricade.GetComponent<Barricade>().RpcStopRepairSound();
    }
}