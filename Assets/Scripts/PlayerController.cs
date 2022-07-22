using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{

    public float speed = 10.0f;

    // everyone can read, only owner can write
    public NetworkVariable<Vector3> NetPosition = new NetworkVariable<Vector3>(
        /* default,
        NetworkVariableBase.DefaultReadPerm, // Everyone
        NetworkVariableWritePermission.Owner */);

    // everyone can read, only server can write
    public NetworkVariable<Color> NetColor = new NetworkVariable<Color>(
        default,
        NetworkVariableBase.DefaultReadPerm, // Everyone
        NetworkVariableWritePermission.Owner);
    //NetworkVariableWritePermission.Server);

    void Update()
    {
        //transform.position = NetPosition.Value;
        if (IsOwner)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var horizontal = Input.GetAxis("Horizontal");
                var vertical = Input.GetAxis("Vertical");

                transform.Translate(new Vector3(horizontal, 0, vertical) * (speed * Time.deltaTime));
                NetPosition.Value = new Vector3(horizontal, 0, vertical) * (speed * Time.deltaTime);
            }
            else
            {
                SubmitPositionRequestServerRpc();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("C");
                if (NetworkManager.Singleton.IsServer)
                {
                    ChangeColorServerRpc();
                }
                else
                {
                    NetColor.Value = Random.ColorHSV();
                }
                //NetColor.Value = Random.ColorHSV();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                //Move();
                Debug.Log("M");
                NetPosition.Value = GetRandomPositionOnPlane();
            }


        }
        else
        {
            transform.Translate(NetPosition.Value);
        }
    }

    public void Move()
    {
        Debug.Log("M");
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("S");
            var randomPosition = GetRandomPositionOnPlane();
            Debug.Log(randomPosition);
            transform.position = randomPosition;
            NetPosition.Value = randomPosition;
        }
        else
        {
            Debug.Log("C");
            SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc]
    void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(horizontal, 0, vertical) * (speed * Time.deltaTime));
    }

    static Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
    }

    public override void OnNetworkSpawn()
    {
        NetColor.OnValueChanged += OnColorChanged;
        //NetPosition.OnValueChanged += OnPositionChanged;
        if (IsOwner)
        {
            NetColor.Value = Random.ColorHSV();
        }
    }

    public override void OnNetworkDespawn()
    {
        NetColor.OnValueChanged -= OnColorChanged;
        //NetPosition.OnValueChanged -= OnPositionChanged;
    }

    public void OnColorChanged(Color previous, Color current)
    {
        //Debug.Log(new Vector3(current.r, current.g, current.b));
        // update materials etc.
        Debug.Log("Color changed");
        var cubeRenderer = GetComponent<Renderer>();
        cubeRenderer.material.SetColor("_Color", current);
    }
    public void OnPositionChanged(Vector3 previous, Vector3 current)
    {
        // update materials etc.
        /* if (previous != current)
        { */
        gameObject.transform.Translate(current);
        /* } */
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeColorServerRpc()
    {
        // this will cause a replication over the network
        // and ultimately invoke `OnValueChanged` on receivers
        Debug.Log("Color replication");
        NetColor.Value = Random.ColorHSV();
    }
}
