using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Represents a Player instance in the Lobby.
public class LobbyPlayer : MessageBase
{
    public int connectionId = 0;
    public bool readyState = false;
    public string name = string.Empty;
}

// Manages hosting and connecting to a Network (Server) based on given IP and Port. Also manages the Player Lobby.
public class NetworkFramework : NetworkManager
{
    public string onlineSceneName;

    public float connectTimeout = 2000f;

    public GameObject parentUI;
    public GameObject playerScoreEntry;
    public GameObject playerScoreList;

    [HideInInspector]
    public UIManager UIManagerComponent = null;

    [HideInInspector]
    public LobbyPlayer myPlayer = new LobbyPlayer();

    [HideInInspector]
    public List<LobbyPlayer> players;

    private int readyNumber = 0;
    private int loadedNumber = 0;

    private float timeoutTimer = 0f;

    private bool readyState = false;
    private bool isServer = false;
    private bool isConnecting = false;

    private InputField networkAddressInput = null;
    private InputField networkPortInput = null;

    private Text lobbyTitleText = null;

    private AsyncOperation operation = null;

    // Custom message types for message handling.
    private class CustomMsgType
    {
        public const short AddLobbyPlayer = MsgType.Highest + 1;
        public const short ChangeState = MsgType.Highest + 2;
        public const short RemovePlayer = MsgType.Highest + 3;
        public const short ChangeScene = MsgType.Highest + 4;
        public const short SetLocalPlayer = MsgType.Highest + 5;
    }

    #region Unity Functions

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        UIManagerComponent = parentUI.GetComponent<UIManager>();

        networkAddressInput = UIManagerComponent.GetUIObject(new string[] { "Network Menu", "IP Field" }).GetComponent<InputField>();
        networkPortInput = UIManagerComponent.GetUIObject(new string[] { "Network Menu", "Port Field" }).GetComponent<InputField>();

        lobbyTitleText = UIManagerComponent.GetUIObject(new string[] { "Lobby", "Lobby Title" }).GetComponent<Text>();

        // Initialize the <players> list.
        players = new List<LobbyPlayer>(maxConnections);
    }

    // Called every frame after Start().
    private void Update ()
    {
        networkAddress = networkAddressInput.text;
        networkPort = int.Parse(networkPortInput.text);
        lobbyTitleText.text = string.Format("Lobby ({0}/{1})", players.Count, maxConnections);

        if (client != null && client.isConnected)
        {
            // If the Player presses the 'Ready' button, send his state change to the Server.
            if (Input.GetButtonDown("Ready"))
            {
                readyState = !readyState;
                myPlayer.readyState = readyState;

                // Tell the Server that we have changed our <readyState>.
                client.Send(CustomMsgType.ChangeState, myPlayer);
            }

            // If all the Players are ready, start the game by loading the online Scene over the Network.
            if (isServer && players.Count > 0)
            {
                if (readyNumber == players.Count)
                {
                    NetworkServer.SendToAll(CustomMsgType.ChangeScene, new IntegerMessage(1));

                    readyNumber = 0;
                }
            }

            if (operation != null && operation.isDone)
            {
                client.Send(CustomMsgType.ChangeScene, new EmptyMessage());

                operation = null;
            }
        }
        else if (isConnecting)
        {
            timeoutTimer += Time.deltaTime;

            if (timeoutTimer >= (connectTimeout / 1000))
            {
                Debug.Log("Failed to connect to Server after " + connectTimeout + "ms");

                isConnecting = false;
                timeoutTimer = 0f;
            }
        }
    }

    // Called before the application quits (on iOS applications don't quit, they suspend)
    private void OnApplicationQuit ()
    {
        DisconnectionCleanUp();
    }

    #endregion

    #region UI Functions

    // Called when the Network Menu -> "Host" UI button is clicked.
    public void HostClick ()
    {
        // If we are currently attempting to connect to a Server already, prevent us from hosting one.
        if (isConnecting)
        {
            return;
        }

        StartHost();

        // Register Server handlers.
        NetworkServer.RegisterHandler(MsgType.Connect, ServerConnectedHandler);
        NetworkServer.RegisterHandler(MsgType.Disconnect, ServerDisconnectedHandler);
        NetworkServer.RegisterHandler(CustomMsgType.AddLobbyPlayer, NetworkCallback);
        NetworkServer.RegisterHandler(CustomMsgType.ChangeState, NetworkCallback);
        NetworkServer.RegisterHandler(CustomMsgType.ChangeScene, ChangeSceneCounter);

        // Register Client handlers.
        client.RegisterHandler(MsgType.Connect, ClientConnectedHandler);

        isServer = true;
        isConnecting = true;

        Debug.Log("Attempting to create Server...");
    }

    // Called when the Network Menu -> "Connect" UI button is clicked.
    public void ConnectClick ()
    {
        StartClient();

        // Register Client handlers.
        client.RegisterHandler(MsgType.Connect, ClientConnectedHandler);

        isConnecting = true;

        Debug.Log("Attempting to connect to Server...");
    }

    // Called when the Network Menu -> "Back" UI button is clicked.
    public void BackClick ()
    {
        UIManagerComponent.ChangeUI("Main Menu");
    }

    // Called when the Lobby -> "Disconnect" UI button is clicked.
    public void DisconnectClick ()
    {
        DisconnectionCleanUp();
    }

    #endregion

    #region UNET Override Functions
    
    // Called on the Server when a Player has sent a 'AddPlayer' message.
    public override void OnServerAddPlayer (NetworkConnection connection, short controllerID)
    {
        Transform spawnPoint = GetStartPosition();

        // Spawn the Player on the Server.
        GameObject playerClone = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation) as GameObject;

        playerClone.GetComponent<NetworkUser>().playerName = players.Find(p => p.connectionId == connection.connectionId).name;

        // Spawn the Player on the Clients.
        NetworkServer.AddPlayerForConnection(connection, playerClone, controllerID);
    }

    // Called on the Client when disconnected from within the game.
    public override void OnClientDisconnect (NetworkConnection connection)
    {
        DisconnectionCleanUp();
    }

    #endregion

    #region UNET Message Handler Functions

    // Called on the Server when a Client connected to the Server.
    private void ServerConnectedHandler (NetworkMessage networkMessage)
    {
        Debug.Log("A client has connected to the Server: " + networkMessage.conn.connectionId);

        // Send a message to the new Client requesting that he adds all the other Clients.
        foreach (LobbyPlayer player in players)
        {
            NetworkServer.SendToClient(networkMessage.conn.connectionId, CustomMsgType.AddLobbyPlayer, player);
        }
    }

    // Called on the Server when a Client disconnected from the Server.
    private void ServerDisconnectedHandler(NetworkMessage networkMessage)
    {
        Debug.Log("A client has disconnected from the Server: " + networkMessage.conn.connectionId);

        // Destroy all the disconnected Player's objects.
        NetworkServer.DestroyPlayersForConnection(networkMessage.conn);

        // Find the disconnected Player's LobbyPlayer.
        LobbyPlayer disconnectedPlayer = players.Find(p => p.connectionId == networkMessage.conn.connectionId);

        // Tell all the Clients to remove his LobbyPlayer.
        NetworkServer.SendToAll(CustomMsgType.RemovePlayer, disconnectedPlayer);
    }

    // Called on a Client when he connected to a Server.
    private void ClientConnectedHandler (NetworkMessage networkMessage)
    {
        // Register Client handlers.
        client.RegisterHandler(MsgType.AddPlayer, ClientAddPlayer);
        client.RegisterHandler(CustomMsgType.AddLobbyPlayer, AddNewLobbyPlayer);
        client.RegisterHandler(CustomMsgType.ChangeState, ChangeReadyState);
        client.RegisterHandler(CustomMsgType.RemovePlayer, RemoveDisconnectedPlayer);
        client.RegisterHandler(CustomMsgType.ChangeScene, ChangeScene);
        client.RegisterHandler(CustomMsgType.SetLocalPlayer, SetLocalPlayer);

        // Create a LobbyPlayer for this new Client.
        myPlayer.name = PlayerPrefs.GetString("PlayerName", "Player");

        // Add my own Player instance to the players list.
        client.Send(CustomMsgType.AddLobbyPlayer, myPlayer);

        isConnecting = false;

        // Change to the Lobby UI.
        UIManagerComponent.ChangeUI("Lobby");

        Debug.Log("Connected to the Server.");
    }

    // Called when this Client receives the 'SetLocalPlayer' message.
    private void SetLocalPlayer (NetworkMessage networkMessage)
    {
        myPlayer = networkMessage.ReadMessage<LobbyPlayer>();
    }

    // Called when this Client receives the 'AddPlayer' message.
    private void ClientAddPlayer (NetworkMessage networkMessage)
    {
        ClientScene.Ready(client.connection);
        ClientScene.AddPlayer(0);
    }

    // Called when this Client receives a 'AddLobbyPlayer' message.
    private void AddNewLobbyPlayer (NetworkMessage networkMessage)
    {
        LobbyPlayer targetPlayer = networkMessage.ReadMessage<LobbyPlayer>();

        // Add the new lobby Player.
        players.Add(targetPlayer);

        // Update the lobby slots to contain the change.
        UpdateLobbySlots();
    }

    // Used to receive, configure, and re-send NetworkMessages received from Clients.
    private void NetworkCallback (NetworkMessage networkMessage)
    {
        LobbyPlayer targetPlayer = null;

        switch (networkMessage.msgType)
        {
            case CustomMsgType.AddLobbyPlayer:

                // Return the LobbyPlayer to be added.
                targetPlayer = networkMessage.ReadMessage<LobbyPlayer>();
                targetPlayer.connectionId = networkMessage.conn.connectionId;

                NetworkServer.SendToClient(targetPlayer.connectionId, CustomMsgType.SetLocalPlayer, targetPlayer);

                break;

            case CustomMsgType.ChangeState:

                // Get the LobbyPlayer that sent the ChangeState message.
                targetPlayer = networkMessage.ReadMessage<LobbyPlayer>();

                // Update the amount of ready Players based on the Player's ready state.
                readyNumber += targetPlayer.readyState ? 1 : -1;

                break;
        }

        // Send the configured NetworkMessage to all the Clients.
        NetworkServer.SendToAll(networkMessage.msgType, targetPlayer);
    }

    // Called when this Client received a 'ChangeState' NetworkMessage.
    private void ChangeReadyState (NetworkMessage networkMessage)
    {
        LobbyPlayer targetPlayer = networkMessage.ReadMessage<LobbyPlayer>();

        // Find the Player that changed his ready state in our <players> list.
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].connectionId == targetPlayer.connectionId)
            {
                // Update his ready state.
                players[i].readyState = targetPlayer.readyState;
                break;
            }
        }

        // Update the lobby slots to contain the change.
        UpdateLobbySlots();
    }

    // Called when this Client received a 'RemovePlayer' NetworkMessage.
    private void RemoveDisconnectedPlayer (NetworkMessage networkMessage)
    {
        LobbyPlayer disconnectedPlayer = networkMessage.ReadMessage<LobbyPlayer>();

        // Remove the <disconnectedPlayer> from our <players> list.
        players.Remove(players.Find(p => p.connectionId == disconnectedPlayer.connectionId));

        // Update the lobby slots to contain the change.
        UpdateLobbySlots();
    }

    // Called when this Client receives a 'ChangeScene' message.
    private void ChangeScene (NetworkMessage networkMessage)
    {
        int sceneIndex = networkMessage.ReadMessage<IntegerMessage>().value;

        operation = SceneManager.LoadSceneAsync(sceneIndex);
    }

    // Called when this Server receives a 'ChangeScene' message.
    private void ChangeSceneCounter (NetworkMessage networkMessage)
    {
        loadedNumber++;

        if (loadedNumber == players.Count)
        {
            NetworkServer.SpawnObjects();
            NetworkServer.SendToAll(MsgType.AddPlayer, new EmptyMessage());

            loadedNumber = 0;
        }
    }

    #endregion

    #region Custom Utility Functions

    // Called to clean up singleton instances after disconnecting from a Server.
    public void DisconnectionCleanUp ()
    {
        if (isServer)
        {
            // If we are a Server, clean-up the Server-side of this Host.

            StopServer();

            NetworkServer.Shutdown();
            NetworkServer.Reset();

            isServer = false;
        }

        StopClient();

        // If we disconnected from another scene, go back to the 'Menu' scene.
        if (SceneManager.GetActiveScene().name != "Menu")
        {
            SceneManager.LoadScene("Menu");
        }

        // Change back to the Network Menu UI.
        UIManagerComponent.ChangeUI("Network Menu");
        UIManagerComponent.GetUIObject(new string[] { "Escape Menu" }).SetActive(false);

        // Reset the Player HUD UI elements.
        Transform playerHUD = UIManagerComponent.GetUIObject(new string[] { "Player HUD" }).transform;

        playerHUD.Find("Crosshair").gameObject.SetActive(true);
        playerHUD.Find("Death Message").gameObject.SetActive(false);

        Transform ammoUI = playerHUD.Find("Ammo");

        ammoUI.gameObject.SetActive(true);
        ammoUI.GetComponent<Text>().text = "0/0";

        Transform waveNumberUI = playerHUD.Find("Wave Number");

        waveNumberUI.gameObject.SetActive(true);
        waveNumberUI.GetComponent<Text>().text = "0";

        // Reset the variables from this singleton NetworkManager.
        readyState = false;
        myPlayer = new LobbyPlayer();
        players = new List<LobbyPlayer>(4);

        UpdateLobbySlots();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Disconnected from the Server.");
    }

    // Will re-order all the Slots with updated information on the Player it contains, if <reset> 
    // is set to true, then it will reset all the Lobby Slots.
    private void UpdateLobbySlots ()
    {
        List<Transform> slots = new List<Transform>();

        // May cause NullReferenceException if the Player had activated the Escape Menu and closed the Application.
        if (UIManagerComponent == null)
        {
            return;
        }

        // Add all the Lobby Slot UI Transforms to the <slots> list.
        foreach (Transform lobbyChild in UIManagerComponent.GetUIObject(new string[] { "Lobby" }).transform)
        {
            if (lobbyChild.name.Contains("Lobby Slot"))
            {
                slots.Add(lobbyChild.FindChild("Text"));
            }
        }

        // Change the information of the populated Lobby Slots, based on if it is an update or a reset request.
        for (int i = 0; i < slots.Count; i++)
        {
            Text slotText = slots[i].GetComponent<Text>();

            if (players.Count - 1 < i)
            {
                slotText.text = "Empty Slot";
            }
            else
            {
                string readyState = players[i].readyState ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";

                slotText.text = string.Format("{0}: {1}", players[i].name, readyState);
            }
        }
    }

    #endregion
}