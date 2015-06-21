using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkUser))]

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Takes care of Player movement like walking and sprinting, step sounds and etc.

public class PlayerMovement : NetworkBehaviour
{
    public float movementSpeed = 2f;
    public float sprintingSpeed = 4f;

    public AudioClip[] stepSounds;

    private float currentSpeed = 0f;
    private float stepTimer = 0f;
    private float idleTimer = 0f;

    private Vector3 movementDirection = Vector3.zero;
    private Vector3 lastMovementPosition = Vector3.zero;

    private Animator animator = null;

    private NetworkUser localNetworkUser = null;

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        // Freeze the Players rigidbody to prevent it from tipping over.
        GetComponent<Rigidbody>().freezeRotation = true;

        animator = GetComponent<Animator>();
        localNetworkUser = GetComponent<NetworkUser>();

        lastMovementPosition = transform.position;
    }

    // Called every frame after Start().
    private void Update ()
    {
        // Only the owner of this Player instance can control this component.
        if (!isLocalPlayer)
        {
            return;
        }

        // Updated the movement animation float, based on the movement velocity.
        animator.SetFloat("Movement Speed", movementDirection.magnitude);

        // Only allow the Player to move if the Escape Menu is not activate.
        if (!localNetworkUser.showEscapeMenu)
        {
            currentSpeed = Input.GetButton("Sprint") ? sprintingSpeed : movementSpeed;

            movementDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            movementDirection = transform.TransformDirection(movementDirection);

            transform.position += movementDirection * currentSpeed * Time.deltaTime;

            if (movementDirection.magnitude > 0.5f)
            {
                if (stepTimer >= 1 / currentSpeed)
                {
                    // Add a variation to the step sound pitch.
                    float randomPitch = Random.Range(0.9f, 1.3f);

                    // Randomly select a step sound from <stepSounds>.
                    int stepSoundIndex = Random.Range(0, stepSounds.Length);

                    // Play the step sound over the Network.
                    CmdPlayStepSound(stepSoundIndex, randomPitch);
                    stepTimer = 0f;
                }
                else
                {
                    stepTimer += Time.deltaTime;
                }
            }
        }
        else
        {
            movementDirection = Vector3.zero;
        }

        // If the Player moves, break his idle.
        if (lastMovementPosition != transform.position)
        {
            lastMovementPosition = transform.position;

            localNetworkUser.BreakIdle();
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= localNetworkUser.idleTime)
            {
                animator.SetBool("Idle", true);
                idleTimer = 0f;
            }
        }
    }

    #endregion

    #region UNET Message Functions

    // Called on the Server. Used to callback a RPC to all the Clients for the same function.
    [Command(channel = 0)]
    private void CmdPlayStepSound (int index, float pitch)
    {
        RpcPlayStepSound(index, pitch);
    }

    // Called on the Clients. Plays the step sound (index) over the Network with given pitch.
    // It's also a little work-around to the AudioSource.PlayClipAtPoint(), since it does not return
    // the created GameObject, so that we can control certain aspects of its AudioSource.
    [ClientRpc(channel = 0)]
    private void RpcPlayStepSound (int index, float pitch)
    {
        GameObject soundObject = new GameObject("Step Sound");

        soundObject.transform.position = transform.position;

        AudioSource stepSource = soundObject.AddComponent<AudioSource>();
        AudioClip stepSound = stepSounds[index];

        stepSource.volume = 0.25f;
        stepSource.pitch = pitch;

        stepSource.PlayOneShot(stepSound);

        Destroy(soundObject, stepSound.length);
    }

    #endregion
}