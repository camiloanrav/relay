using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<PlayerNetworkData> _netState = new(writePerm: NetworkVariableWritePermission.Owner);
    private Vector2 _vel;
    private float _vel2;
    private float _interpolationtime = 0.1f;

    // Update is called once per frame
    void Update()
    {
        if (!IsSpawned){
            return;
        }
        if(IsOwner){
            _netState.Value = new PlayerNetworkData() {
                Position = transform.position,
                Scale = transform.localScale
            };
        }else{
            transform.position = Vector2.SmoothDamp(transform.position, _netState.Value.Position, ref _vel, _interpolationtime);
            transform.localScale = new Vector2(Mathf.SmoothDamp(transform.localScale.x, _netState.Value.Scale.x, ref _vel2, 0.001f), 1);
        }
        /* if(IsOwner){
            _netState.Value = new PlayerNetworkData() {
                Position = transform.position,
                Scale = transform.localScale
            };
        }else{
            transform.position = _netState.Value.Position;
            transform.localScale = _netState.Value.Scale;
        } */
    }

    struct PlayerNetworkData : INetworkSerializable
    {
        private float _x, _y;
        private sbyte _xScale;

        internal Vector2 Position {
            get => new Vector2(_x, _y);
            set {
                _x = value.x;
                _y = value.y;
            }
        }

        internal Vector2 Scale {
            get => new Vector3(_xScale, 1);
            set => _xScale = (sbyte)value.x;
            
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _x);
            serializer.SerializeValue(ref _y);

            serializer.SerializeValue(ref _xScale);
        }
    }
}
