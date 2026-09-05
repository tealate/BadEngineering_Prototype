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
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (sourceRoot != null && collision.transform.root.gameObject == sourceRoot)
            {
                return;
            }

            MonoBehaviour[] behaviours = collision.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable)
                {
                    damageable.ApplyDamage(damage);
                    break;
                }
            }
            Destroy(gameObject);
        }
    }
}
