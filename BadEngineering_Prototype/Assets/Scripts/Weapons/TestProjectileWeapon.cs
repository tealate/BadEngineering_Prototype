using UnityEngine;
using BadEngineering.Player;
using BadEngineering.Combat;

namespace BadEngineering.Weapons
{
    public sealed class TestProjectileWeapon : Weapon
    {
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0f)] private float projectileSpeed = 28f;
        [SerializeField, Min(0f)] private float projectileLifetime = 3f;
        [SerializeField, Min(0f)] private float recoilImpulse = 65f;
        [SerializeField, Min(0f)] private float projectileDamage = 25f;

        public override void PrimaryPressed()
        {
            if (!CanFire || muzzle == null)
            {
                return;
            }

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "TestProjectile";
            projectile.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
            projectile.transform.localScale = Vector3.one * 0.12f;

            Rigidbody projectileBody = projectile.AddComponent<Rigidbody>();
            projectileBody.mass = 0.1f;
            projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileBody.linearVelocity = muzzle.forward * projectileSpeed;
            projectile.AddComponent<Projectile>().Initialize(projectileDamage, projectileLifetime, gameObject);

            Vector3 recoil = -muzzle.forward * recoilImpulse;
            var controller = Host?.HostBehaviour != null
                ? Host.HostBehaviour.GetComponent<FirstPersonRigidbodyController>()
                : null;
            if (controller != null)
            {
                controller.ApplyRecoil(recoil, muzzle.position);
            }
            else
            {
                HostBody?.AddForceAtPosition(recoil, muzzle.position, ForceMode.Impulse);
            }
        }
    }
}
