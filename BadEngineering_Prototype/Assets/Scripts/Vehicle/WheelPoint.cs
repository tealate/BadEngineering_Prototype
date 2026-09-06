using UnityEngine;
namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class WheelPoint : MonoBehaviour
    {
        [SerializeField] bool canSteer, canDrive = true;
        [SerializeField] Transform visualRoot;
        [SerializeField] LayerMask groundMask = ~0;
        Quaternion visualBaseLocalRotation;
        TireDefinition appliedTire;
        readonly RaycastHit[] groundHits = new RaycastHit[8];
        public bool CanSteer => canSteer; public bool CanDrive => canDrive; public bool IsGrounded { get; private set; }
        public Vector3 WheelCenter { get; private set; }

        public void Configure(bool steer, bool drive)
        {
            canSteer = steer;
            canDrive = drive;
        }

        public void ApplyTire(TireDefinition tire)
        {
            if (tire == null || appliedTire == tire)
                return;

            if (visualRoot != null && appliedTire == null)
            {
                appliedTire = tire;
                return;
            }

            if (visualRoot != null)
            {
                if (Application.isPlaying) Destroy(visualRoot.gameObject);
                else DestroyImmediate(visualRoot.gameObject);
                visualRoot = null;
            }

            if (tire.VisualPrefab != null)
            {
                GameObject visual = Instantiate(tire.VisualPrefab, transform);
                visual.name = "Visual";
                visualRoot = visual.transform;
                visualBaseLocalRotation = visualRoot.localRotation;
            }
            appliedTire = tire;
        }

        void Awake()
        {
            if (visualRoot != null)
                visualBaseLocalRotation = visualRoot.localRotation;
        }

        public void Simulate(Rigidbody body, TireDefinition tire, VehicleInput input, int drivenCount, int wheelCount)
        {
            Vector3 down = -transform.up; float rayLength = tire.SuspensionLength + tire.Radius;
            IsGrounded = TryGetGroundHit(body, down, rayLength, out RaycastHit hit);
            float centerDistance = IsGrounded ? Mathf.Max(0f, hit.distance - tire.Radius) : tire.SuspensionLength;
            WheelCenter = transform.position + down * centerDistance; UpdateVisual(tire, input.Steering);
            if (!IsGrounded) return;
            Vector3 velocity = body.GetPointVelocity(hit.point);
            float compression = Mathf.Clamp01((rayLength - hit.distance) / tire.SuspensionLength);
            float suspensionForce = Mathf.Max(0f, compression * tire.Spring - Vector3.Dot(velocity, transform.up) * tire.Damping);
            float staticLoadPerWheel = body.mass * Physics.gravity.magnitude / Mathf.Max(1, wheelCount);
            suspensionForce = Mathf.Min(suspensionForce, staticLoadPerWheel * tire.MaximumSuspensionLoad);
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

        bool TryGetGroundHit(Rigidbody body, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            int count = Physics.RaycastNonAlloc(
                transform.position,
                direction,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);
            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = groundHits[i];
                Rigidbody hitBody = candidate.rigidbody;
                if (hitBody == body || (hitBody != null && !hitBody.isKinematic))
                    continue;
                if (candidate.distance < closestDistance)
                {
                    closestHit = candidate;
                    closestDistance = candidate.distance;
                }
            }
            return closestDistance < float.PositiveInfinity;
        }
        void UpdateVisual(TireDefinition tire, float steering)
        {
            if (visualRoot == null) return; visualRoot.position = WheelCenter;
            Quaternion steeringRotation = Quaternion.AngleAxis(
                canSteer ? steering * tire.MaximumSteeringAngle : 0f,
                Vector3.up);
            visualRoot.rotation = transform.rotation * steeringRotation * visualBaseLocalRotation;
        }
    }
}
