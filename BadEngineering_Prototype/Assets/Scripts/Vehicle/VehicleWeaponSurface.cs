using BadEngineering.Interaction;
using BadEngineering.Player;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class VehicleWeaponSurface : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponHost vehicleHost;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.05f;

        private void Awake()
        {
            if (vehicleHost == null)
            {
                vehicleHost = GetComponentInParent<WeaponHost>();
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            PlayerWeaponSlots slots = interactor != null ? interactor.GetComponent<PlayerWeaponSlots>() : null;
            return vehicleHost != null && slots != null && slots.EquippedWeapon != null &&
                slots.EquippedWeapon.State == WeaponState.Held && slots.EquippedWeapon.Owner == slots;
        }

        public bool TryInteract(GameObject interactor)
        {
            return TryAttach(interactor, transform.position, transform.up);
        }

        public bool TryAttach(GameObject interactor, Vector3 hitPoint, Vector3 hitNormal)
        {
            PlayerWeaponSlots slots = interactor != null ? interactor.GetComponent<PlayerWeaponSlots>() : null;
            Weapon weapon = slots?.EquippedWeapon;
            if (!CanInteract(interactor))
            {
                return false;
            }

            Vector3 forward = Vector3.ProjectOnPlane(interactor.transform.forward, hitNormal).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, hitNormal).normalized;
            }
            Quaternion rotation = Quaternion.LookRotation(forward, hitNormal);
            return weapon.AttachTo(vehicleHost, hitPoint + hitNormal * surfaceOffset, rotation, WeaponState.Attached);
        }

        public WeaponHost Host => vehicleHost != null ? vehicleHost : GetComponentInParent<WeaponHost>();

        public bool ConfirmPlacement(GameObject interactor, Vector3 position, Quaternion rotation)
        {
            if (!CanInteract(interactor)) return false;
            return interactor.GetComponent<PlayerWeaponSlots>().EquippedWeapon.AttachTo(
                Host, position, rotation, WeaponState.Attached);
        }
    }
}
