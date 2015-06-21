using UnityEngine;
using UnityEngine.Networking;

using System.Collections.Generic;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Manages the Players weapons and utilities, note that this system is very primitive.
// I have a more advanced inventory asset, check it out on my website!

public class PlayerInventory : NetworkBehaviour
{
    public Transform inventoryParent;

    [HideInInspector]
    public GameObject equipedWeapon;

    [HideInInspector]
    public List<GameObject> inventory = new List<GameObject>() { null, null, null };

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        // Set the current equiped weapon as the Player's empty hands.
        equipedWeapon = transform.Find("Player Body").Find("Torso").Find("Empty Hands").gameObject;

        // Only the owner of this Player instance can manage the inventory.
        enabled = isLocalPlayer;
    }

    // Called every frame after Start().
    private void Update ()
    {
        if (Input.GetButtonDown("Primary") && inventory[0] != null)
        {
            CmdChangeItem(0);
        }

        if (Input.GetButtonDown("Secondary") && inventory[1] != null)
        {
            CmdChangeItem(1);
        }

        if (Input.GetButtonDown("Utility") && inventory[2] != null)
        {
            CmdChangeItem(2);
        }
    }

    #endregion

    #region UNET Message Functions

    // Called on the Server. Used to callback a RPC to all the Clients for the same function.
    [Command(channel = 1)]
    public void CmdAddItem (string itemName, int itemType)
    {
        RpcAddItem(itemName, itemType);
    }

    // Called on the Clients. Will add a new item to this Player's inventory.
    [ClientRpc(channel = 1)]
    private void RpcAddItem(string itemName, int itemType)
    {
        GameObject newItem = inventoryParent.Find(itemName + " (Player)").gameObject;

        // Check if there already is an item occupying this items slot.
        if (inventory[itemType] == newItem)
        {
            Weapon itemWeapon = newItem.GetComponentInChildren<Weapon>();

            if (itemWeapon.fireType == Weapon.FireType.Throwable)
            {
                itemWeapon.loadedAmmo = itemWeapon.maxAmmoPerLoad;
            }
            else
            {
                itemWeapon.storedAmmo = itemWeapon.maxAmmoStorage;
            }

            return;
        }

        // Desactivate the current weapon.
        equipedWeapon.SetActive(false);

        // Equip the newly added weapon.
        equipedWeapon = newItem;
        equipedWeapon.SetActive(true);

        // Add the new item to the inventory.
        inventory[itemType] = newItem;
    }

    // Called on the Server. Used to callback a RPC to all the Clients for the same function.
    [Command(channel = 1)]
    private void CmdChangeItem (int itemIndex)
    {
        RpcChangeItem(itemIndex);
    }

    // Called on the Clients. Will change this Player instances item on all Clients.
    [ClientRpc(channel = 1)]
    private void RpcChangeItem (int itemIndex)
    {
        GameObject targetItem = inventory[itemIndex];

        if (equipedWeapon != targetItem)
        {
            // Unequip the current equiped weapon.
            equipedWeapon.SetActive(false);

            // Equip the <targetItem>.
            equipedWeapon = targetItem;
            equipedWeapon.SetActive(true);
        }
    }

    #endregion
}