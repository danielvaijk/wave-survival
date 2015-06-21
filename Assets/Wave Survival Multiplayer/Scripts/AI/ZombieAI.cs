using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkAnimator))]

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Controls all aspects for a Zombie, being pathfinding, going after Players, receiving and dealing damage, and etc.

public class ZombieAI : NetworkBehaviour
{
    public float currentHealth = 100f;

    public float movementSpeed = 2f;
    public float turningSpeed = 8f;

    public float attackDamage = 30f;
    public float attackRange = 1.4f;
    public float attackRate = 3f;
    public float screamRate = 4f;

    public int damageReward = 10;
    public int deathReward = 50;

    public GameObject deadBody;

    public AudioClip[] screamSounds;

    [HideInInspector]
    public bool isInside = false;

    [HideInInspector]
    public Throwable grenadeExplosion;

    private float attackTimer = 0f;
    private float screamTimer = 0f;

    private bool isDead = false;

    private Transform currentTarget = null;
    private Transform closestPlayer = null;

    private UnityEngine.AI.NavMeshAgent navMeshAgent = null;
    private Animator thisAnimator = null;
    private NetworkAnimator thisNetworkAnimator = null;

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        // If this is a Player Zombie, then setup the local Player zombie.
        if (isLocalPlayer)
        {
            GetComponentInChildren<Camera>().enabled = true;
            GetComponentInChildren<AudioListener>().enabled = true;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Get the Player HUD UI.
            UIManager UIManagerComponent = GameObject.Find("UI").GetComponent<UIManager>();

            // Disable all UI canvases except the Player HUD.
            foreach (Transform UIElement in UIManagerComponent.transform)
            {
                if (UIElement.gameObject.name == "Player HUD")
                {
                    continue;
                }

                UIElement.gameObject.SetActive(false);
            }

            // Modify the Player HUD UI to display only the death message.
            UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Crosshair" }).gameObject.SetActive(false);
            UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Death Message" }).gameObject.SetActive(true);
            UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Ammo" }).gameObject.SetActive(false);
            UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Wave Number" }).gameObject.SetActive(false);

            // Disable the Escape Menu.
            UIManagerComponent.GetUIObject(new string[] { "Escape Menu" }).gameObject.SetActive(false);
        }

        // Get the essential variables for this Zombie (both for Clients and the Server).
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        thisAnimator = GetComponent<Animator>();
        thisNetworkAnimator = GetComponent<NetworkAnimator>();

        thisAnimator.SetFloat("Attack Rate", attackRate);

        if (!isServer)
        {
            // From here on only the Server will manage the NavMeshAgent and ZombieAI components.
            navMeshAgent.enabled = false;
            enabled = false;
            return;
        }

        // Get the barricade that we are going to use to reach the Player.
        Transform closestBarricade = ClosestBarricade(GameObject.FindGameObjectsWithTag("Barricade"));
        closestPlayer = ClosestPlayer(GameObject.FindGameObjectsWithTag("Player"));

        if (!isInside)
        {
            // If there are no barricades to target, then go to the closest Player, otherwise go to the closest barricade.
            currentTarget = (closestBarricade == null) ? closestPlayer : closestBarricade;
        }
        else
        {
            // If we are inside go after the closest Player.
            currentTarget = closestPlayer;
        }

        // Offset the attack range by taking the NavMeshAgent radius into account.
        attackRange += navMeshAgent.radius;

        // Configure the NavMeshAgent to match this Zombie's variable settings.
        navMeshAgent.destination = currentTarget.position;
        navMeshAgent.stoppingDistance = 0f;
        navMeshAgent.updateRotation = false;
        navMeshAgent.speed = movementSpeed;
    }

    // Called every frame after Start().
    private void Update ()
    {
        closestPlayer = ClosestPlayer(GameObject.FindGameObjectsWithTag("Player"));

        // Updated the movement animation float, based on the movement velocity.
        thisAnimator.SetFloat("Movement Speed", navMeshAgent.velocity.magnitude);

        // If our scream timer is up and we have a scream sound available, then scream.
        if ((screamTimer >= screamRate) && (screamSounds.Length > 0))
        {
            int screamSoundIndex = Random.Range(0, screamSounds.Length);

            // Scream over the Network.
            RpcScream(screamSoundIndex);
            screamTimer = 0f;
        }
        else
        {
            screamTimer += Time.deltaTime;
        }

        if (currentTarget != null)
        {
            // Get the NavMeshPath distance to our current target.
            float currentTargetDistance = GetPathDistance(currentTarget.position, Color.blue);

            // Check if the target is within our attack range.
            if (currentTargetDistance <= attackRange)
            {
                if (currentTarget.tag == "Barricade")
                {
                    Barricade targetBarricade = currentTarget.GetComponent<Barricade>();

                    if (targetBarricade.currentHealth <= 0f)
                    {
                        // If the barricade we were currently attacking is open, then we are inside.
                        isInside = true;
                        navMeshAgent.stoppingDistance = attackRange;
                    }
                    else if (attackTimer >= attackRate)
                    {
                        // Get the distance check data.
                        float interactOffset = closestPlayer.GetComponent<NetworkUser>().interactDistance;
                        float correctedRange = attackRange + interactOffset;
                        float closestPlayerDistance = Vector3.Distance(transform.position, closestPlayer.position);

                        // If the closest Player to us is within our attack range, then attack him instead of the barricade.
                        Transform target = (closestPlayerDistance <= correctedRange) ? closestPlayer : currentTarget;

                        AttackTarget(target);
                    }
                }
                else
                {
                    if (attackTimer >= attackRate)
                    {
                        // Attack our current target Player.
                        AttackTarget(currentTarget);
                    }
                }
            }

            // If our destination is not our current position, then update it.
            if (navMeshAgent.destination != currentTarget.position)
            {
                navMeshAgent.destination = currentTarget.position;
            }

            // Get the rotation we need to align to, to face our current target.
            Quaternion toRotation = Quaternion.LookRotation(currentTarget.position - transform.position, Vector3.up);

            // Isolate the Y axis of the <toRotation> rotation.
            toRotation.x = 0f;
            toRotation.z = 0f;

            // Rotate towards <toRotation> rotation.
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * turningSpeed);
        }

        // If there is a player that is closest to us and we are inside, then set him as our target.
        if ((closestPlayer != null) && isInside)
        {
            currentTarget = closestPlayer;
        }

        attackTimer += Time.deltaTime;
    }

    #endregion

    #region Custom Utility Functions

    // Handles receiving damage, dying and rewarding the Player with <shooterID> for the damage/kill.
    public void TakeDamage(float damage, NetworkInstanceId shooterID)
    {
        // Prevent this zombie from taking damage is he is dead.
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        if (isServer)
        {
            // If we are the Server, reward the Player with <shooterID>.
            RewardPlayer(shooterID);
        }

        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
        }

        if (isDead)
        {
            // Get the flame child, if any.
            Transform flameChild = transform.Find("Flame");

            // Instantiate the ragdoll for this zombie, if there is one.
            if (deadBody != null)
            {
                GameObject deadClone = Instantiate(deadBody, transform.position, transform.rotation) as GameObject;

                // Set the <deadClone> name and make sure that he does not have a 'Zombie' tag.
                deadClone.name = "Dead Zombie";
                deadClone.tag = "Untagged";

                if (localPlayerAuthority)
                {
                    NetworkServer.ReplacePlayerForConnection(connectionToClient, deadClone, 0);
                }

                // If we had a flame child, then parent it to our dead body.
                if (flameChild != null)
                {
                    flameChild.SetParent(deadClone.transform, true);
                    flameChild.localPosition = Vector3.zero;
                }

                // If the death was by grenade and we are the Server, then apply explosion force to the ragdoll.
                if ((grenadeExplosion != null) && isServer)
                {
                    // Apply the grenade explosion force to all rigidbody children of the <deadClone>.
                    foreach (Transform child in deadClone.transform)
                    {
                        Rigidbody childRigidbody = child.GetComponent<Rigidbody>();

                        // Get the explosion data.
                        float force = grenadeExplosion.damage * 10f;
                        float radius = grenadeExplosion.blastRadius;
                        Vector3 position = grenadeExplosion.transform.position;

                        // Apply the grenade explosion force to the current child of the <deadClone>.
                        childRigidbody.AddExplosionForce(force, position, radius);
                    }
                }
            }
            else if (flameChild != null)
            {
                Destroy(flameChild.gameObject);
            }
            else if (localPlayerAuthority && isServer)
            {
                NetworkServer.DestroyPlayersForConnection(connectionToClient);
            }

            Destroy(gameObject);

            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    // Applies damage to the given <target>.
    private void AttackTarget (Transform target)
    {
        // Trigger the attack animation.
        thisNetworkAnimator.SetTrigger("Attack");

        if (target.tag == "Barricade")
        {
            Barricade targetBarricade = target.GetComponent<Barricade>();

            // Damage the Barricade over the Network.
            targetBarricade.CmdDamageBarricade(attackDamage);
        }
        else
        {
            NetworkUser targetPlayer = target.GetComponent<NetworkUser>();

            // Damage the Player over the Network.
            targetPlayer.CmdDamagePlayer(attackDamage);
        }

        // Reset the attack timer so we can attack again.
        attackTimer = 0f;
    }

    // Checks all the Barricades in <barricades> for the one that is closest to this Zombie.
    private Transform ClosestBarricade (GameObject[] barricades)
    {
        float smallestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (GameObject barricade in barricades)
        {
            // Get the path distance to the current barricade.
            float barricadeDistance = GetPathDistance(barricade.transform.position, Color.yellow);

            // If this path is the smallest one so far, set this as the closest Barricade.
            if (barricadeDistance < smallestDistance)
            {
                smallestDistance = barricadeDistance;
                closestTarget = barricade.transform;
            }
        }

        return closestTarget;
    }

    // Returns the closest alive Player to this Zombie.
    private Transform ClosestPlayer (GameObject[] players)
    {
        // Only check for the closest Player if there are any.
        if (players.Length > 0)
        {
            float smallestDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (GameObject player in players)
            {
                // Get the vector distance to this Player.
                float playerDistance = 0f;

                if (isInside)
                {
                    // If we are inside get the NavMeshPath distance.
                    playerDistance = GetPathDistance(player.transform.position, Color.yellow);
                }
                else
                {
                    // If we are outside get the Vector3 distance.
                    playerDistance = Vector3.Distance(transform.position, player.transform.position);
                }

                // If this distance is the smallest one so far, set this as the closest Player.
                if (playerDistance < smallestDistance)
                {
                    smallestDistance = playerDistance;
                    closestTarget = player.transform;
                }
            }

            return closestTarget;
        }

        return null;
    }

    // Will reward the Player with the NetworkInstanceID equal to <shooterID> that killed this Zombie.
    private void RewardPlayer (NetworkInstanceId shooterID)
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkUser networkUser = player.GetComponent<NetworkUser>();

            // If we found the Player with <shooterID>, then reward him.
            if (networkUser.netId == shooterID)
            {
                if (currentHealth <= 0f)
                {
                    // If he killed us, add to his kills and give him the <deadReward>.
                    networkUser.CmdUpdateKills();
                    networkUser.CmdUpdateCurrency(deathReward, false, false);
                }
                else
                {
                    // If he damaged us, give him the <damageReward>.
                    networkUser.CmdUpdateCurrency(damageReward, false, false);
                }
            }
        }
    }

    // Will calculate a NavMeshPath on the NavMesh to the <targetPosition> and will
    // return the distance from start to end on the calculated NavMeshPath.
    private float GetPathDistance (Vector3 targetPosition, Color color)
    {
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

        // Calculate the path to the <targetPosition>.
        navMeshAgent.CalculatePath(targetPosition, path);

        float distance = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            // Add-up to the total path distance.
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);

            // Draw the path.
            Debug.DrawLine(path.corners[i - 1], path.corners[i], color);
        }

        return distance;
    }

    #endregion

    #region UNET Message Functions

    // Scream over the Network.
    [ClientRpc(channel = 1)]
    private void RpcScream (int screamSoundIndex)
    {
        AudioClip screamSound = screamSounds[screamSoundIndex];

        GetComponent<AudioSource>().PlayOneShot(screamSound);

        // Trigger the scream animation, and define how fast it will play depending on the scream sound length.
        thisAnimator.SetFloat("Scream Speed", screamSound.length);
        thisAnimator.SetTrigger("Scream");
    }

    #endregion
}