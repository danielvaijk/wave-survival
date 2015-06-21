using UnityEngine;
using UnityEngine.Networking;

using System;
using System.Collections.Generic;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// 1. Make sure decal prefab GameObjects don't have colliders on them.

public class DecalMaterial : MonoBehaviour
{
    public float penetrability = 0f;

    public float splatterRange = 10f;

    public bool doesSplatter = false;

    public List<Decal> entryDecals;
    public List<Decal> entryParticles;

    public List<Decal> exitDecals;
    public List<Decal> exitParticles;

    public List<Decal> splatterDecals;

    // Represents a decal.
    [Serializable]
    public class Decal
    {
        public GameObject decalObject;
        public float duration;
    }

    // Represents the type of the decal.
    private enum DecalType { Entry, Exit, Splatter }

    // Will calculate the trajectory of a weapon projectile, affecting the total trajectory distance
    // depending on the penetration value of the hit DecalMaterial's on the trajectory.
    // Will also apply damage to Zombies if they are within the trajectory.
    public void CalculatePenetration (Weapon shotWeapon, NetworkInstanceId shooterID)
    {
        float shotDistance = shotWeapon.fireDistance;
        Transform shootPoint = shotWeapon.shootPoint;
        RaycastHit hit = shotWeapon.hit;
        Ray entryRay = new Ray(shootPoint.position, shootPoint.forward);

        // Calculate the entry raycast and get all the hit information.
        RaycastHit[] entryHits = Physics.RaycastAll(entryRay, shotDistance, ~(1 << 8), QueryTriggerInteraction.Ignore);

        // Sort the entry hits from smallest to biggest distance.
        Array.Sort(entryHits, delegate(RaycastHit hitA, RaycastHit hitB)
        {
            return hitA.distance.CompareTo(hitB.distance);
        });

        // Get the end point of the total trajectory, so we can raycast back to the origin.
        Vector3 rayEndPoint = shootPoint.position + (shootPoint.forward * shotDistance);
        float offsetDistance = Vector3.Distance(rayEndPoint, hit.point);
        Ray exitRay = new Ray(rayEndPoint, hit.point - rayEndPoint);

        // Calculate the exit raycast and get all the hit information.
        RaycastHit[] exitHits = Physics.RaycastAll(exitRay, offsetDistance, ~(1 << 8), QueryTriggerInteraction.Ignore);

        // Sort the exit hits from smallest to biggest distance.
        Array.Sort(exitHits, delegate (RaycastHit hitA, RaycastHit hitB)
        {
            return hitA.distance.CompareTo(hitB.distance);
        });

        float finalTravelDistance = shotDistance;

        for (int i = 0; i < entryHits.Length; i++)
        {
            // Calculate the entry hits and decals.

            // If the root object of the collider we hit in the trajectory is a zombie, then apply damage to him.
            if (entryHits[i].transform.root.tag == "Zombie")
            {
                ZombieAI hitZombie = entryHits[i].transform.root.GetComponent<ZombieAI>();

                // If we hit the Zombie's head, subtract all he has left for health from his remainder health.
                float totalDamage = (entryHits[i].collider.name == "Head") ? hitZombie.currentHealth : shotWeapon.shotDamage;

                // Apply the damage to the zombie.
                hitZombie.TakeDamage(totalDamage, shooterID);
            }

            // Get the DecalMaterial from this entry hit object.
            DecalMaterial hitMaterial = entryHits[i].collider.GetComponent<DecalMaterial>();

            // If this entry hit object does not have a DecalMaterial, then return and ignore further calculations for this entry hit.
            if (hitMaterial == null)
            {
                continue;
            }

            // Check if the entry point of the projectile was still inside the current final travel distance
            // after penetration discounts have been made.
            if (entryHits[i].distance <= finalTravelDistance)
            {
                CreateDecal(entryHits[i], hitMaterial, DecalType.Entry);

                // Check if there is a object hit after this entry hit (for splatter calculations).
                if (entryHits.Length - 1 >= i + 1)
                {
                    // Get the entry hit after this one.
                    RaycastHit splatterHit = entryHits[i + 1];

                    // Check if the current hit material splatters and that the next hit object is within splatter range.
                    if (hitMaterial.doesSplatter && (splatterHit.distance <= splatterRange))
                    {
                        CreateDecal(splatterHit, hitMaterial, DecalType.Splatter);
                    }
                }
            }
            else
            {
                break;
            }

            // Affect the final travel distance depending on the hit objects material penetrability.
            finalTravelDistance *= hitMaterial.penetrability;

            // Check if there is a exit hit for the current entry hit.
            if (exitHits.Length - 1 >= i)
            {
                RaycastHit currentExitHit = exitHits[exitHits.Length - 1 - i];

                // Check if the exit decal is within the projectile travel distance.
                if (Vector3.Distance(currentExitHit.point, shootPoint.position) <= finalTravelDistance)
                {
                    CreateDecal(currentExitHit, hitMaterial, DecalType.Exit);
                }
            }
        }

        Debug.DrawRay(shootPoint.position, shootPoint.forward * finalTravelDistance, Color.blue, 2000f);
    }

    // Will create and configure a decal and particle based on given RaycastHit and DecalMaterial information from the hit object.
    private void CreateDecal (RaycastHit raycastHit, DecalMaterial hitMaterial, DecalType decalType)
    {
        // Calculate the position and rotation for this decal based on the RaycastHit.
        Vector3 decalOffset = raycastHit.point + (raycastHit.normal * 0.002f);
        Quaternion decalRotation = Quaternion.FromToRotation(Vector3.up, raycastHit.normal);

        Decal targetDecal = new Decal();
        Decal targetParticle = new Decal();

        int randomIndex = 0;

        switch (decalType)
        {
            case DecalType.Entry:
                if (hitMaterial.entryDecals.Count > 0)
                {
                    randomIndex = UnityEngine.Random.Range(0, hitMaterial.entryDecals.Count);
                    targetDecal = hitMaterial.entryDecals[randomIndex];
                }

                if (hitMaterial.entryParticles.Count > 0)
                {
                    randomIndex = UnityEngine.Random.Range(0, hitMaterial.entryParticles.Count);
                    targetParticle = hitMaterial.entryParticles[randomIndex];
                }
                break;

            case DecalType.Exit:
                if (hitMaterial.exitDecals.Count > 0)
                {
                    randomIndex = UnityEngine.Random.Range(0, hitMaterial.exitDecals.Count);
                    targetDecal = hitMaterial.exitDecals[randomIndex];
                }

                if (hitMaterial.exitParticles.Count > 0)
                {
                    randomIndex = UnityEngine.Random.Range(0, hitMaterial.exitParticles.Count);
                    targetParticle = hitMaterial.exitParticles[randomIndex];
                }
                break;

            case DecalType.Splatter:
                if (hitMaterial.splatterDecals.Count > 0)
                {
                    randomIndex = UnityEngine.Random.Range(0, hitMaterial.splatterDecals.Count);
                    targetDecal = hitMaterial.splatterDecals[randomIndex];
                }
                break;
        }

        // Create the decal.
        if (targetDecal.decalObject != null)
        {
            GameObject decal = Instantiate(targetDecal.decalObject, decalOffset, decalRotation) as GameObject;

            decal.transform.parent = raycastHit.transform;
            decal.name = decalType + " Decal";

            Destroy(decal, targetDecal.duration);
        }

        if (targetParticle.decalObject != null)
        {
            // If there is a particle, then create it and destroy it in a second.
            Destroy(Instantiate(targetParticle.decalObject, decalOffset, decalRotation), targetParticle.duration);
        }
    }
}