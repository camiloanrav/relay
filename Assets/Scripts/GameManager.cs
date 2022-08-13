using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Http;
using Unity.Services.Relay.Models;
using System.Linq;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Netcode.Transports.UTP;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager _instance;
    public static GameManager Instance => _instance;
    public Text LobbyeLabel;
    private string _lobbyId;

    private RelayHostData _hostData;
    private RelayJoinData _joinData;

    private void Awake()
    {
        // Just a basic singleton
        if (_instance is null)
        {
            _instance = this;
            return;
        }

        Destroy(this);
    }

    // Start is called before the first frame update
    async void Start()
    {
        var options = new InitializationOptions();
        #if UNITY_EDITOR
            // Remove this if you don't have ParrelSync installed. 
            // It's used to differentiate the clients, otherwise lobby will count them as the same
            if (ClonesManager.IsClone()) options.SetProfile(ClonesManager.GetArgument());
            else options.SetProfile("Primary");
        #endif
        // UnityServices.InitializeAsync() will initialize all services that are subscribed to Core.
        await UnityServices.InitializeAsync(options);
        Debug.Log(UnityServices.State);
        SetupEvents();
        // Login
        await SignInAnonymouslyAsync();

        // Subscribe to NetworkManager events
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
    }

    #region Network events

    private void ClientConnected(ulong id)
    {
        // Player with id connected to our session
        Debug.Log("Connected player with id: " + id);

    }

    #endregion

    #region Login

    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            // Shows how to get a playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
            // Shows how to get an access token
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player signed out.");
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    #region Lobby

    public async void FindMatch()
    {
        Debug.Log("Looking for a Lobby...");
        try
        {
            // Quick-join a random lobby with a maximum capacity of 10 or more players.
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            /* options.Filter = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.MaxPlayers,
                    op: QueryFilter.OpOptions.GE,
                    value: "10")
            }; */

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log("Joined lobby: " + lobby.Id);
            Debug.Log("Lobby players: " + lobby.Players.Count);

            // Relay...

            // Retrieve the Relay code previously set in the create match
            string joinCode = lobby.Data["joinCode"].Value;

            Debug.Log("Received code: " + joinCode);

            //Ask Unity Services to join a Relay allocation based on our join code
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);

            //NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("room password");

            // Finally start the client
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Cannot find a match: " + e);
            CreateMath();
        }
    }

    public async void FindAllMatches()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            // The number of results to return.
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            Debug.Log("Lobbies available:");
            if (lobbies.Results.Count > 0)
            {
                for (int i = 0; i < lobbies.Results.Count; i++)
                {
                    Debug.Log(lobbies.Results[i].Id + " - " + lobbies.Results[i].Name);
                }
            }
            else
            {
                Debug.Log("Nothing");
            }
            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void CreateMath(bool IsPrivate = false)
    {
        Debug.Log("Crating a new Lobby...");
        string lobbyName = "test lobby";
        int maxPlayers = 4;
        // External connections
        int maxConnections = maxPlayers - 1;

        //Ask Unity Services to allocate a Relay server that will handle up to 4 players: 3 peers and the host
        //The Allocation class represents all the necessary data for a Host player to start hosting using the specific Relay server allocated.
        Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

        _hostData = new RelayHostData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        // Retrieve JoinCode
        _hostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = IsPrivate;

        // Put the JoinCode in the lobby data, visible by every member
        options.Data = new Dictionary<string, DataObject>()
        {
            {
                "joinCode", new DataObject(
                    visibility: DataObject.VisibilityOptions.Member,
                    value: _hostData.JoinCode)
            },
        };

        // Lobby parameters code goes here...
        // See 'Creating a Lobby' for example parameters
        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        // Save Lobby ID for later uses
        _lobbyId = lobby.Id;

        LobbyeLabel.text = lobby.Id;

        // Heartbeat the lobby every 15 seconds.
        StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

        // Now that RELAY and LOBBY are set...

        // Set Transports data
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            _hostData.IPv4Address,
            _hostData.Port,
            _hostData.AllocationIDBytes,
            _hostData.Key,
            _hostData.ConnectionData);

        //NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

        // Finally start host
        NetworkManager.Singleton.StartHost();
    }

    /* private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        //Your logic here
        bool approve = true;
        bool createPlayerObject = true;
        Debug.Log("connectionData: " + connectionData);

        // Position to spawn the player object at, set to null to use the default position
        Vector3? positionToSpawnAt = Vector3.zero;

        // Rotation to spawn the player object at, set to null to use the default rotation
        Quaternion rotationToSpawnWith = Quaternion.identity;

        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, positionToSpawnAt, rotationToSpawnWith);
    } */

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            Debug.Log("HeartbeatLobbyCoroutine");
            yield return delay;
        }
    }

    private void OnDestroy()
    {
        LobbyService.Instance.DeleteLobbyAsync(_lobbyId);
    }

    #endregion

    /// <summary>
    /// RelayHostData represents the necessary information
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayJoinData
    {
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }

    /// <summary>
    /// RelayHostData represents the necessary information
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }
}
