using UnityEngine;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Used to show a information on the object that the Player is currently 'interacting' with.

public class Interactable : MonoBehaviour
{
    public InteractStatus interactStatus;

    // Different types of interactable types and their status.
    public enum InteractStatus { StaticBuy, StaticRepair, NonStatic }

    // Returns this interactable items interact text.
    public string GetInteractText ()
    {
        switch (interactStatus)
        {
            case InteractStatus.StaticBuy:
                Item thisItem = GetComponent<Item>();

                return string.Format("Press <color=yellow>E</color> to buy {0} [{1}]", thisItem.itemName, thisItem.itemPrice);

            case InteractStatus.StaticRepair:
                return string.Format("Hold <color=yellow>E</color> to repair the Barricade");

            default:
                return string.Empty;
        }
    }
}