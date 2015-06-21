using UnityEngine;
using UnityEngine.Networking;

using System.Collections.Generic;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents the throwable object of a throwable weapon.

// 1. <damage> for Molotov type throwables is damage per second (DPS).

public class Throwable : MonoBehaviour
{
    public ThrowableType throwableType = ThrowableType.Grenade;

    public float damage = 20f;
    public float timer = 2f;
    public float blastRadius = 15f;

    public bool showBlastRadius = false;

    public GameObject explosionParticle;
    public GameObject fireParticle;

    public AudioClip explosionSound;
    public AudioClip collisionSound;

    private float counter = 0f;

    private bool hasExploded = false;
    private bool firstCollision = true;

    [HideInInspector]
    public NetworkInstanceId shooterID;

    private List<Transform> hitZombies = new List<Transform>();

    // The types of throwable weapons available.
    public enum ThrowableType { Grenade, Molotov }

    #region Unity Functions

    // Draws gizmos in the Scene View.
    private void OnDrawGizmos ()
    {
        if (showBlastRadius)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }

    // Called every frame after Start().
    private void Update ()
    {
        if (throwableType == ThrowableType.Grenade)
        {
            if (counter >= timer)
            {
                if (explosionSound != null)
                {
                    // Play the explosion sound at the point where the grenade exploded.
                    AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
                }

                if (explosionParticle != null)
                {
                    // Create the explosion particle for the grenade explosion.
                    Instantiate(explosionParticle, transform.position, Quaternion.identity);
                }

                // Apply damage to any zombies inside the SphereCast.
                OverlapSphereDamage();
            }
            else
            {
                counter += Time.deltaTime;
            }
        }
        else if ((throwableType == ThrowableType.Molotov) && hasExploded)
        {
            // Apply damage to any zombies inside the SphereCast every frame.
            OverlapSphereDamage();
        }
    }

    // Called when this Collider/Rigidbody starts touching another Collider/Rigidbody.
    private void OnCollisionEnter (Collision collision)
    {
        if (throwableType == ThrowableType.Molotov)
        {
            // Hitting any Player body parts does not count as a explosion hit.
            if (collision.transform.root.tag != "Player")
            {
                if (firstCollision)
                {
                    // If this is this molotov's first collision, play the collision sound.
                    AudioSource.PlayClipAtPoint(collisionSound, transform.position, 0.5f);
                    firstCollision = false;
                }

                if (explosionParticle != null)
                {
                    // Create the fire from the molotov explosion.
                    Instantiate(explosionParticle, transform.position, Quaternion.identity);
                }

                hasExploded = true;
            }
        }
    }

    #endregion

    #region Custom Utility Functions

    // Will apply damage to any zombies within the Overlap Sphere.
    private void OverlapSphereDamage ()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);

        foreach (Collider collider in hitColliders)
        {
            Transform hitTransform = collider.transform;

            if (hitTransform.root.tag == "Zombie")
            {
                // If we have already iterated through a Zombie containing this collider, then skip to next.
                if (hitZombies.Contains(hitTransform.root))
                {
                    continue;
                }

                // Add the new zombie to the <hitZombies> so that he don't get iterated more than once.
                hitZombies.Add(collider.transform.root);

                ZombieAI targetZombie = collider.transform.root.GetComponent<ZombieAI>();

                if (throwableType == ThrowableType.Molotov)
                {
                    foreach (Transform child in targetZombie.transform)
                    {
                        // If the zombie is already on fire, there is not need to set him on fire.
                        if (child.GetComponent<FireDamage>() != null)
                        {
                            continue;
                        }
                    }

                    // Create a fire particle (does apply damage by itself).
                    GameObject fireClone = Instantiate(fireParticle, collider.transform.position, Quaternion.identity) as GameObject;

                    fireClone.name = "Flame";
                    fireClone.transform.SetParent(targetZombie.transform, true);
                    fireClone.transform.localPosition = Vector3.zero;

                    // Setup the fire particle FireDamage component.
                    FireDamage fireDamage = fireClone.GetComponent<FireDamage>();

                    fireDamage.shooterID = shooterID;
                    fireDamage.targetZombie = targetZombie;
                }
                else
                {
                    // Set the grenade explosion to this one (for ExplosionForce physics) and apply damage to the zombie.
                    targetZombie.grenadeExplosion = this;
                    targetZombie.TakeDamage(damage, shooterID);
                }
            }
            else if (throwableType == ThrowableType.Grenade)
            {
                Rigidbody targetRigidbody = hitTransform.GetComponent<Rigidbody>();

                if (targetRigidbody != null)
                {
                    targetRigidbody.AddExplosionForce(damage * 10f, transform.position, blastRadius, 2f);
                }
            }
        }

        if (throwableType == ThrowableType.Grenade)
        {
            Destroy(gameObject);
        }
    }

    #endregion
}