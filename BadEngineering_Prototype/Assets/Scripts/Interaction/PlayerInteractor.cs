using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Interaction
{
    [RequireComponent(typeof(VehicleStationUser))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        private VehicleStationUser stationUser;
        private void Awake()
        {
            stationUser = GetComponent<VehicleStationUser>();
            if (interactionCamera == null) interactionCamera = GetComponentInChildren<Camera>();
        }
        public bool Interact() => InteractRay(interactionCamera.transform.position, interactionCamera.transform.forward);

        /// <summary>Hostが照準の到達範囲を検証し、取得または乗降を確定する。</summary>
        public bool InteractRay(Vector3 origin, Vector3 direction)
        {
            if (stationUser.IsUsingStation) return stationUser.TryLeaveStation();
            if (!TryHit(origin, direction, out RaycastHit hit)) return false;
            var weapon = hit.collider.GetComponentInParent<Weapon>();
            if (weapon != null) return weapon.State == WeaponState.Dropped && weapon.PickUp(GetComponent<PlayerWeaponSlots>());
            var part = hit.collider.GetComponentInParent<TireItem>();
            if (part != null) return part.TryInteract(gameObject);
            var seat = hit.collider.GetComponentInParent<VehicleInteractionPoint>();
            return seat != null && seat.TryInteract(gameObject);
        }

        public bool PlaceCommand(PlayerCommand command)
        {
            if (stationUser.IsDriving) return false;
            if (TryHit(command.rayOrigin, command.rayDirection, out RaycastHit hit))
            {
                var weapon = hit.collider.GetComponentInParent<Weapon>();
                if (weapon != null)
                    return weapon.State == WeaponState.Attached && weapon.Owner == GetComponent<PlayerWeaponSlots>() && weapon.HoldByOwner();
                var part = GetComponent<PlayerWeaponSlots>().EquippedItem as TireItem;
                if (part != null)
                {
                    var vehicle = hit.collider.GetComponentInParent<VehiclePhysicsController>();
                    return vehicle != null && part.Install(vehicle.Movement as WheelSystem);
                }
            }
            if (!command.hasPlacement) return false;
            VehiclePhysicsController target = null;
            var network = Unity.Netcode.NetworkManager.Singleton;
            if (network != null && network.IsListening)
            {
                if (network.SpawnManager.SpawnedObjects.TryGetValue(command.vehicleId, out var obj))
                    target = obj.GetComponent<VehiclePhysicsController>();
            }
            else target = GetComponent<WeaponPlacementController>()?.Surface?.Host.GetComponent<VehiclePhysicsController>();
            if (target == null) return false;
            var surface = target.GetComponentInChildren<VehicleWeaponSurface>();
            if (surface == null) return false;
            Vector3 position = target.transform.TransformPoint(command.placementPosition);
            if (!IsFinite(position) || !IsFinite(command.placementRotation)) return false;
            if (Vector3.Distance(position, transform.position) > interactionDistance + 1f) return false;
            // 厳密な干渉判定は行わず、車体表面から離れた不正な位置だけ拒否する。
            if (Vector3.Distance(surface.GetComponent<Collider>().ClosestPoint(position), position) > 0.2f) return false;
            return surface.ConfirmPlacement(gameObject, position, target.transform.rotation * command.placementRotation.normalized);
        }

        private bool TryHit(Vector3 origin, Vector3 direction, out RaycastHit hit)
        {
            hit = default;
            if (!IsFinite(origin) || !IsFinite(direction) || Vector3.Distance(origin, transform.position) > 2.5f) return false;
            return Physics.Raycast(origin, direction.normalized, out hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);
        }
        private static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
