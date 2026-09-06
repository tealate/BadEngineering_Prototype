using BadEngineering.Player;
using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Network
{
    /// <summary>入力要求の送信者をPlayerのNetwork Ownerで検証する。Physics Authorityは常にServer。</summary>
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private GameObject starterWeapon;
        private bool ownerConnected;
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ownerConnected = OwnerClientId == NetworkManager.LocalClientId;
                NetworkManager.OnClientConnectedCallback += OnOwnerConnected;
                transform.position = new Vector3((int)(OwnerClientId % 4) * 2f, 1.1f, 0f);
                StartCoroutine(GiveStarterAfterSynchronization());
            }
            foreach (Camera camera in GetComponentsInChildren<Camera>()) camera.enabled = IsOwner;
            foreach (AudioListener listener in GetComponentsInChildren<AudioListener>()) listener.enabled = IsOwner;
            GetComponent<PlayerViewController>().enabled = IsOwner;
            var avatar = transform.Find("Player Visual");
            if (avatar != null && IsOwner) avatar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            if (IsOwner) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        private System.Collections.IEnumerator GiveStarterAfterSynchronization()
        {
            // PlayerのOnNetworkSpawn内で追加Spawnすると、初期Scene同期と生成通知が重複する。
            yield return null;
            while (IsSpawned && !ownerConnected)
                yield return null;
            if (!IsSpawned || starterWeapon == null) yield break;
            GameObject weapon = Instantiate(starterWeapon);
            weapon.GetComponent<NetworkObject>().Spawn();
            GetComponent<PlayerWeaponSlots>().AddOwnedWeapon(weapon.GetComponent<Weapons.Weapon>());
        }
        private void OnOwnerConnected(ulong clientId)
        {
            if (clientId == OwnerClientId) ownerConnected = true;
        }

        public void Submit(PlayerCommand command)
        {
            if (!IsOwner) return;
            if (IsServer) GetComponent<FirstPersonRigidbodyController>().ApplyCommand(command);
            else SubmitRpc(command);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitRpc(PlayerCommand command)
        {
            if (!float.IsFinite(command.move.x) || !float.IsFinite(command.move.y) ||
                !float.IsFinite(command.look.x) || !float.IsFinite(command.look.y) || !float.IsFinite(command.scroll)) return;
            command.look = Vector2.ClampMagnitude(command.look, 500f);
            GetComponent<FirstPersonRigidbodyController>().ApplyCommand(command);
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.OnClientConnectedCallback -= OnOwnerConnected;
            if (!IsServer) return;
            GetComponent<Vehicle.VehicleStationUser>()?.TryLeaveStation();
            var slots = GetComponent<PlayerWeaponSlots>();
            for (int i = 0; i < slots.SlotCount; i++)
            {
                slots.SelectSlot(i);
                slots.DropSelectedWeapon();
            }
        }
    }
}
