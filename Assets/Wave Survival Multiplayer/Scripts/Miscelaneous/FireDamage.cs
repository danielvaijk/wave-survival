using UnityEngine;
using UnityEngine.Networking;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents a fire instance that will damage its target (Zombie) per second (DPS) for a certain duration.

public class FireDamage : MonoBehaviour
{
    public float damagePerSecond = 5f;
    public float fireDuration = 10f;

    [HideInInspector]
    public NetworkInstanceId shooterID;

    [HideInInspector]
    public ZombieAI targetZombie;

    private float dpsCounter = 0f;

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        Destroy(gameObject, fireDuration);
    }

    // Called every frame after Start().
    private void Update ()
    {
        // If our <targetZombie> is not null, damage him per second.
        if (targetZombie != null)
        {
            if (dpsCounter >= 1f)
            {
                targetZombie.TakeDamage(damagePerSecond, shooterID);
                dpsCounter = 0f;
            }
            else
            {
                dpsCounter += Time.deltaTime;
            }
        }
    }
}