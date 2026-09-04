using UnityEngine;
namespace BadEngineering.Vehicle
{
    [CreateAssetMenu(fileName = "New Tire", menuName = "Bad Engineering/Vehicle/Tire Definition")]
    public sealed class TireDefinition : ScriptableObject
    {
        [SerializeField, Min(.05f)] float radius = .48f;
        [SerializeField, Min(0f)] float mass = 18f, grip = 6f, spring = 22000f, damping = 2800f, drivePower = 6500f, brakePower = 9000f;
        [SerializeField, Range(0f, 60f)] float maximumSteeringAngle = 30f;
        [SerializeField, Min(.01f)] float suspensionLength = .55f;
        [SerializeField] GameObject visualPrefab;
        public float Radius => radius; public float Mass => mass; public float Grip => grip;
        public float Spring => spring; public float Damping => damping; public float DrivePower => drivePower;
        public float BrakePower => brakePower; public float MaximumSteeringAngle => maximumSteeringAngle;
        public float SuspensionLength => suspensionLength; public GameObject VisualPrefab => visualPrefab;
    }
}
