using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(UnityEngine.AI.OffMeshLink))]
[RequireComponent(typeof(Interactable))]

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents a barricade that is the Zombies main obstacle, before he can reach any of the Players.

// 1. Keep the main collider infront of the barricade object, so that the ray 
//    from the PlayerInteraction will hit the main objects collider.
// 2. Check the order of the planks to start at the outmost one all the way to the innermost one.

public class Barricade : NetworkBehaviour
{
    public int repairReward = 50;

    public float repairRate = 2f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float rubbleDestroyTime = 10f;

    public bool allowBarricadePhysics = true;

    public GameObject[] plankObjects;

    public AudioClip repairSound;
    public AudioClip repairCompleteSound;

    public AudioClip[] damageSounds;
    public AudioClip[] destroySounds;

    private int destructionCounter = -1;

    private float destructionDamage = 0;
    private float acumulatedDamage = 0;
    private float repairPercentage = 0;
    private float repairTimer = 0f;
    private float soundTimer = 0f;

    private bool isRepairing = false;
    private bool beingInteracted = false;

    private Transform alphaClone = null;
    private Transform infoUI = null;

    private AudioSource thisAudioSource = null;
    private MeshRenderer alphaCloneRenderer = null;

    private Color plankColor = new Color();

    private NetworkUser localPlayer = null;

    #region Unity Functions

    // Called on the editor when a variable's value has changed.
    private void OnValidate ()
    {
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        if (maxHealth < 0f)
        {
            maxHealth = 0f;
        }

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }
    }

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        thisAudioSource = GetComponent<AudioSource>();

        soundTimer = repairSound.length;

        infoUI = GameObject.FindGameObjectWithTag("UI Manager").transform.Find("Info");

        // Calculate the amount of damage to destroy each plank of this barricade.
        destructionDamage = maxHealth / plankObjects.Length;

        int destroyedPlankNumber = Mathf.CeilToInt((maxHealth - currentHealth) / destructionDamage);

        // Destroy planks based on this barricades starting <currentHealth> amount.
        for (int i = 0; i < destroyedPlankNumber; i++)
        {
            DestroyPlank(plankObjects[i].transform);
        }
    }

    // Called every frame after Start().
    private void Update ()
    {
        GetComponent<UnityEngine.AI.OffMeshLink>().activated = (currentHealth == 0f);

        if (localPlayer == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            // Find our Player GameObject inside the Scene and retrieve its data.
            foreach (GameObject player in players)
            {
                NetworkUser networkUser = player.GetComponent<NetworkUser>();

                if (networkUser.isLocalPlayer)
                {
                    localPlayer = networkUser;
                    break;
                }
            }
        }
        else
        {
            // Check if the current interaction target from the player is us.
            beingInteracted = (localPlayer.currentTarget == gameObject);
        }

        // If we are no longer repairing this barricade, reset the repair timer.
        if (isRepairing)
        {
            infoUI.Find("Barricade Text").GetComponent<Text>().text = string.Format("Repairing... {0}%", Mathf.Floor(repairPercentage));
            GetComponent<Interactable>().interactStatus = Interactable.InteractStatus.NonStatic;
        }

        if (beingInteracted)
        {
            // If this barricade is being interacted and there is planks to repair.
            if (Input.GetButton("Interact") && destructionCounter > -1)
            {
                isRepairing = true;

                repairTimer += Time.deltaTime;

                repairPercentage = ((repairTimer / repairRate) * 100f);

                if (alphaCloneRenderer != null)
                {
                    plankColor.a = Mathf.Lerp(0f, 1f, repairPercentage / 100f);

                    alphaCloneRenderer.material.color = plankColor;
                }

                // Prevents stacking sounds ontop of each other.
                if (soundTimer >= repairSound.length)
                {
                    // Play the repair sound over the Network.
                    localPlayer.GetComponent<NetworkCallback>().CmdPlayRepairSound(gameObject);

                    soundTimer = 0f;
                }
                else
                {
                    soundTimer += Time.deltaTime;
                }

                if (repairTimer >= repairRate)
                {
                    // Repair the destroyed plank over the Network.
                    localPlayer.GetComponent<NetworkCallback>().CmdRepairPlank(gameObject);

                    // Give the Player who repaired the Barricade the <repairReward>.
                    localPlayer.CmdUpdateCurrency(repairReward, false, true);

                    repairTimer = 0f;
                }
            }
            else if (isRepairing)
            {
                StopRepair();
            }
        }
        else if (isRepairing)
        {
            StopRepair();
        }
    }

    #endregion

    #region UNET Message Functions

    // Called on the Server. Will choose random damage/destroy sounds and callback the function on the Clients.
    [Command(channel = 2)]
    public void CmdDamageBarricade(float damage)
    {
        int randomDamageSound = Random.Range(0, damageSounds.Length);
        int randomDestroySound = Random.Range(0, destroySounds.Length);

        RpcDamageBarricade(damage, randomDamageSound, randomDestroySound);
    }

    // Handles barricade destruction based on the <damage> amount and given sound indexes from the Server.
    [ClientRpc(channel = 2)]
    private void RpcDamageBarricade (float damage, int randomDamageSound, int randomDestroySound)
    {
        // Apply and acumulate damage to this barricade.
        currentHealth -= damage;
        acumulatedDamage += damage;

        // Plays a random damage sound from <damageSounds> over the Network.
        thisAudioSource.PlayOneShot(damageSounds[randomDamageSound]);

        // Check if the amount of acumulated damage is enough to destroy a plank from the barricade.
        if (acumulatedDamage >= destructionDamage && destructionCounter < plankObjects.Length - 1)
        {
            for (float i = 0; i < acumulatedDamage; i += destructionDamage)
            {
                thisAudioSource.PlayOneShot(destroySounds[randomDestroySound]);

                destructionCounter++;

                Transform destroyedPlank = GetDestructionPlank();

                if (allowBarricadePhysics)
                {
                    GameObject clone = Instantiate(destroyedPlank.gameObject, destroyedPlank.position, destroyedPlank.rotation) as GameObject;

                    // Make sure this plank clone has its alpha set to 1. (due to a Unity bug we have to hard code this)
                    MeshRenderer cloneRenderer = clone.GetComponent<MeshRenderer>();
                    Color cloneColor = cloneRenderer.material.color;

                    cloneColor.a = 1f;

                    cloneRenderer.material.color = cloneColor;

                    Rigidbody cloneRigidbody = clone.AddComponent<Rigidbody>();

                    cloneRigidbody.AddForce(-transform.forward * 50, ForceMode.Force);
                    cloneRigidbody.AddTorque(new Vector3(50, 100, 1) * 200, ForceMode.Force);

                    // Destroy the plank rubble after <rubbleDestroyTime>.
                    Destroy(clone, rubbleDestroyTime);
                }

                // Destroy all the children within the destroyed decal (mostly for decals).
                foreach (Transform child in destroyedPlank)
                {
                    Destroy(child.gameObject);
                }

                // Destroy the plank.
                DestroyPlank(destroyedPlank);

                // Deduct from the total acumulated damage from this planks destruction.
                acumulatedDamage -= destructionDamage;
            }
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            acumulatedDamage = 0f;
        }
    }

    // Called on the Clients. Repairs a plank of the barricade over the Network.
    [ClientRpc(channel = 2)]
    public void RpcRepairPlank ()
    {
        GameObject fixedPlank = GetDestructionPlank().gameObject;

        plankColor.a = 1f;

        fixedPlank.GetComponent<BoxCollider>().isTrigger = false;
        fixedPlank.GetComponent<MeshRenderer>().material.color = plankColor;

        currentHealth += destructionDamage;
        destructionCounter--;

        // Get the next plank that we are going to fade-in on repair.
        if (destructionCounter > -1)
        {
            alphaClone = GetDestructionPlank();
            alphaCloneRenderer = alphaClone.GetComponent<MeshRenderer>();

            plankColor = alphaCloneRenderer.material.color;
            plankColor.a = 0f;
        }
        else
        {
            alphaClone = null;
            alphaCloneRenderer = null;

            plankColor = new Color();
        }

        if (repairCompleteSound != null)
        {
            thisAudioSource.PlayOneShot(repairCompleteSound);
        }
    }

    // Called on the Clients. Plays the repair sound once over the Network.
    [ClientRpc(channel = 1)]
    public void RpcPlayRepairSound ()
    {
        thisAudioSource.PlayOneShot(repairSound);
    }

    // Called on the Clients. Stops the repair sound from playing over the Network.
    [ClientRpc(channel = 1)]
    public void RpcStopRepairSound ()
    {
        thisAudioSource.Stop();
    }

    #endregion

    #region Custom Utility Functions

    // Stops the repair process on this barricade.
    private void StopRepair ()
    {
        repairTimer = 0f;
        soundTimer = repairSound.length;

        GetComponent<Interactable>().interactStatus = Interactable.InteractStatus.StaticRepair;
        infoUI.Find("Barricade Text").GetComponent<Text>().text = string.Empty;

        if (alphaClone != null)
        {
            plankColor.a = 0f;

            alphaCloneRenderer.material.color = plankColor;
        }

        // If the repair sound is still player, stop it over the Network.
        if (thisAudioSource.isPlaying)
        {
            localPlayer.GetComponent<NetworkCallback>().CmdStopRepairSound(gameObject);
        }

        isRepairing = false;
    }

    private void DestroyPlank (Transform targetPlank)
    {
        alphaClone = targetPlank;
        alphaCloneRenderer = alphaClone.GetComponent<MeshRenderer>();

        plankColor = alphaCloneRenderer.material.color;
        plankColor.a = 0f;

        alphaCloneRenderer.material.color = plankColor;

        alphaClone.GetComponent<BoxCollider>().isTrigger = true;
    }

    private Transform GetDestructionPlank ()
    {
        return plankObjects[destructionCounter].transform;
    }

    #endregion
}