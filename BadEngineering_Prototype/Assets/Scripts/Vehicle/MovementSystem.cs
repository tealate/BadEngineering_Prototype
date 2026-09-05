using UnityEngine;
namespace BadEngineering.Vehicle
{
    [RequireComponent(typeof(VehiclePhysicsController))]
    public abstract class MovementSystem : MonoBehaviour
    {
        protected VehiclePhysicsController Vehicle { get; private set; }
        protected Rigidbody Body => Vehicle.Body;
        protected VehicleInput Input { get; private set; }
        protected virtual void Awake() => Vehicle = GetComponent<VehiclePhysicsController>();
        public virtual void ApplyInput(VehicleInput input) => Input = input;
        public abstract void SimulatePhysics();
    }
}
