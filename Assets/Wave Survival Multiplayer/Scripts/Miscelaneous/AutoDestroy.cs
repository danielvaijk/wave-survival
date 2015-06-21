using UnityEngine;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Will destroy this GameObject after a certain amount of time.

public class AutoDestroy : MonoBehaviour
{
    public float delay = 0f;

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        Destroy(gameObject, delay);
    }
}