using System;
using UnityEngine;

namespace BadEngineering.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        public float Current { get; private set; }
        public float Maximum => maximumHealth;
        public bool IsDead => Current <= 0f;
        public event Action<Health> Died;

        private void Awake()
        {
            Current = maximumHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            if (!IsDead)
            {
                return;
            }

            Died?.Invoke(this);
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
