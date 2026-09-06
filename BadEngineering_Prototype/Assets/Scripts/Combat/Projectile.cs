using UnityEngine;

namespace BadEngineering.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class Projectile : MonoBehaviour
    {
        private float damage;
        private GameObject sourceRoot;

        public void Initialize(float projectileDamage, float lifetime, GameObject source)
        {
            damage = projectileDamage;
            sourceRoot = source != null ? source.transform.root.gameObject : null;
            if (sourceRoot != null)
            {
                Collider projectileCollider = GetComponent<Collider>();
                Collider[] sourceColliders = sourceRoot.GetComponentsInChildren<Collider>();
                foreach (Collider sourceCollider in sourceColliders)
                {
                    Physics.IgnoreCollision(projectileCollider, sourceCollider, true);
                }
            }
            expiresAt = Time.time + lifetime;
        }
        private float expiresAt = float.PositiveInfinity;
        private void Update()
        {
            if (BadEngineering.Network.GameplayAuthority.CanSimulate && Time.time >= expiresAt) Remove();
        }
        private void Remove()
        {
            var networkObject = GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
            else Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!BadEngineering.Network.GameplayAuthority.CanSimulate) return;
            if (sourceRoot != null && collision.transform.root.gameObject == sourceRoot)
            {
                return;
            }

            // 今回はHitと消滅だけを扱う。HP・Damageはテスト範囲外。
            Remove();
        }
    }
}
