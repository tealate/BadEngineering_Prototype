using UnityEngine;

namespace BadEngineering.Weapons
{
    public abstract class Weapon : MonoBehaviour
    {
        [SerializeField] private string displayName = "Weapon";

        protected Transform OwnerTransform { get; private set; }
        protected Rigidbody OwnerBody { get; private set; }
        protected bool IsHeld { get; private set; }
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public void SetOwner(Transform ownerTransform, Rigidbody ownerBody)
        {
            OwnerTransform = ownerTransform;
            OwnerBody = ownerBody;
        }

        public void SetHeld(bool held)
        {
            IsHeld = held;
            gameObject.SetActive(held);
            OnHeldChanged(held);
        }

        public virtual void PrimaryPressed()
        {
        }

        public virtual void PrimaryReleased()
        {
        }

        public virtual void SecondaryPressed()
        {
        }

        public virtual void SecondaryReleased()
        {
        }

        protected virtual void OnHeldChanged(bool held)
        {
        }
    }
}
