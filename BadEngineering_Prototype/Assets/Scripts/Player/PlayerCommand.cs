using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Player
{
    /// <summary>デバイスから読み取った操作要求。Host側の物理更新はこの値だけを参照する。</summary>
    public struct PlayerCommand : INetworkSerializable
    {
        public Vector2 move, look;
        public float scroll;
        public bool jump, brake, fire, aim, interact, place, drop;
        public int slot;
        public Vector3 rayOrigin, rayDirection, placementPosition;
        public Quaternion placementRotation;
        public ulong vehicleId;
        public bool hasPlacement;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref move); s.SerializeValue(ref look); s.SerializeValue(ref scroll);
            s.SerializeValue(ref jump); s.SerializeValue(ref brake); s.SerializeValue(ref fire); s.SerializeValue(ref aim);
            s.SerializeValue(ref interact); s.SerializeValue(ref place); s.SerializeValue(ref drop); s.SerializeValue(ref slot);
            s.SerializeValue(ref rayOrigin); s.SerializeValue(ref rayDirection);
            s.SerializeValue(ref placementPosition); s.SerializeValue(ref placementRotation);
            s.SerializeValue(ref vehicleId); s.SerializeValue(ref hasPlacement);
        }
    }
}
