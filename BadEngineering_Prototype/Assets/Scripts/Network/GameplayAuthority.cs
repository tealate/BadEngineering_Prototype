using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Network
{
    public static class GameplayAuthority
    {
        public static bool CanSimulate => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;
        public static bool IsLocal(GameObject player)
        {
            var actor = player.GetComponent<NetworkObject>();
            return actor == null || !actor.IsSpawned || actor.IsOwner;
        }
    }
}
