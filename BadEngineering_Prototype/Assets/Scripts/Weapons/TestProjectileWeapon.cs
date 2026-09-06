using BadEngineering.Combat;
using BadEngineering.Player;
using UnityEngine;

namespace BadEngineering.Weapons
{
    /// <summary>共通射撃と設定による固定砲・可動砲・重砲。</summary>
    public sealed class TestProjectileWeapon : Weapon
    {
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0f)] private float projectileSpeed = 28f;
        [SerializeField, Min(0f)] private float projectileLifetime = 3f;
        [SerializeField, Min(0f)] private float recoilImpulse = 65f;
        private bool triggerHeld;
        private float nextShotTime;
        private Quaternion installedRotation;
        private float targetYaw, targetPitch, currentYaw, currentPitch;
        public bool IsAiming { get; private set; }
        public float ZeroDistance { get; private set; } = 30f;
        public bool ZeroReachable { get; private set; } = true;
        public Transform Muzzle => muzzle;
        protected override void OnStateChanged()
        {
            installedRotation = transform.localRotation;
            targetYaw = targetPitch = currentYaw = currentPitch = 0f;
        }
        public override void PrimaryPressed() { triggerHeld = true; TryFire(); }
        public override void PrimaryReleased() => triggerHeld = false;
        public override void SecondaryPressed() => IsAiming = CanFire;
        public override void SecondaryReleased() => IsAiming = false;
        private void Update()
        {
            if (!BadEngineering.Network.GameplayAuthority.CanSimulate) return;
            if (triggerHeld && Settings != null && Settings.automatic) TryFire();
            UpdateAim(Time.deltaTime);
        }
        /// <summary>デバイス非依存のAim要求。正のYは上方向。</summary>
        public void ApplyAim(Vector2 delta, float scroll)
        {
            if (!IsAiming || Settings == null || Settings.aimMode == WeaponAimMode.Fixed) return;
            targetYaw = Mathf.Clamp(targetYaw + delta.x * Settings.aimSensitivity, Settings.yawLimits.x, Settings.yawLimits.y);
            targetPitch = Mathf.Clamp(targetPitch + delta.y * Settings.aimSensitivity, Settings.pitchLimits.x, Settings.pitchLimits.y);
            if (Settings.aimMode == WeaponAimMode.Heavy && scroll != 0f)
                ZeroDistance = Mathf.Clamp(ZeroDistance + Mathf.Sign(scroll) * Settings.zeroDistanceStep,
                    Settings.zeroDistanceRange.x, Settings.zeroDistanceRange.y);
        }
        private void UpdateAim(float seconds)
        {
            if (Settings == null || Settings.aimMode == WeaponAimMode.Fixed || State == WeaponState.Dropped) return;
            float elevation = 0f;
            if (Settings.aimMode == WeaponAimMode.Heavy)
            {
                ZeroDistance = Mathf.Clamp(ZeroDistance, Settings.zeroDistanceRange.x, Settings.zeroDistanceRange.y);
                ZeroReachable = BallisticZero.TryElevation(ZeroDistance, Settings.projectileSpeed, Physics.gravity.magnitude, out elevation);
            }
            float pitch = Mathf.Clamp(targetPitch + elevation, Settings.pitchLimits.x, Settings.pitchLimits.y);
            bool heavy = Settings.aimMode == WeaponAimMode.Heavy;
            currentYaw = heavy ? Mathf.MoveTowards(currentYaw, targetYaw, Settings.yawSpeed * seconds) : targetYaw;
            currentPitch = heavy ? Mathf.MoveTowards(currentPitch, pitch, Settings.pitchSpeed * seconds) : pitch;
            transform.localRotation = installedRotation * Quaternion.Euler(-currentPitch, currentYaw, 0f);
        }
        public bool TryFire()
        {
            if (!BadEngineering.Network.GameplayAuthority.CanSimulate) return false;
            if (!CanFire || muzzle == null || Time.time < nextShotTime) return false;
            nextShotTime = Time.time + (Settings != null ? Settings.cooldown : 0.3f);
            float speed = Settings != null ? Settings.projectileSpeed : projectileSpeed;
            float lifetime = Settings != null ? Settings.projectileLifetime : projectileLifetime;
            GameObject shot;
            if (Settings != null && Settings.projectilePrefab != null)
                shot = Instantiate(Settings.projectilePrefab, muzzle.position, muzzle.rotation);
            else
            {
                shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shot.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
                shot.transform.localScale = Vector3.one * 0.12f;
                shot.AddComponent<Rigidbody>();
                shot.AddComponent<Projectile>();
            }
            shot.name = "Projectile";
            Rigidbody shotBody = shot.GetComponent<Rigidbody>();
            shotBody.mass = 0.1f;
            shotBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            shotBody.linearVelocity = muzzle.forward * speed;
            shot.GetComponent<Projectile>().Initialize(0f, lifetime, gameObject);
            var networkObject = shot.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObject != null && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
                networkObject.Spawn();
            Vector3 recoil = -muzzle.forward * (Settings != null ? Settings.recoilImpulse : recoilImpulse);
            var player = Host?.HostBehaviour != null ? Host.HostBehaviour.GetComponent<FirstPersonRigidbodyController>() : null;
            if (player != null) player.ApplyRecoil(recoil, muzzle.position);
            else HostBody?.AddForceAtPosition(recoil, muzzle.position, ForceMode.Impulse);
            return true;
        }
        public void SetReplicaAim(bool aim, float zero) { IsAiming = aim; ZeroDistance = zero; }
    }
}
