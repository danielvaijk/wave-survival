using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents a weapon inside the game.

// 1. All GameObjects with colliders in the scene must have a DecalMaterial component on them.

[RequireComponent(typeof(AudioSource))]

[DisallowMultipleComponent]

public class Weapon : MonoBehaviour
{
    public FireType fireType = FireType.Automatic;

    public int loadedAmmo = 30;
    public int storedAmmo = 60;

    public int maxAmmoPerLoad = 30;
    public int maxAmmoStorage = 60;

    public float fireDistance = 25f;
    public float fireRate = 0.1f;
    public float reloadSpeed = 2f;
    public float aimSpeed = 20f;
    public float shotDamage = 25f;

    public string reloadAnimationTrigger;

    public bool allowDebugging;

    public Transform shootPoint;

    public GameObject muzzleFlash;
    public GameObject throwObject;

    public Vector3 aimPoint;

    public AudioClip shotSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    [HideInInspector]
    public RaycastHit hit;

    // Since we use a WeaponEditor, these won't appear in the inspector. These are set by the local NetworkUser.
    public PlayerInventory playerInventory = null;
    public NetworkUser localNetworkUser = null;

    private int weaponIndex = 0;

    private float fireTimer = 0f;
    private float reloadTimer = 0f;

    private bool isReloading = false;

    private Vector3 inicialPosition = Vector3.zero;

    private Animator animator = null;
    private Text playerAmmo = null;

    private NetworkCallback networkCallback = null;

    public enum FireType { Automatic, SemiAutomatic, BoltAction, Throwable };

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        fireTimer = fireRate;
        reloadTimer = reloadSpeed;
        inicialPosition = transform.parent.localPosition;

        weaponIndex = GetWeaponIndex();

        playerAmmo = GameObject.Find("Ammo").GetComponent<Text>();

        animator = localNetworkUser.GetComponent<Animator>();
        networkCallback = localNetworkUser.GetComponent<NetworkCallback>();
    }

    // Called every frame after Start().
    private void Update ()
    {
        reloadTimer += Time.deltaTime;

        if (reloadTimer >= reloadSpeed)
        {
            isReloading = false;
        }

        // Show the ammo UI based on the type of weapon this is.
        if (fireType != FireType.Throwable)
        {
            playerAmmo.text = string.Format("{0}/{1}", loadedAmmo, storedAmmo);
        }
        else
        {
            playerAmmo.text = loadedAmmo.ToString();
        }

        // We cannot perform any weapon functionalities while the pause menu is activated.
        if (localNetworkUser.showEscapeMenu)
        {
            return;
        }

        if (Input.GetButton("Fire2"))
        {
            // Break the players idle, if any.
            BreakPlayerIdle();

            // If we press the Right Mouse Button then move to the <aimPoint> position.
            transform.parent.localPosition = Vector3.Slerp(transform.parent.localPosition, aimPoint, aimSpeed * Time.deltaTime);
        }
        else
        {
            // If we release the Right Mouse Button, then move back to the <aimPoint> position.
            if (transform.parent.localPosition != inicialPosition)
            {
                transform.parent.localPosition = Vector3.Slerp(transform.parent.localPosition, inicialPosition, aimSpeed * Time.deltaTime);
            }
        }

        if (Input.GetButtonDown("Reload") && fireType != FireType.Throwable)
        {
            if (reloadTimer >= reloadSpeed)
            {
                // Reload only if are <loadedAmmo> is not full, and we have <storedAmmo> to reload from.
                if ((loadedAmmo < maxAmmoPerLoad) && (storedAmmo > 0))
                {
                    // The amount to be reloaded into <loadedAmmo>.
                    int missingAmount = maxAmmoPerLoad - loadedAmmo;

                    if (storedAmmo >= missingAmount)
                    {
                        loadedAmmo += missingAmount;
                        storedAmmo -= missingAmount;
                    }
                    else
                    {
                        loadedAmmo += storedAmmo;
                        storedAmmo = 0;
                    }

                    isReloading = true;

                    // Play the reload sound over the Network.
                    networkCallback.CmdReload(weaponIndex, reloadAnimationTrigger);
                    reloadTimer = 0f;
                }
            }
        }

        if (!isReloading)
        {
            if (fireType == FireType.Automatic && Input.GetButton("Fire1"))
            {
                AttemptShot();
            }
            else if (fireType == FireType.SemiAutomatic && Input.GetButtonDown("Fire1"))
            {
                AttemptShot();
            }
            else if (fireType == FireType.BoltAction && Input.GetButtonDown("Fire1"))
            {
                AttemptShot();
            }
            else if (Input.GetButtonDown("Throw"))
            {
                AttemptShot();
            }

            fireTimer += Time.deltaTime;
        }
    }

    #endregion

    #region Custom Utility Functions

    // Will attempt to shoot the weapon, depending what type this weapon it is, and if it has loaded ammo or not.
    private void AttemptShot ()
    {
        BreakPlayerIdle();

        bool specialWeapon = ((fireType == FireType.BoltAction) || (fireType == FireType.Throwable));

        if (loadedAmmo > 0)
        {
            if (fireTimer >= fireRate || specialWeapon)
            {
                // Shoot over the Network.
                networkCallback.CmdFire(localNetworkUser.netId, weaponIndex);

                loadedAmmo--;

                if (!specialWeapon)
                {
                    fireTimer = 0f;
                }
            }
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            AudioSource.PlayClipAtPoint(emptySound, transform.position, 0.5f);
        }
    }

    // Will make this Player with <shooterID> shoot over the Network.
    public void Shoot (NetworkInstanceId shooterID)
    {
        Ray ray = new Ray(shootPoint.position, shootPoint.forward);

        // If the muzzle flash is not null then instantiate it at the <shootPoint> position.
        if (muzzleFlash != null)
        {
            GameObject muzzleFlashClone = Instantiate(muzzleFlash, shootPoint.position, shootPoint.rotation) as GameObject;
            Destroy(muzzleFlashClone, 0.05f);
        }

        if (Physics.Raycast(ray, out hit, fireDistance, ~(1 << 8)))
        {
            DecalMaterial decalMaterial = hit.transform.GetComponent<DecalMaterial>();

            if (decalMaterial != null)
            {
                // Calculate the bullets trajectory through DecalMaterial GameObjects.
                decalMaterial.CalculatePenetration(this, shooterID);
            }
        }

        // Play the shot sound if there is one.
        if (shotSound != null)
        {
            AudioSource.PlayClipAtPoint(shotSound, transform.position, 1f);
        }

        if (allowDebugging)
        {
            Debug.DrawRay(shootPoint.position, shootPoint.forward * fireDistance, Color.red);
        }
    }

    // Will make this Player with <shooterID> throw this weapon over the Network.
    public void Throw (NetworkInstanceId shooterID)
    {
        // Create the throwable version of this weapon.
        GameObject clone = Instantiate(throwObject, shootPoint.position, shootPoint.rotation) as GameObject;

        Throwable throwable = clone.GetComponent<Throwable>();
        Rigidbody cloneRigidbody = clone.GetComponent<Rigidbody>();

        if (shotSound != null)
        {
            AudioSource.PlayClipAtPoint(shotSound, transform.position, 0.5f);
        }

        // Throw the 'weapon' using physics.
        cloneRigidbody.AddForce(shootPoint.forward * fireDistance, ForceMode.Impulse);

        throwable.shooterID = shooterID;
    }

    // Gets this weapons child index in the <inventoryParent> Transform.
    private int GetWeaponIndex ()
    {
        for (int i = 0; i < playerInventory.inventoryParent.childCount; i++)
        {
            if (playerInventory.inventoryParent.GetChild(i).name == transform.name + " (Player)")
            {
                return i;
            }
        }

        Debug.LogWarning("Could not find child weapon index, returning 0.");
        return 0;
    }

    // Will brake the Player from the idle status.
    private void BreakPlayerIdle ()
    {
        if (animator.GetBool("Idle"))
        {
            animator.SetBool("Idle", false);
        }
    }

    #endregion
}