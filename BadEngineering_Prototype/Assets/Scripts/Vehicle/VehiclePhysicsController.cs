using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehiclePhysicsController : MonoBehaviour
    {
        [SerializeField] private MovementSystem movementSystem;
        [SerializeField] private Transform centerOfMassMarker;

        private Rigidbody body;
        private VehicleInput movementInput;
        private PhysicsMaterial contactMaterial;

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
        }

        public void SetMovementSystem(MovementSystem system)
        {
            if (movementSystem == system)
                return;

            movementSystem?.ApplyInput(VehicleInput.None);
            movementSystem = system;
            movementSystem?.ApplyInput(movementInput);
        }

        public void SetDriveInput(Vector2 input) => SetMovementInput(new VehicleInput(input.y, input.x, 0f));

        private void FixedUpdate()
        {
            movementSystem?.SimulatePhysics();
        }

        private void OnDisable() => SetMovementInput(VehicleInput.None);

        private void OnDestroy()
        {
            if (contactMaterial != null)
                Destroy(contactMaterial);
        }
    }
}
