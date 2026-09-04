namespace BadEngineering.Vehicle
{
    public sealed class WheelSystem : MovementSystem
    {
        [UnityEngine.SerializeField] WheelPoint[] wheelPoints;
        [UnityEngine.SerializeField] TireDefinition currentTire;
        public WheelPoint[] WheelPoints => wheelPoints; public TireDefinition CurrentTire => currentTire;
        protected override void Awake() { base.Awake(); if (wheelPoints == null || wheelPoints.Length == 0) wheelPoints = GetComponentsInChildren<WheelPoint>(true); }
        public void ReplaceTire(TireDefinition tire) => currentTire = tire;
        public override void SimulatePhysics()
        {
            if (currentTire == null || wheelPoints == null) return; int driven = 0;
            foreach (WheelPoint p in wheelPoints) if (p != null && p.CanDrive) driven++;
            foreach (WheelPoint p in wheelPoints) p?.Simulate(Body, currentTire, Input, driven);
        }
    }
}
