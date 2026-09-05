using UnityEngine;

namespace BadEngineering.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WeaponHost : MonoBehaviour, IWeaponHost
    {
        [SerializeField] private Transform weaponAttachRoot;
        [SerializeField] private bool includeAttachedWeaponMass;

        private float baseMass;
        private Vector3 baseCenterOfMass;

        public Transform WeaponAttachRoot => weaponAttachRoot != null ? weaponAttachRoot : transform;
        public Rigidbody Body { get; private set; }
        public MonoBehaviour HostBehaviour => this;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            baseMass = Body.mass;
            baseCenterOfMass = Body.centerOfMass;
            RefreshMassProperties();
        }

        public void RefreshMassProperties()
        {
            if (Body == null || !includeAttachedWeaponMass)
            {
                return;
            }

            float totalMass = baseMass;
            Vector3 weightedCenter = baseCenterOfMass * baseMass;
            Weapon[] weapons = WeaponAttachRoot.GetComponentsInChildren<Weapon>(true);
            foreach (Weapon weapon in weapons)
            {
                if (!ReferenceEquals(weapon.Host, this) || weapon.State != WeaponState.Attached)
                {
                    continue;
                }

                totalMass += weapon.WeaponMass;
                weightedCenter += transform.InverseTransformPoint(weapon.transform.position) * weapon.WeaponMass;
            }

            Body.mass = totalMass;
            Body.centerOfMass = weightedCenter / totalMass;
        }
    }
}
