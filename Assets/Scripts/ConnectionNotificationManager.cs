using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Only attach this example component to the NetworkManager GameObject.
/// This will provide you with a single location to register for client 
/// connect and disconnect events.  
/// </summary>
public class ConnectionNotificationManager : MonoBehaviour
{

    public static ConnectionNotificationManager Singleton { get; internal set; }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected
    }

    public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;

    private void Awake()
    {
        if (Singleton != null)
        {
            // As long as you aren't creating multiple NetworkManager instances, throw an exception.
            // (***the current position of the callstack will stop here***)
            throw new Exception($"Detected more than one instance of {nameof(ConnectionNotificationManager)}! " +
                $"Do you have more than one component attached to a {nameof(GameObject)}");
        }
        Singleton = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnDestroy()
    {
        // Since the NetworkManager could potentially be destroyed before this component, only 
        // remove the subscriptions if the singleton still exists.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
        // Testing
        if(NetworkManager.Singleton.IsServer){
            Debug.Log("Players list: ");
            for(int i = 0; NetworkManager.Singleton.ConnectedClientsList.Count > i; i++){
                Debug.Log("Player: " + NetworkManager.Singleton.ConnectedClientsList[i].ClientId);
            }
        }
        
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Disconnected);
    }
}