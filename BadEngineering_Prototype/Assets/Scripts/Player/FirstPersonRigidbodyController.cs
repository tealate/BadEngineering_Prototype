using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using BadEngineering.Vehicle;

namespace BadEngineering.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class FirstPersonRigidbodyController : MonoBehaviour
    {
        public enum PhysicalState
        {
            Normal,
            Uncontrolled,
            Recovering
        }

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform headPivot;
        [SerializeField] private PlayerWeaponSlots weaponSlots;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float groundAcceleration = 35f;
        [SerializeField, Min(0f)] private float groundDeceleration = 45f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.25f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float verticalLookLimit = 85f;

        [Header("Grounding")]
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormal = 0.6f;

        [Header("Recoil Loss of Control")]
        [SerializeField, Min(0f)] private float lossOfControlImpulse = 2.5f;
        [SerializeField, Min(0f)] private float collisionLossOfControlImpulse = 180f;
        [SerializeField, Min(0f)] private float minimumUncontrolledDuration = 0.6f;
        [SerializeField, Min(0f)] private float uncontrolledAngularDamping = 2f;
        [SerializeField, Min(0f)] private float recoveryAngularSpeed = 1.5f;
        [SerializeField, Min(0f)] private float recoveryLinearSpeed = 0.5f;
        [SerializeField, Min(0f)] private float recoveryTorque = 20f;
        [SerializeField, Min(0f)] private float recoveryAngularDamping = 5f;
        [SerializeField, Range(0f, 10f)] private float uprightAngleTolerance = 0.5f;
        [SerializeField, Min(0f)] private float recoveryCompletionAngularSpeed = 0.15f;
        [SerializeField, Min(0f)] private float recoveryStableDuration = 0.25f;

        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector2 moveInput;
        private float yaw;
        private float pitch;
        private float freeLookYaw;
        private bool jumpQueued;
        private bool isGrounded;
        private PhysicalState physicalState;
        private float uncontrolledUntil;
        private float stableSince = -1f;
        private float normalAngularDamping;
        private VehicleStationUser stationUser;

        public bool IsUncontrolled => physicalState == PhysicalState.Uncontrolled;
        public PhysicalState CurrentPhysicalState => physicalState;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            stationUser = GetComponent<VehicleStationUser>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (weaponSlots == null)
            {
                weaponSlots = GetComponent<PlayerWeaponSlots>();
            }

            if (headPivot == null && playerCamera != null)
            {
                headPivot = playerCamera.transform;
            }

            yaw = transform.eulerAngles.y;
            normalAngularDamping = body.angularDamping;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            LockCursor();
        }

        private void Update()
        {
            ReadInput();
            ReadWeaponInput();
            UpdateLook();
        }

        private void FixedUpdate()
        {
            if (stationUser != null && stationUser.IsUsingStation)
            {
                if (stationUser.IsDriving)
                {
                    stationUser.CurrentStation.Vehicle?.SetDriveInput(moveInput);
                }
                return;
            }

            isGrounded = CheckGrounded();

            if (physicalState == PhysicalState.Uncontrolled)
            {
                TryStartRecovering();
                return;
            }

            if (physicalState == PhysicalState.Recovering)
            {
                ApplyRecoveryTorque();
                return;
            }

            ApplyRotation();
            ApplyMovement();
            ApplyJump();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                LockCursor();
            }
        }

        private void ReadInput()
        {
            if (physicalState != PhysicalState.Normal)
            {
                moveInput = Vector2.zero;
                jumpQueued = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            moveInput = new Vector2(
                ReadAxis(keyboard.aKey, keyboard.dKey),
                ReadAxis(keyboard.sKey, keyboard.wKey));
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            if (stationUser != null && stationUser.IsUsingStation)
            {
                jumpQueued = false;
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpQueued = true;
            }
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private void UpdateLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || headPivot == null)
            {
                return;
            }

            Vector2 lookDelta = mouse.delta.ReadValue() * mouseSensitivity;
            if (physicalState == PhysicalState.Normal && (stationUser == null || !stationUser.IsUsingStation))
            {
                yaw += lookDelta.x;
            }
            else
            {
                freeLookYaw += lookDelta.x;
            }
            pitch = Mathf.Clamp(pitch - lookDelta.y, -verticalLookLimit, verticalLookLimit);
            headPivot.localRotation = Quaternion.Euler(pitch, freeLookYaw, 0f);
        }

        private void ReadWeaponInput()
        {
            if (weaponSlots == null)
            {
                return;
            }

            if (stationUser != null && stationUser.IsDriving)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    weaponSlots.EquipSlot(0);
                }
                else if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    weaponSlots.EquipSlot(1);
                }
                else if (keyboard.digit3Key.wasPressedThisFrame)
                {
                    weaponSlots.EquipSlot(2);
                }
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    weaponSlots.DropSelectedWeapon();
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                weaponSlots.PrimaryPressed();
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                weaponSlots.PrimaryReleased();
            }
            if (mouse.rightButton.wasPressedThisFrame)
            {
                weaponSlots.SecondaryPressed();
            }
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                weaponSlots.SecondaryReleased();
            }
        }

        private void ApplyRotation()
        {
            if (!Mathf.Approximately(freeLookYaw, 0f))
            {
                yaw += freeLookYaw;
                freeLookYaw = 0f;
                headPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        public void ApplyRecoil(Vector3 impulse, Vector3 forcePosition)
        {
            ApplyImpulse(impulse, forcePosition);
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 forcePosition)
        {
            body.AddForceAtPosition(impulse, forcePosition, ForceMode.Impulse);
            if (impulse.magnitude >= lossOfControlImpulse)
            {
                EnterUncontrolledState();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (physicalState == PhysicalState.Normal &&
                collision.impulse.magnitude >= collisionLossOfControlImpulse)
            {
                EnterUncontrolledState();
            }
        }

        private void EnterUncontrolledState()
        {
            uncontrolledUntil = Time.time + minimumUncontrolledDuration;
            physicalState = PhysicalState.Uncontrolled;
            stableSince = -1f;
            jumpQueued = false;
            moveInput = Vector2.zero;
            body.angularDamping = Mathf.Max(normalAngularDamping, uncontrolledAngularDamping);
            body.constraints &= ~(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);
        }

        private void TryStartRecovering()
        {
            if (Time.time < uncontrolledUntil || !isGrounded ||
                body.linearVelocity.magnitude > recoveryLinearSpeed ||
                body.angularVelocity.magnitude > recoveryAngularSpeed)
            {
                return;
            }

            physicalState = PhysicalState.Recovering;
            body.angularDamping = Mathf.Max(normalAngularDamping, recoveryAngularDamping);
            stableSince = -1f;
        }

        private void ApplyRecoveryTorque()
        {
            Vector3 uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            if (uprightAxis.sqrMagnitude < 0.0001f && Vector3.Dot(transform.up, Vector3.up) < 0f)
            {
                uprightAxis = transform.right;
            }

            body.AddTorque(uprightAxis * recoveryTorque, ForceMode.Acceleration);

            float uprightError = Vector3.Angle(transform.up, Vector3.up);
            if (uprightError <= uprightAngleTolerance &&
                body.angularVelocity.magnitude <= recoveryCompletionAngularSpeed)
            {
                if (stableSince < 0f)
                {
                    stableSince = Time.time;
                }
                else if (Time.time - stableSince >= recoveryStableDuration)
                {
                    EnterNormalState();
                }
            }
            else
            {
                stableSince = -1f;
            }
        }

        private void EnterNormalState()
        {
            physicalState = PhysicalState.Normal;
            body.angularDamping = normalAngularDamping;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            yaw = transform.eulerAngles.y;
            stableSince = -1f;
        }

        private void ApplyMovement()
        {
            Vector3 desiredVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
            Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 velocityChange = desiredVelocity - currentHorizontalVelocity;

            float acceleration = moveInput.sqrMagnitude > 0f ? groundAcceleration : groundDeceleration;
            if (!isGrounded)
            {
                acceleration *= airControl;
            }

            Vector3 accelerationVector = Vector3.ClampMagnitude(
                velocityChange / Time.fixedDeltaTime,
                acceleration);
            body.AddForce(accelerationVector, ForceMode.Acceleration);
        }

        private void ApplyJump()
        {
            if (!jumpQueued)
            {
                return;
            }

            jumpQueued = false;
            if (!isGrounded)
            {
                return;
            }

            float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            Vector3 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, 0f) + jumpSpeed;
            body.linearVelocity = velocity;
            isGrounded = false;
        }

        private bool CheckGrounded()
        {
            Vector3 center = transform.TransformPoint(capsule.center);
            float scaledHalfHeight = capsule.height * Mathf.Abs(transform.lossyScale.y) * 0.5f;
            float scaledRadius = capsule.radius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z));
            float rayDistance = Mathf.Max(0f, scaledHalfHeight - scaledRadius) + groundCheckDistance;

            int hitCount = Physics.SphereCastNonAlloc(
                center,
                scaledRadius * 0.9f,
                Vector3.down,
                groundHits,
                rayDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.collider != capsule && Vector3.Dot(hit.normal, Vector3.up) >= minimumGroundNormal)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
