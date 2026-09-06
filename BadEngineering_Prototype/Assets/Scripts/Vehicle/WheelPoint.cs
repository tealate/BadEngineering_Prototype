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

        [Header("Grip")]
        [SerializeField, Min(0f)] float lateralVelocityDeadZone = 0.03f;

        // 横滑りを1 FixedUpdateで完全に消そうとすると、
        // 複数WheelPoint間で補正が反転しやすいため、
        // 1ステップあたりの補正量を抑える。
        [SerializeField, Range(0f, 1f)] float lateralGripResponse = 0.2f;

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
                ConfigureMountedCollider();
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
            ConfigureMountedCollider();
        }

        private void ConfigureMountedCollider()
        {
            if (visualRoot == null) return;
            PrototypeCollision.SetLayer(visualRoot.gameObject, PrototypeCollision.MountedTire);
            if (visualRoot.GetComponent<Collider>() == null)
            {
                var collider = visualRoot.gameObject.AddComponent<BoxCollider>();
                var mesh = visualRoot.GetComponent<MeshFilter>();
                if (mesh != null && mesh.sharedMesh != null)
                { collider.center = mesh.sharedMesh.bounds.center; collider.size = mesh.sharedMesh.bounds.size; }
            }
        }

        public void RefreshReplicaVisual(Rigidbody body, TireDefinition tire, float steering)
        {
            if (tire == null) return;
            bool hitGround = TryGetGroundHit(body, -transform.up, tire.SuspensionLength + tire.Radius, out RaycastHit hit);
            WheelCenter = transform.position - transform.up * (hitGround ? Mathf.Max(0f, hit.distance - tire.Radius) : tire.SuspensionLength);
            UpdateVisual(tire, steering);
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
            {
                SimulateSideContact(body, tire, wheelCount);
                return;
            }

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
                // 各WheelPointが担当すると仮定する車体質量。
                float supportedMass =
                    body.mass /
                    Mathf.Max(1, wheelCount);

                // 横速度を1 FixedUpdateで完全に0へ持っていく力ではなく、
                // lateralGripResponse分だけ減衰させる力を求める。
                //
                // これにより、複数のWheelPointが同時に強い補正を行って
                // 横速度や角速度が毎フレーム反転するのを抑える。
                float forceToReduceSlip =
                    Mathf.Abs(lateralSpeed) *
                    supportedMass /
                    Time.fixedDeltaTime *
                    lateralGripResponse;

                // 最大横グリップ力は、
                // 現在そのタイヤが受け持っている垂直荷重に比例させる。
                float maximumGripForce =
                    suspensionForce *
                    tire.FrictionForNormal(hit.normal, transform.right);

                float lateralForceMagnitude =
                    Mathf.Min(
                        forceToReduceSlip,
                        maximumGripForce);

                Vector3 lateralForce =
                    -right *
                    Mathf.Sign(lateralSpeed) *
                    lateralForceMagnitude;

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
                int layer = candidate.collider.gameObject.layer;
                if (layer == PrototypeCollision.Player || layer == PrototypeCollision.Weapon ||
                    layer == PrototypeCollision.MountedTire || layer == PrototypeCollision.TireItem) continue;

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

        private void SimulateSideContact(Rigidbody body, TireDefinition tire, int wheelCount)
        {
            // 横転時はサスペンションRayと地面が平行になるため、重力方向にも接触を探す。
            Vector3 down = Physics.gravity.normalized;
            if (Mathf.Abs(Vector3.Dot(transform.right, down)) < 0.65f) return;
            if (!Physics.Raycast(WheelCenter - down * tire.Radius, down, out RaycastHit hit,
                tire.Radius * 1.5f, 1 << 0, QueryTriggerInteraction.Ignore)) return;
            float penetration = tire.Radius * 1.5f - hit.distance;
            float normalSpeed = Vector3.Dot(body.GetPointVelocity(hit.point), hit.normal);
            float load = Mathf.Clamp(penetration * tire.Spring - normalSpeed * tire.Damping, 0f,
                body.mass * Physics.gravity.magnitude / Mathf.Max(1, wheelCount) * tire.MaximumSuspensionLoad);
            body.AddForceAtPosition(hit.normal * load, hit.point);
            Vector3 slip = Vector3.ProjectOnPlane(body.GetPointVelocity(hit.point), hit.normal);
            Vector3 friction = Vector3.ClampMagnitude(-slip * body.mass / Mathf.Max(1, wheelCount) / Time.fixedDeltaTime,
                load * tire.FrictionForNormal(hit.normal, transform.right));
            body.AddForceAtPosition(friction, hit.point);
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
