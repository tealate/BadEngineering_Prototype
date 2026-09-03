using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehiclePhysicsController : MonoBehaviour
    {
        [Header("Drive")]
        [SerializeField, Min(0f)] private float acceleration = 16f;
        [SerializeField, Min(0f)] private float reverseAcceleration = 10f;
        [SerializeField, Min(0f)] private float steeringAcceleration = 5f;
        [SerializeField, Min(0f)] private float maximumSpeed = 18f;
        [SerializeField, Min(0f)] private float lateralGrip = 5f;
        [SerializeField] private Transform centerOfMassMarker;

        private Rigidbody body;
        private Vector2 driveInput;
        private float baseMass;

        public Rigidbody Body => body;
        public Vector2 DriveInput => driveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            baseMass = body.mass;
            if (centerOfMassMarker != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMassMarker.position);
            }
        }

        public void SetDriveInput(Vector2 input)
        {
            driveInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void FixedUpdate()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            float driveAcceleration = driveInput.y >= 0f ? acceleration : reverseAcceleration;
            if (Mathf.Abs(localVelocity.z) < maximumSpeed || Mathf.Sign(driveInput.y) != Mathf.Sign(localVelocity.z))
            {
                body.AddForce(
                    transform.forward * (driveInput.y * driveAcceleration * baseMass),
                    ForceMode.Force);
            }

            float speedFactor = Mathf.Clamp01(Mathf.Abs(localVelocity.z) / 2f);
            body.AddTorque(
                transform.up * (driveInput.x * steeringAcceleration * speedFactor * baseMass),
                ForceMode.Force);
            body.AddForce(-transform.right * (localVelocity.x * lateralGrip), ForceMode.Acceleration);
        }
    }
}
