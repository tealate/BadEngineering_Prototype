using UnityEngine;

namespace BadEngineering.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WeaponHost : MonoBehaviour, IWeaponHost
    {
        [SerializeField] private Transform weaponAttachRoot;

        public Transform WeaponAttachRoot => weaponAttachRoot != null ? weaponAttachRoot : transform;
        public Rigidbody Body { get; private set; }
        public MonoBehaviour HostBehaviour => this;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }
    }
}
