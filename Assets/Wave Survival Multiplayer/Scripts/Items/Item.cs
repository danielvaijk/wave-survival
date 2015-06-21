using UnityEngine;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents a purchasable item that the Player can buy, equip and use.

// 1. The <itemName> must be the name of the item itself, since the PlayerInventory will base off it (e.g <itemName> (Player)).

public class Item : MonoBehaviour
{
    public ItemType itemType = ItemType.Primary;

    public int itemPrice = 0;

    public string itemName = "Item";

    private bool beingInteracted = false;

    private NetworkUser localPlayer = null;
    private PlayerInventory playerInventory = null;

    // The types of item that are available.
    public enum ItemType : int
    { 
        Primary = 0,
        Secondary = 1,
        Utility = 2
    };

    // Called every frame after Start().
    private void Update ()
    {
        if (localPlayer == null)
        {
            // Look for our local Player.
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                NetworkUser networkUser = player.GetComponent<NetworkUser>();

                if (networkUser.isLocalPlayer)
                {
                    localPlayer = networkUser;
                    playerInventory = localPlayer.GetComponent<PlayerInventory>();

                    return;
                }
            }
        }
        else
        {
            beingInteracted = (localPlayer.currentTarget == gameObject);
        }

        // If we press the interact button and we were interacting with this item, attempt to buy it.
        if (Input.GetButtonDown("Interact") && beingInteracted)
        {
            if (localPlayer.currency >= itemPrice)
            {
                // Charge the player and equip the new item over the Network.
                localPlayer.CmdUpdateCurrency(itemPrice, true, true);
                playerInventory.CmdAddItem(itemName, (int)itemType);
            }
        }
    }
}