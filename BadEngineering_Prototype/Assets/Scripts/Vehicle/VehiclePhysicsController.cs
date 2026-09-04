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

        public Rigidbody Body => body;
        public MovementSystem Movement => movementSystem;
        public VehicleInput MovementInput => movementInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            movementSystem ??= GetComponent<MovementSystem>();
            if (centerOfMassMarker != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMassMarker.position);
            }
        }

        public void SetMovementInput(VehicleInput input)
        {
            movementInput = input;
            movementSystem?.ApplyInput(input);
        }

        public void SetDriveInput(Vector2 input) => SetMovementInput(new VehicleInput(input.y, input.x, 0f));

        private void FixedUpdate()
        {
            movementSystem?.SimulatePhysics();
        }

        private void OnDisable() => SetMovementInput(VehicleInput.None);
    }
}
