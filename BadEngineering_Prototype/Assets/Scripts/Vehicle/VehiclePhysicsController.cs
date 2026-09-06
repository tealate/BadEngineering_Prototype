using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehiclePhysicsController : MonoBehaviour
    {
        [SerializeField] private MovementSystem movementSystem;
        [SerializeField] private Transform centerOfMassMarker;

        [Header("Sleep")]
        [SerializeField, Min(0f)] private float sleepLinearThreshold = 0.03f;
        [SerializeField, Min(0f)] private float sleepAngularThreshold = 0.03f;
        [SerializeField, Min(0f)] private float sleepDelay = 0.5f;

        private Rigidbody body;
        private VehicleInput movementInput;
        private PhysicsMaterial contactMaterial;
        private float stationaryTime;

        public Rigidbody Body => body;
        public MovementSystem Movement => movementSystem;
        public VehicleInput MovementInput => movementInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.maxDepenetrationVelocity = 1f;
            body.angularDamping = Mathf.Max(body.angularDamping, 1f);
            ApplyFrictionlessChassisMaterial();
            movementSystem ??= GetComponentInChildren<MovementSystem>(true);
            if (centerOfMassMarker != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMassMarker.position);
            }
        }

        private void ApplyFrictionlessChassisMaterial()
        {
            contactMaterial = new PhysicsMaterial("Vehicle Chassis Contact")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            foreach (Collider vehicleCollider in GetComponentsInChildren<Collider>(true))
            {
                if (!vehicleCollider.isTrigger && vehicleCollider.GetComponentInParent<WheelPoint>() == null)
                    vehicleCollider.sharedMaterial = contactMaterial;
            }
        }

        public void SetMovementInput(VehicleInput input)
        {
            movementInput = input;
            movementSystem?.ApplyInput(input);

            if (HasMovementInput(input))
            {
                stationaryTime = 0f;
                body?.WakeUp();
            }
        }

        public void SetMovementSystem(MovementSystem system)
        {
            if (movementSystem == system)
                return;

            movementSystem?.ApplyInput(VehicleInput.None);
            movementSystem = system;
            movementSystem?.ApplyInput(movementInput);

            if (HasMovementInput(movementInput))
                body?.WakeUp();
        }

        public void SetDriveInput(Vector2 input) => SetMovementInput(new VehicleInput(input.y, input.x, 0f));

        private void FixedUpdate()
        {
            if (!BadEngineering.Network.GameplayAuthority.CanSimulate) return;
            if (body.IsSleeping())
                return;

            if (ShouldSleep())
            {
                stationaryTime += Time.fixedDeltaTime;
                if (stationaryTime >= sleepDelay)
                {
                    body.Sleep();
                    return;
                }
            }
            else
            {
                stationaryTime = 0f;
            }

            movementSystem?.SimulatePhysics();
        }

        private bool ShouldSleep()
        {
            return !HasMovementInput(movementInput) &&
                   body.linearVelocity.sqrMagnitude <= sleepLinearThreshold * sleepLinearThreshold &&
                   body.angularVelocity.sqrMagnitude <= sleepAngularThreshold * sleepAngularThreshold;
        }

        private static bool HasMovementInput(VehicleInput input)
        {
            const float inputDeadZone = 0.001f;
            return Mathf.Abs(input.Forward) > inputDeadZone ||
                   Mathf.Abs(input.Steering) > inputDeadZone ||
                   input.Brake > inputDeadZone;
        }

        private void OnCollisionEnter(Collision collision)
        {
            stationaryTime = 0f;
        }

        private void OnDisable() => SetMovementInput(VehicleInput.None);

        private void OnDestroy()
        {
            if (contactMaterial != null)
                Destroy(contactMaterial);
        }
    }
}
