using UnityEngine;
namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class WheelPoint : MonoBehaviour
    {
        [SerializeField] bool canSteer, canDrive = true;
        [SerializeField] Transform visualRoot;
        [SerializeField] LayerMask groundMask = ~0;
        public bool CanDrive => canDrive; public bool IsGrounded { get; private set; }
        public Vector3 WheelCenter { get; private set; }
        public void Simulate(Rigidbody body, TireDefinition tire, VehicleInput input, int drivenCount)
        {
            Vector3 down = -transform.up; float rayLength = tire.SuspensionLength + tire.Radius;
            IsGrounded = Physics.Raycast(transform.position, down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore);
            float centerDistance = IsGrounded ? Mathf.Max(0f, hit.distance - tire.Radius) : tire.SuspensionLength;
            WheelCenter = transform.position + down * centerDistance; UpdateVisual(tire, input.Steering);
            if (!IsGrounded) return;
            Vector3 velocity = body.GetPointVelocity(hit.point);
            float compression = Mathf.Clamp01((rayLength - hit.distance) / tire.SuspensionLength);
            float suspensionForce = Mathf.Max(0f, compression * tire.Spring - Vector3.Dot(velocity, transform.up) * tire.Damping);
            body.AddForceAtPosition(transform.up * suspensionForce, hit.point);
            Quaternion steer = canSteer ? Quaternion.AngleAxis(input.Steering * tire.MaximumSteeringAngle, transform.up) : Quaternion.identity;
            Vector3 forward = Vector3.ProjectOnPlane(steer * transform.forward, hit.normal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(steer * transform.right, hit.normal).normalized;
            body.AddForceAtPosition(-right * Vector3.Dot(velocity, right) * tire.Grip * body.mass, hit.point);
            if (canDrive && drivenCount > 0) body.AddForceAtPosition(forward * input.Forward * tire.DrivePower / drivenCount, hit.point);
            float speed = Vector3.Dot(velocity, forward);
            if (input.Brake > 0f && !Mathf.Approximately(speed, 0f))
            {
                float force = Mathf.Min(Mathf.Abs(speed) * body.mass / Time.fixedDeltaTime, tire.BrakePower * input.Brake);
                body.AddForceAtPosition(-forward * Mathf.Sign(speed) * force, hit.point);
            }
        }
        void UpdateVisual(TireDefinition tire, float steering)
        {
            if (visualRoot == null) return; visualRoot.position = WheelCenter;
            visualRoot.rotation = transform.rotation * Quaternion.AngleAxis(canSteer ? steering * tire.MaximumSteeringAngle : 0f, Vector3.up);
        }
    }
}
