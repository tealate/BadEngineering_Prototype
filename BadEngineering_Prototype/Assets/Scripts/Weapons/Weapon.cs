using System.Collections.Generic;
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
        private readonly Dictionary<Collider, bool> originalEnabledStates = new Dictionary<Collider, bool>();
        private readonly Dictionary<Collider, bool> originalTriggerStates = new Dictionary<Collider, bool>();

        public PlayerWeaponSlots Owner { get; private set; }
        public IWeaponHost Host { get; private set; }
        public WeaponState State { get; private set; } = WeaponState.Dropped;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float WeaponMass => weaponMass;
        protected Rigidbody HostBody => Host?.Body;
        protected bool CanFire => Owner != null && State != WeaponState.Dropped;

        protected virtual void Awake()
        {
            CachePhysicalComponents();
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
            if (host == null || host.WeaponAttachRoot == null || Owner == null ||
                state != WeaponState.Held && state != WeaponState.Attached)
            {
                return false;
            }

            IWeaponHost ownerHost = Owner.GetComponent<IWeaponHost>();
            if (state == WeaponState.Held && !ReferenceEquals(host, ownerHost))
            {
                return false;
            }

            WeaponHost previousHost = Host?.HostBehaviour as WeaponHost;
            SetPhysicalMode(state);
            transform.SetParent(host.WeaponAttachRoot, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            Host = host;
            State = state;
            gameObject.SetActive(true);
            previousHost?.RefreshMassProperties();
            (Host.HostBehaviour as WeaponHost)?.RefreshMassProperties();
            OnStateChanged();
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

        protected virtual void OnDestroy()
        {
            PlayerWeaponSlots previousOwner = Owner;
            WeaponHost previousHost = Host?.HostBehaviour as WeaponHost;
            Owner = null;
            Host = null;
            State = WeaponState.Dropped;
            previousOwner?.RemoveOwnedWeapon(this);
            previousHost?.RefreshMassProperties();
        }

        private void SetPhysicalMode(WeaponState state)
        {
            bool dropped = state == WeaponState.Dropped;
            CachePhysicalComponents();
            if (weaponBody == null && dropped)
            {
                weaponBody = gameObject.AddComponent<Rigidbody>();
            }

            if (weaponBody != null)
            {
                weaponBody.mass = weaponMass;
                if (!dropped)
                {
                    weaponBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
                weaponBody.isKinematic = !dropped;
                weaponBody.detectCollisions = dropped;
                if (dropped)
                {
                    weaponBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
            }

            for (int i = 0; i < weaponColliders.Length; i++)
            {
                Collider weaponCollider = weaponColliders[i];
                if (weaponCollider == null)
                {
                    continue;
                }

                weaponCollider.enabled = state == WeaponState.Held
                    ? false
                    : originalEnabledStates[weaponCollider];
                weaponCollider.isTrigger = state == WeaponState.Attached || originalTriggerStates[weaponCollider];
            }
        }

        private void CachePhysicalComponents()
        {
            weaponBody = GetComponent<Rigidbody>();
            weaponColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < weaponColliders.Length; i++)
            {
                Collider weaponCollider = weaponColliders[i];
                if (!originalEnabledStates.ContainsKey(weaponCollider))
                {
                    originalEnabledStates.Add(weaponCollider, weaponCollider.enabled);
                    originalTriggerStates.Add(weaponCollider, weaponCollider.isTrigger);
                }
            }
        }
    }
}
