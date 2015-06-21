using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(NetworkIdentity))]

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Manages a Player instance in the Network for things like health, currency, kills, Network Synchronization, and etc.

// 1. Make sure the Players <deadBody> does not have the tag 'Player' on it.

public class NetworkUser : NetworkBehaviour
{
    [SyncVar]
    public int currency = 400;

    [SyncVar]
    public int kills = 0;

    [SyncVar]
    public float currentHealth = 100f;

    [SyncVar]
    [HideInInspector]
    public string playerName = "";

    public float maxHealth = 100f;
    public float healSpeed = 3f;
    public float interactDistance = 2f;
    public float idleTime = 5f;

    public GameObject playerZombie;
    public GameObject currencyText;

    public AudioClip playerBreathingSound;
    public AudioClip currencyRewardSound;

    public AudioClip[] playerDamageSounds;

    [HideInInspector]
    public bool showEscapeMenu = false;

    [HideInInspector]
    public GameObject currentTarget;

    private Transform localPlayerEntry = null;
    private Transform rewardText = null;
    private Transform interactText = null;

    private AudioSource thisAudioSource = null;

    private NetworkFramework networkFramework = null;
    private UIManager UIManagerComponent = null;

    #region Unity Functions

    // Called when the script instance is being loaded.
    private void Awake ()
    {
        networkFramework = NetworkFramework.singleton as NetworkFramework;
    }

    // Called on the frame this script is enabled, before Update().
    public void Start ()
    {
        // Set the name of this GameObject as that of the Player's name.
        name = playerName;

        // Set this Player instance's Player Tag to its Player name.
        GetComponentInChildren<PlayerTag>().SetPlayerName(playerName);

        // Create a Player score entry.
        GameObject playerScoreEntry = Instantiate(networkFramework.playerScoreEntry) as GameObject;

        // Parent the score entry to the score list.
        playerScoreEntry.transform.SetParent(networkFramework.playerScoreList.transform, false);

        // Set the name for the Player score entry.
        localPlayerEntry = playerScoreEntry.transform;
        localPlayerEntry.Find("Name").GetComponent<Text>().text = playerName;
    }

    // Called every frame after Start().
    private void Update()
    {
        if (localPlayerEntry != null)
        {
            // Updated the <kills> and <currency> on the scoreboard for this Players instance.
            localPlayerEntry.Find("Kills").GetComponent<Text>().text = kills.ToString();
            localPlayerEntry.Find("Currency").GetComponent<Text>().text = currency + "C";
        }

        // If this is not the actual local Player, then return.
        if (!isLocalPlayer)
        {
            return;
        }

        // If we hit the 'Escape' key, then activate the Escape menu.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEscapeMenu();
        }

        if (!showEscapeMenu)
        {
            // Handle Player interaction.

            Transform playerCamera = transform.Find("Player Camera");

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            RaycastHit hit;

            Text interactTextComponent = interactText.GetComponent<Text>();

            // If we hit a collider, then set our <currentTarget> to it.
            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                currentTarget = hit.transform.gameObject;

                Interactable item = currentTarget.GetComponent<Interactable>();

                if (item != null)
                {
                    interactTextComponent.text = item.GetInteractText();
                }
                else if (interactTextComponent.text.Length > 0)
                {
                    // If the raycast is hitting a collider that is not a interactable item, and
                    // the interact text is still visible, then erase it.
                    interactTextComponent.text = string.Empty;
                }
            }
            else
            {
                // If the raycast is not hittin any colliders, then reset our interact variables.
                interactTextComponent.text = string.Empty;
                currentTarget = null;
            }

            // Toggle the scoreboard if the 'Tab' key is pressed.
            UIManagerComponent.GetUIObject(new string[] { "Scoreboard" }).SetActive(Input.GetKey(KeyCode.Tab));
        }

        // If this Players health is not full the add the damage UI image and regenerate based on the <healSpeed>.
        if (currentHealth < maxHealth)
        {
            Transform damageUI = networkFramework.UIManagerComponent.transform.Find("Player HUD").Find("Damage");
            Image damageImage = damageUI.GetComponent<Image>();
            Color newDamageColor = damageImage.color;

            newDamageColor.a = 1 - (currentHealth / maxHealth);
            damageImage.color = newDamageColor;

            thisAudioSource.clip = playerBreathingSound;
            thisAudioSource.volume = 0.5f - (currentHealth / maxHealth);
            thisAudioSource.loop = true;

            if (!thisAudioSource.isPlaying)
            {
                thisAudioSource.Play();
            }

            // Heal this Players health over the Network.
            CmdHealPlayer(healSpeed * Time.deltaTime);
        }
        else if (thisAudioSource.isPlaying)
        {
            thisAudioSource.clip = null;
            thisAudioSource.volume = 1;
            thisAudioSource.loop = false;

            thisAudioSource.Stop();
        }
    }

    #endregion

    #region UNET Override Functions

    // Called when the local player object has been set up.
    public override void OnStartLocalPlayer ()
    {
        UIManagerComponent = networkFramework.UIManagerComponent;
        thisAudioSource = GetComponent<AudioSource>();

        // Disable the effects of physics on this Player GameObject.
        GetComponent<Rigidbody>().isKinematic = false;

        // Activate MonoBehaviours in the parent GameObject.
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            behaviour.enabled = true;
        }

        // Activate the Components in the Camera GameObject.
        GetComponentInChildren<Camera>().enabled = true;
        GetComponentInChildren<AudioListener>().enabled = true;
        transform.Find("Player Camera").GetComponent<MouseRotation>().enabled = true;

        // Activate the Components of the Weapons in the <inventoryParent>.
        foreach (Transform weapon in GetComponent<PlayerInventory>().inventoryParent)
        {
            Weapon weaponComponent = weapon.GetComponentInChildren<Weapon>();

            weaponComponent.enabled = true;
            weaponComponent.localNetworkUser = this;
            weaponComponent.playerInventory = GetComponent<PlayerInventory>();
        }

        // Setup the local UIManager and change to the Player HUD.
        UIManagerComponent.ChangeUI("Player HUD");
        UIManagerComponent.localNetworkUser = this;

        rewardText = UIManagerComponent.GetUIObject(new string[] { "Info" }).transform;
        interactText = UIManagerComponent.GetUIObject(new string[] { "Info", "Interact Text" }).transform;

        // Confine and remove the mouse cursor from the screen.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region UNET Message Functions

    // Called on the Server. Will update the <currentHealth> syncVar based on the new value.
    [Command(channel = 1)]
    private void CmdHealPlayer (float amount)
    {
        // Synchronized on Clients via syncVar attribute.
        currentHealth += amount;
    }

    // Called on the Server. Will update the players <currentHealth> and play a random damage sound over the Network.
    [Command(channel = 1)]
    public void CmdDamagePlayer (float amount)
    {
        // Synchronized on the Clients via syncVar attribute.
        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            // Request that the Server spawn my Zombie Player.
            CmdSpawnZombiePlayer();
        }
        else
        {
            RpcDamagePlayer(Random.Range(0, playerDamageSounds.Length));
        }
    }

    // Called on the Clients. Will play a the designated random damage sound given from the Server.
    // This is a small workaround to AudioSource.PlayClipAtPoint().
    [ClientRpc(channel = 1)]
    private void RpcDamagePlayer (int damageSoundIndex)
    {
        GameObject playOneShot = new GameObject("Player Damage Sound");

        // Configure the empty GameObject to be parented to this Player and be in its center.
        playOneShot.transform.SetParent(transform, true);
        playOneShot.transform.localPosition = Vector3.zero;

        AudioSource audioSource = playOneShot.AddComponent<AudioSource>();
        AudioClip damageSound = playerDamageSounds[damageSoundIndex];

        // Configure the AudioSource and play the AudioClip once.
        audioSource.volume = 0.5f;
        audioSource.PlayOneShot(damageSound);

        // Destroy the Audio GameObject once it's done playing the AudioClip.
        Destroy(playOneShot, damageSound.length);
    }

    // Called on the Server. Will update this Players <currency> over the Network.
    [Command(channel = 1)]
    public void CmdUpdateCurrency (int amount, bool isCharge, bool playSound)
    {
        currency += (isCharge ? -amount : amount);

        RpcShowCurrencyText(amount, isCharge, playSound);
    }

    // Called on the Clients. The local Player will show the currency update UI on his screen.
    [ClientRpc(channel = 1)]
    private void RpcShowCurrencyText (int amount, bool isCharge, bool playSound)
    {
        if (isLocalPlayer)
        {
            GameObject textClone = Instantiate(currencyText);

            textClone.transform.SetParent(rewardText);
            textClone.transform.localPosition = Vector3.zero;
            textClone.name = "Currency Text";

            textClone.GetComponent<TextFade>().text = (isCharge ? "-" : "+") + amount;

            if (currencyRewardSound != null && playSound)
            {
                AudioSource.PlayClipAtPoint(currencyRewardSound, transform.position, 0.25f);
            }
        }
    }

    // Called on the Server. Will update this Players <kills> over the Network.
    [Command(channel = 1)]
    public void CmdUpdateKills()
    {
        kills++;
    }

    // Called on the Server. Will replace and spawn the Zombie Player for this Player.
    [Command(channel = 4)]
    private void CmdSpawnZombiePlayer ()
    {
        // Create the Zombie Player on the Server.
        GameObject zombiePlayer = Instantiate(playerZombie, transform.position, transform.rotation) as GameObject;

        ZombieAI cloneZombie = zombiePlayer.GetComponent<ZombieAI>();

        cloneZombie.isInside = true;
        cloneZombie.name = name + " (Zombie)";

        // Casdasdsdreate the Zombie Player on the Clients.
        NetworkServer.ReplacePlayerForConnection(connectionToClient, zombiePlayer, 0);
        NetworkServer.Destroy(gameObject);
    }

    #endregion

    #region Custom Utility Functions

    // Will remove this Player from the idle state.
    public void BreakIdle ()
    {
        GetComponent<Animator>().SetBool("Idle", false);
    }

    // Toggles on/off the escape menu and its settings.
    public void ToggleEscapeMenu ()
    {
        showEscapeMenu = !Cursor.visible;

        Cursor.lockState = Cursor.visible ? CursorLockMode.Locked : CursorLockMode.None;

        // Change to or from the Escape Menu UI.
        networkFramework.UIManagerComponent.GetUIObject(new string[] { "Escape Menu" }).SetActive(!Cursor.visible);
        networkFramework.UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Crosshair" }).SetActive(Cursor.visible);

        Cursor.visible = !Cursor.visible;
    }

    #endregion
}