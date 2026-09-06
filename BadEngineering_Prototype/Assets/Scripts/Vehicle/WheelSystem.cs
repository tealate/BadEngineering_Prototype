using UnityEngine;

namespace BadEngineering.Vehicle
{
    public sealed class WheelSystem : MovementSystem
    {
        const int RequiredWheelCount = 4;

        [Header("Wheel layout")]
        [SerializeField] Collider chassisBoundsSource;
        [SerializeField, Range(0f, 1.5f)] float sidePositionRatio = 0.9f;
        [SerializeField, Range(0f, 1.5f)] float frontPositionRatio = 0.75f;
        [SerializeField, Range(0f, 1.5f)] float rearPositionRatio = 0.75f;
        [SerializeField] float verticalOffset;
        [SerializeField] bool autoPlaceWheels = true;

        [Header("Wheels")]
        [SerializeField] WheelPoint[] wheelPoints;
        [SerializeField] TireDefinition currentTire;
        public WheelPoint[] WheelPoints => wheelPoints; public TireDefinition CurrentTire => currentTire;

        protected override void Awake()
        {
            base.Awake();
            if (autoPlaceWheels)
                RebuildWheelLayout();
            else if (wheelPoints == null || wheelPoints.Length == 0)
                wheelPoints = GetComponentsInChildren<WheelPoint>(true);
        }

        [ContextMenu("Rebuild Wheel Layout")]
        public void RebuildWheelLayout()
        {
            chassisBoundsSource ??= FindChassisCollider();
            if (chassisBoundsSource == null)
            {
                Debug.LogWarning("WheelSystem requires a Chassis Bounds Source to place its wheels.", this);
                return;
            }

            EnsureFourWheelPoints();
            Bounds bounds = CalculateLocalChassisBounds(chassisBoundsSource);
            float wheelX = bounds.extents.x * sidePositionRatio;
            float frontZ = bounds.extents.z * frontPositionRatio;
            float rearZ = -bounds.extents.z * rearPositionRatio;
            float wheelY = bounds.min.y + verticalOffset;

            SetWheel(0, "WheelPoint_FL", new Vector3(bounds.center.x - wheelX, wheelY, bounds.center.z + frontZ), true);
            SetWheel(1, "WheelPoint_FR", new Vector3(bounds.center.x + wheelX, wheelY, bounds.center.z + frontZ), true);
            SetWheel(2, "WheelPoint_RL", new Vector3(bounds.center.x - wheelX, wheelY, bounds.center.z + rearZ), false);
            SetWheel(3, "WheelPoint_RR", new Vector3(bounds.center.x + wheelX, wheelY, bounds.center.z + rearZ), false);
        }

        void EnsureFourWheelPoints()
        {
            WheelPoint[] existing = GetComponentsInChildren<WheelPoint>(true);
            wheelPoints = new WheelPoint[RequiredWheelCount];
            for (int i = 0; i < RequiredWheelCount; i++)
            {
                string expectedName = WheelName(i);
                foreach (WheelPoint point in existing)
                {
                    if (point != null && point.name == expectedName)
                    {
                        wheelPoints[i] = point;
                        break;
                    }
                }

                if (wheelPoints[i] == null && i < existing.Length)
                    wheelPoints[i] = existing[i];
                if (wheelPoints[i] == null)
                {
                    GameObject anchor = new GameObject(expectedName);
                    anchor.transform.SetParent(transform, false);
                    wheelPoints[i] = anchor.AddComponent<WheelPoint>();
                }
            }
        }

        void SetWheel(int index, string wheelName, Vector3 localPosition, bool canSteer)
        {
            WheelPoint point = wheelPoints[index];
            point.name = wheelName;
            point.transform.SetParent(transform, true);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            point.Configure(canSteer, true);
            point.ApplyTire(currentTire);
        }

        Collider FindChassisCollider()
        {
            Collider best = null;
            float largestVolume = 0f;
            Transform searchRoot = Vehicle != null ? Vehicle.transform : transform.root;
            foreach (Collider candidate in searchRoot.GetComponentsInChildren<Collider>(true))
            {
                if (candidate.isTrigger || candidate.GetComponentInParent<WheelPoint>() != null)
                    continue;
                Vector3 size = candidate.bounds.size;
                float volume = size.x * size.y * size.z;
                if (volume > largestVolume)
                {
                    best = candidate;
                    largestVolume = volume;
                }
            }
            return best;
        }

        Bounds CalculateLocalChassisBounds(Collider source)
        {
            if (source is BoxCollider box)
            {
                Vector3 half = box.size * 0.5f;
                Bounds result = new Bounds(transform.InverseTransformPoint(box.transform.TransformPoint(box.center)), Vector3.zero);
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    result.Encapsulate(transform.InverseTransformPoint(
                        box.transform.TransformPoint(box.center + Vector3.Scale(half, new Vector3(x, y, z)))));
                return result;
            }

            Bounds world = source.bounds;
            Bounds fallback = new Bounds(transform.InverseTransformPoint(world.center), Vector3.zero);
            Vector3 halfWorld = world.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                fallback.Encapsulate(transform.InverseTransformPoint(
                    world.center + Vector3.Scale(halfWorld, new Vector3(x, y, z))));
            return fallback;
        }

        static string WheelName(int index) => index switch
        {
            0 => "WheelPoint_FL",
            1 => "WheelPoint_FR",
            2 => "WheelPoint_RL",
            _ => "WheelPoint_RR"
        };

        public void ReplaceTire(TireDefinition tire)
        {
            if (tire == null || currentTire == tire)
                return;

            currentTire = tire;
            if (wheelPoints == null)
                return;
            foreach (WheelPoint point in wheelPoints)
                point?.ApplyTire(tire);
        }
        public override void SimulatePhysics()
        {
            if (currentTire == null || wheelPoints == null) return; int driven = 0;
            foreach (WheelPoint p in wheelPoints) if (p != null && p.CanDrive) driven++;
            foreach (WheelPoint p in wheelPoints) p?.Simulate(Body, currentTire, Input, driven, wheelPoints.Length);
        }
    }
}
