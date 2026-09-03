using BadEngineering.Interaction;
using BadEngineering.Player;
using UnityEngine;

namespace BadEngineering.Weapons
{
    public enum WeaponState
    {
        Held,
        Attached,
        Dropped
    }

    [DisallowMultipleComponent]
    public abstract class Weapon : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Weapon";
        [SerializeField, Min(0.01f)] private float weaponMass = 8f;

        private Rigidbody weaponBody;
        private Collider[] weaponColliders;
        private bool[] originalTriggerStates;

        public PlayerWeaponSlots Owner { get; private set; }
        public IWeaponHost Host { get; private set; }
        public WeaponState State { get; private set; } = WeaponState.Dropped;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float WeaponMass => weaponMass;
        protected Rigidbody HostBody => Host?.Body;
        protected bool CanFire => Owner != null && State != WeaponState.Dropped;

        protected virtual void Awake()
        {
            weaponBody = GetComponent<Rigidbody>();
            weaponColliders = GetComponentsInChildren<Collider>(true);
            originalTriggerStates = new bool[weaponColliders.Length];
            for (int i = 0; i < weaponColliders.Length; i++)
            {
                originalTriggerStates[i] = weaponColliders[i].isTrigger;
            }
        }

        public void SetOwner(PlayerWeaponSlots owner)
        {
            Owner = owner;
        }

        public void SetSelected(bool selected)
        {
            if (State == WeaponState.Held)
            {
                gameObject.SetActive(selected);
            }
        }

        public bool AttachTo(IWeaponHost host, Vector3 worldPosition, Quaternion worldRotation, WeaponState state)
        {
            if (host == null || state == WeaponState.Dropped)
            {
                return false;
            }

            WeaponHost previousHost = Host?.HostBehaviour as WeaponHost;
            Host = host;
            State = state;
            SetPhysicalMode(state);
            transform.SetParent(host.WeaponAttachRoot, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            gameObject.SetActive(true);
            OnStateChanged();
            previousHost?.RefreshMassProperties();
            (Host.HostBehaviour as WeaponHost)?.RefreshMassProperties();
            return true;
        }

        public bool HoldByOwner()
        {
            IWeaponHost playerHost = Owner != null ? Owner.GetComponent<IWeaponHost>() : null;
            if (playerHost == null)
            {
                return false;
            }

            bool attached = AttachTo(
                playerHost,
                playerHost.WeaponAttachRoot.position,
                playerHost.WeaponAttachRoot.rotation,
                WeaponState.Held);
            SetSelected(Owner.EquippedWeapon == this);
            return attached;
        }

        public void Drop(Vector3 worldPosition, Vector3 inheritedVelocity)
        {
            PlayerWeaponSlots previousOwner = Owner;
            Owner = null;
            WeaponHost previousHost = Host?.HostBehaviour as WeaponHost;
            Host = null;
            State = WeaponState.Dropped;
            previousOwner?.RemoveOwnedWeapon(this);

            transform.SetParent(null, true);
            transform.position = worldPosition;
            gameObject.SetActive(true);
            SetPhysicalMode(WeaponState.Dropped);
            if (weaponBody != null)
            {
                weaponBody.linearVelocity = inheritedVelocity;
            }
            OnStateChanged();
            previousHost?.RefreshMassProperties();
        }

        public bool PickUp(PlayerWeaponSlots newOwner)
        {
            return State == WeaponState.Dropped && newOwner != null && newOwner.AddOwnedWeapon(this);
        }

        public bool CanInteract(GameObject interactor)
        {
            PlayerWeaponSlots slots = interactor != null ? interactor.GetComponent<PlayerWeaponSlots>() : null;
            return slots != null &&
                   (State == WeaponState.Dropped || State == WeaponState.Attached && Owner == slots);
        }

        public bool TryInteract(GameObject interactor)
        {
            PlayerWeaponSlots slots = interactor != null ? interactor.GetComponent<PlayerWeaponSlots>() : null;
            if (State == WeaponState.Attached && Owner == slots)
            {
                return HoldByOwner();
            }
            return PickUp(slots);
        }

        public virtual void PrimaryPressed() { }
        public virtual void PrimaryReleased() { }
        public virtual void SecondaryPressed() { }
        public virtual void SecondaryReleased() { }
        protected virtual void OnStateChanged() { }

        private void SetPhysicalMode(WeaponState state)
        {
            bool dropped = state == WeaponState.Dropped;
            if (weaponBody == null && dropped)
            {
                weaponBody = gameObject.AddComponent<Rigidbody>();
                weaponBody.mass = weaponMass;
                weaponBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            if (weaponBody != null)
            {
                weaponBody.isKinematic = !dropped;
                weaponBody.detectCollisions = dropped;
            }

            if (weaponColliders == null || weaponColliders.Length == 0)
            {
                weaponColliders = GetComponentsInChildren<Collider>(true);
                originalTriggerStates = new bool[weaponColliders.Length];
                for (int i = 0; i < weaponColliders.Length; i++)
                {
                    originalTriggerStates[i] = weaponColliders[i].isTrigger;
                }
            }

            for (int i = 0; i < weaponColliders.Length; i++)
            {
                weaponColliders[i].enabled = state != WeaponState.Held;
                weaponColliders[i].isTrigger = state == WeaponState.Attached || originalTriggerStates[i];
            }
        }
    }
}
