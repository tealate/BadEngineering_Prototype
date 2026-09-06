using UnityEngine;

namespace BadEngineering.Vehicle
{
    /// <summary>Prefabと物理設定で共通使用するPrototypeの衝突区分。</summary>
    public static class PrototypeCollision
    {
        public const int Player = 6, Vehicle = 7, Weapon = 8, MountedTire = 9, TireItem = 10;
        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }
    }
}
