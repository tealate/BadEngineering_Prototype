using UnityEngine;
namespace BadEngineering.Vehicle
{
    [CreateAssetMenu(fileName = "New Tire", menuName = "Bad Engineering/Vehicle/Tire Definition")]
    public sealed class TireDefinition : ScriptableObject
    {
        [SerializeField, Min(.05f)] float radius = .48f;
        [SerializeField, Min(0f)] float mass = 18f;
        [SerializeField, Min(0f), Tooltip("Maximum lateral force as a multiple of the wheel's current vertical load.")]
        float grip = 1.1f;
        [SerializeField, Min(0f)] float sidewallFriction = 0.25f;
        [SerializeField, Range(0f, 1f), Tooltip("接触法線と車軸の内積がこの値以上なら側面接触。")] float sidewallThreshold = 0.65f;
        public float TreadFriction => grip;
        public float SidewallFriction => sidewallFriction;
        public float FrictionForNormal(Vector3 normal, Vector3 axle) => Mathf.Abs(Vector3.Dot(normal.normalized, axle.normalized)) >= sidewallThreshold ? sidewallFriction : grip;
        [SerializeField, Min(0f)] float spring = 22000f, damping = 2800f, drivePower = 6500f, brakePower = 9000f;
        [SerializeField, Range(0f, 60f)] float maximumSteeringAngle = 30f;
        [SerializeField, Min(.01f)] float suspensionLength = .7f;
        [SerializeField, Min(1f)] float maximumSuspensionLoad = 3f;
        [SerializeField] GameObject visualPrefab;
        public float Radius => radius; public float Mass => mass; public float Grip => grip;
        public float Spring => spring; public float Damping => damping; public float DrivePower => drivePower;
        public float BrakePower => brakePower; public float MaximumSteeringAngle => maximumSteeringAngle;
        public float SuspensionLength => suspensionLength; public GameObject VisualPrefab => visualPrefab;
        public float MaximumSuspensionLoad => maximumSuspensionLoad;
    }
}
