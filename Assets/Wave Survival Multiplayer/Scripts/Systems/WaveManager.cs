using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using System.Collections;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Used to spawn an infinite amount of waves of zombies that increase in number as the waves increment.

// 1. Remember to keep the spawn points inside a valid space in the scene.

public class WaveManager : NetworkBehaviour
{
    public int amount = 1;
    public int increment = 2;

    public float waveRate = 1;
    public float zombieRate = 2;

    public GameObject[] zombies;

    public Transform[] spawns;

    private int aliveZombies = 0;
    private int amountToSpawn = 0;
    private int currentWave = 0;
    private int lastWave = 0;

    private bool canSpawnWave = true;
    private bool canSpawnZombie = true;

    private NetworkFramework networkFramework = null;

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        enabled = isServer;

        networkFramework = NetworkFramework.singleton as NetworkFramework;
    }

    // Called every frame after Start().
    private void Update ()
    {
        int spawnedPlayers = GameObject.FindGameObjectsWithTag("Player").Length;

        aliveZombies = GameObject.FindGameObjectsWithTag("Zombie").Length;

        // If all the Players have spawned, start the wave spawning.
        if (spawnedPlayers == networkFramework.players.Count)
        {
            // If there are no zombies alive, spawn a new wave.
            if (aliveZombies == 0)
            {
                if (currentWave == lastWave)
                {
                    // If we are still in the old wave, increment the wave.
                    if (canSpawnWave)
                    {
                        StartCoroutine(NewWave(waveRate));
                    }
                }
                else
                {
                    // If we are in the new wave and there are no zombies alive, start spawning the zombies.
                    if (canSpawnZombie)
                    {
                        StartCoroutine(SpawnZombies(zombieRate));
                    }
                }
            }
        }
    }

    #endregion

    #region UNET Message Functions

    // Will update the current wave number UI on the Clients.
    [ClientRpc(channel = 1)]
    private void RpcUpdateWaveNumber(int waveNumber)
    {
        UIManager UIManagerComponent = GameObject.Find("UI").GetComponent<UIManager>();
        Text waveNumberText = UIManagerComponent.GetUIObject(new string[] { "Player HUD", "Wave Number" }).GetComponent<Text>();

        waveNumberText.text = waveNumber.ToString();
    }

    #endregion

    #region Custom Utility Functions

    // Increments the wave number.
    private IEnumerator NewWave (float waitTime)
    {
        canSpawnWave = false;
        amountToSpawn = amount + (increment * currentWave);

        yield return new WaitForSeconds(waitTime);

        currentWave++;

        // Update the wave UI number on all Clients.
        RpcUpdateWaveNumber(currentWave);
    }

    // Spawns the zombies based on the incremented wave.
    private IEnumerator SpawnZombies (float waitTime)
    {
        canSpawnZombie = false;

        for (int i = 0; i < amountToSpawn; i++)
        {
            yield return new WaitForSeconds(waitTime);

            // Get the random Zombie and Spawn Position instances.
            GameObject randomZombie = zombies[Random.Range(0, zombies.Length)];
            Vector3 randomSpawnPosition = spawns[Random.Range(0, spawns.Length)].position;

            // Spawn the Zombie on the Server.
            GameObject zombie = Instantiate(randomZombie, randomSpawnPosition, Quaternion.identity) as GameObject;

            zombie.name = string.Format("Zombie {0}", aliveZombies);

            // Spawn the Zombie on the Clients.
            NetworkServer.Spawn(zombie);
        }

        canSpawnWave = true;
        canSpawnZombie = true;
        lastWave = currentWave;
    }

    #endregion
}