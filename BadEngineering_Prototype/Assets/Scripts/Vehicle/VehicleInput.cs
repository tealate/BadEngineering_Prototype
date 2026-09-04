using UnityEngine;
namespace BadEngineering.Vehicle
{
    public readonly struct VehicleInput
    {
        public VehicleInput(float forward, float steering, float brake)
        { Forward = Mathf.Clamp(forward, -1f, 1f); Steering = Mathf.Clamp(steering, -1f, 1f); Brake = Mathf.Clamp01(brake); }
        public float Forward { get; }
        public float Steering { get; }
        public float Brake { get; }
        public static VehicleInput None => new(0f, 0f, 0f);
    }
}
