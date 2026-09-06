using UnityEngine;
namespace BadEngineering.Vehicle
{
    public abstract class MovementSystem : MonoBehaviour
    {
        protected VehiclePhysicsController Vehicle { get; private set; }
        protected Rigidbody Body => Vehicle.Body;
        protected VehicleInput Input { get; private set; }
        protected virtual void Awake()
        {
            Vehicle = GetComponentInParent<VehiclePhysicsController>();
            if (Vehicle == null)
                Debug.LogError("MovementSystem must be placed below a VehiclePhysicsController.", this);
        }
        public virtual void ApplyInput(VehicleInput input) => Input = input;
        public abstract void SimulatePhysics();
    }
}
