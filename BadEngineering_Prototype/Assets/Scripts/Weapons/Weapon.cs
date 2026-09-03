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

            Host = host;
            State = state;
            SetPhysicsEnabled(false);
            transform.SetParent(host.WeaponAttachRoot, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            gameObject.SetActive(true);
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
            Host = null;
            State = WeaponState.Dropped;
            previousOwner?.RemoveOwnedWeapon(this);

            transform.SetParent(null, true);
            transform.position = worldPosition;
            gameObject.SetActive(true);
            SetPhysicsEnabled(true);
            if (weaponBody != null)
            {
                weaponBody.linearVelocity = inheritedVelocity;
            }
            OnStateChanged();
        }

        public bool PickUp(PlayerWeaponSlots newOwner)
        {
            return State == WeaponState.Dropped && newOwner != null && newOwner.AddOwnedWeapon(this);
        }

        public bool CanInteract(GameObject interactor) =>
            State == WeaponState.Dropped &&
            interactor != null &&
            interactor.GetComponent<PlayerWeaponSlots>() != null;

        public bool TryInteract(GameObject interactor) =>
            interactor != null && PickUp(interactor.GetComponent<PlayerWeaponSlots>());

        public virtual void PrimaryPressed() { }
        public virtual void PrimaryReleased() { }
        public virtual void SecondaryPressed() { }
        public virtual void SecondaryReleased() { }
        protected virtual void OnStateChanged() { }

        private void SetPhysicsEnabled(bool enabled)
        {
            if (weaponBody == null && enabled)
            {
                weaponBody = gameObject.AddComponent<Rigidbody>();
                weaponBody.mass = weaponMass;
                weaponBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            if (weaponBody != null)
            {
                weaponBody.isKinematic = !enabled;
                weaponBody.detectCollisions = enabled;
            }

            if (weaponColliders == null || weaponColliders.Length == 0)
            {
                weaponColliders = GetComponentsInChildren<Collider>(true);
            }

            foreach (Collider weaponCollider in weaponColliders)
            {
                weaponCollider.enabled = enabled;
            }
        }
    }
}
