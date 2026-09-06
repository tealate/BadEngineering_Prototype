using UnityEngine;

namespace BadEngineering.Weapons
{
    public enum WeaponAimMode { Fixed, Direct, Heavy }
    public enum PlacementBasis { VehicleForward, SurfaceNormal }

    [CreateAssetMenu(menuName = "Bad Engineering/Weapon Settings")]
    public sealed class WeaponSettings : ScriptableObject
    {
        public WeaponAimMode aimMode;
        public PlacementBasis placementBasis;
        public Vector3 rotationOffset;
        [Min(0.02f)] public float cooldown = 0.3f;
        public bool automatic;
        [Min(0.1f)] public float projectileSpeed = 28f;
        [Min(0.1f)] public float projectileLifetime = 8f;
        [Min(0f)] public float recoilImpulse = 65f;
        public GameObject projectilePrefab;
        public bool collideWithWorld;
        public Vector2 yawLimits = new(-90f, 90f);
        public Vector2 pitchLimits = new(-20f, 65f);
        [Min(0f), Tooltip("重砲の旋回速度（度/秒）。")] public float yawSpeed = 20f;
        [Min(0f), Tooltip("重砲の俯仰速度（度/秒）。")] public float pitchSpeed = 12f;
        [Min(0.001f)] public float aimSensitivity = 0.1f;
        public Vector2 zeroDistanceRange = new(10f, 70f);
        [Min(1f)] public float zeroDistanceStep = 5f;
        public Vector3 aimCameraOffset = new(0f, 0.3f, -0.35f);
    }

    public static class BallisticZero
    {
        /// <summary>同じ高さの目標へ到達する低弾道の仰角。距離は水平距離（m）。</summary>
        public static bool TryElevation(float distance, float speed, float gravity, out float degrees)
        {
            degrees = 0f;
            if (distance <= 0f || speed <= 0f || gravity <= 0f) return false;
            float sine = gravity * distance / (speed * speed);
            if (sine > 1f) return false;
            degrees = Mathf.Asin(sine) * Mathf.Rad2Deg * 0.5f;
            return true;
        }
    }
}
