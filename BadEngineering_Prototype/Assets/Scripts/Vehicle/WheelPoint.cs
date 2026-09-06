using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class WheelPoint : MonoBehaviour
    {
        [SerializeField] bool canSteer;
        [SerializeField] bool canDrive = true;
        [SerializeField] Transform visualRoot;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField, Min(0f)] float lateralVelocityDeadZone = 0.03f;

        // 微小な上下速度によるサスペンションの振動を抑える。
        const float SuspensionVelocityDeadZone = 0.02f;

        Quaternion visualBaseLocalRotation;
        TireDefinition appliedTire;

        readonly RaycastHit[] groundHits = new RaycastHit[8];

        public bool CanSteer => canSteer;
        public bool CanDrive => canDrive;
        public bool IsGrounded { get; private set; }
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
                if (Application.isPlaying)
                    Destroy(visualRoot.gameObject);
                else
                    DestroyImmediate(visualRoot.gameObject);

                visualRoot = null;
            }

            if (tire.VisualPrefab != null)
            {
                GameObject visual = Instantiate(
                    tire.VisualPrefab,
                    transform);

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

        public void Simulate(
            Rigidbody body,
            TireDefinition tire,
            VehicleInput input,
            int drivenCount,
            int wheelCount)
        {
            if (body == null || tire == null)
                return;

            Vector3 up = transform.up;
            Vector3 down = -up;

            float suspensionLength = tire.SuspensionLength;
            float rayLength = suspensionLength + tire.Radius;

            // ---------------------------------------------------------
            // Ground detection
            // ---------------------------------------------------------

            IsGrounded = TryGetGroundHit(
                body,
                down,
                rayLength,
                out RaycastHit hit);

            float wheelCenterDistance = IsGrounded
                ? Mathf.Max(0f, hit.distance - tire.Radius)
                : suspensionLength;

            WheelCenter =
                transform.position +
                down * wheelCenterDistance;

            UpdateVisual(tire, input.Steering);

            if (!IsGrounded)
                return;

            // 接地点の速度。
            // タイヤの横グリップ・駆動・ブレーキで使用する。
            Vector3 contactVelocity =
                body.GetPointVelocity(hit.point);

            // ---------------------------------------------------------
            // Suspension
            // ---------------------------------------------------------

            // サスペンションの実際の圧縮距離 [m]
            //
            // 0
            //   = 完全に伸びている
            //
            // SuspensionLength
            //   = 完全に縮んでいる
            float compressionDistance = Mathf.Clamp(
                rayLength - hit.distance,
                0f,
                suspensionLength);

            // サスペンション取付位置そのものの速度を使用する。
            // 接地点の速度を使うと車体の回転などの影響が混ざりやすい。
            Vector3 suspensionPointVelocity =
                body.GetPointVelocity(transform.position);

            float suspensionVelocity =
                Vector3.Dot(
                    suspensionPointVelocity,
                    up);

            // 静止付近の微小速度をダンパーが拾い続けるのを防止。
            if (Mathf.Abs(suspensionVelocity)
                < SuspensionVelocityDeadZone)
            {
                suspensionVelocity = 0f;
            }

            // Hooke's law:
            //
            // F = kx
            //
            // tire.Spring は N/m として扱う。
            float springForce =
                compressionDistance * tire.Spring;

            // Damper:
            //
            // 上方向へ動いているときは力を減らす。
            // 下方向へ動いているときは力を増やす。
            float dampingForce =
                -suspensionVelocity * tire.Damping;

            // 地面を引っ張ることはできないため0未満にはしない。
            float suspensionForce =
                Mathf.Max(
                    0f,
                    springForce + dampingForce);

            // 異常な瞬間荷重を防止。
            float staticLoadPerWheel =
                body.mass *
                Physics.gravity.magnitude /
                Mathf.Max(1, wheelCount);

            float maximumSuspensionForce =
                staticLoadPerWheel *
                tire.MaximumSuspensionLoad;

            suspensionForce =
                Mathf.Min(
                    suspensionForce,
                    maximumSuspensionForce);

            body.AddForceAtPosition(
                up * suspensionForce,
                hit.point,
                ForceMode.Force);

            // ---------------------------------------------------------
            // Wheel orientation
            // ---------------------------------------------------------

            Quaternion steerRotation =
                canSteer
                    ? Quaternion.AngleAxis(
                        input.Steering *
                        tire.MaximumSteeringAngle,
                        up)
                    : Quaternion.identity;

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    steerRotation * transform.forward,
                    hit.normal);

            Vector3 right =
                Vector3.ProjectOnPlane(
                    steerRotation * transform.right,
                    hit.normal);

            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();

            if (right.sqrMagnitude > 0.0001f)
                right.Normalize();

            // ---------------------------------------------------------
            // Lateral grip
            // ---------------------------------------------------------

            float lateralSpeed =
                Vector3.Dot(
                    contactVelocity,
                    right);

            if (Mathf.Abs(lateralSpeed) >= lateralVelocityDeadZone)
            {
                Vector3 lateralForce =
                    -right *
                    lateralSpeed *
                    tire.Grip *
                    body.mass;

                body.AddForceAtPosition(
                    lateralForce,
                    hit.point,
                    ForceMode.Force);
            }

            // ---------------------------------------------------------
            // Drive
            // ---------------------------------------------------------

            if (canDrive && drivenCount > 0)
            {
                float driveForce =
                    input.Forward *
                    tire.DrivePower /
                    drivenCount;

                body.AddForceAtPosition(
                    forward * driveForce,
                    hit.point,
                    ForceMode.Force);
            }

            // ---------------------------------------------------------
            // Brake
            // ---------------------------------------------------------

            float longitudinalSpeed =
                Vector3.Dot(
                    contactVelocity,
                    forward);

            if (input.Brake > 0f &&
                !Mathf.Approximately(
                    longitudinalSpeed,
                    0f))
            {
                float requiredForce =
                    Mathf.Abs(longitudinalSpeed) *
                    body.mass /
                    Time.fixedDeltaTime;

                float brakeForce =
                    Mathf.Min(
                        requiredForce,
                        tire.BrakePower *
                        input.Brake);

                body.AddForceAtPosition(
                    -forward *
                    Mathf.Sign(longitudinalSpeed) *
                    brakeForce,
                    hit.point,
                    ForceMode.Force);
            }
        }

        bool TryGetGroundHit(
            Rigidbody body,
            Vector3 direction,
            float distance,
            out RaycastHit closestHit)
        {
            int count = Physics.RaycastNonAlloc(
                transform.position,
                direction,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            closestHit = default;

            float closestDistance =
                float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate =
                    groundHits[i];

                Rigidbody hitBody =
                    candidate.rigidbody;

                // 自分自身は無視。
                //
                // 動的Rigidbodyも無視する。
                // プレイヤー等をタイヤが地面として扱わないため。
                if (hitBody == body ||
                    (hitBody != null &&
                     !hitBody.isKinematic))
                {
                    continue;
                }

                if (candidate.distance <
                    closestDistance)
                {
                    closestHit = candidate;
                    closestDistance =
                        candidate.distance;
                }
            }

            return closestDistance <
                   float.PositiveInfinity;
        }

        void UpdateVisual(
            TireDefinition tire,
            float steering)
        {
            if (visualRoot == null)
                return;

            visualRoot.position =
                WheelCenter;

            Quaternion steeringRotation =
                Quaternion.AngleAxis(
                    canSteer
                        ? steering *
                          tire.MaximumSteeringAngle
                        : 0f,
                    Vector3.up);

            visualRoot.rotation =
                transform.rotation *
                steeringRotation *
                visualBaseLocalRotation;
        }
    }
}
