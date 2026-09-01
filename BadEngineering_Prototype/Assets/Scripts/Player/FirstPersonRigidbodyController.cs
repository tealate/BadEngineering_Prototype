using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BadEngineering.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class FirstPersonRigidbodyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
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
        [SerializeField, Min(0f)] private float minimumUncontrolledDuration = 0.6f;
        [SerializeField, Min(0f)] private float recoveryAngularSpeed = 1.5f;

        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector2 moveInput;
        private float yaw;
        private float pitch;
        private bool jumpQueued;
        private bool isGrounded;
        private bool isUncontrolled;
        private float uncontrolledUntil;

        public bool IsUncontrolled => isUncontrolled;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (weaponSlots == null)
            {
                weaponSlots = GetComponent<PlayerWeaponSlots>();
            }

            yaw = transform.eulerAngles.y;
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
            isGrounded = CheckGrounded();

            if (isUncontrolled)
            {
                TryRecoverControl();
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
            if (isUncontrolled)
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
            if (isUncontrolled || mouse == null || playerCamera == null)
            {
                return;
            }

            Vector2 lookDelta = mouse.delta.ReadValue() * mouseSensitivity;
            yaw += lookDelta.x;
            pitch = Mathf.Clamp(pitch - lookDelta.y, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ReadWeaponInput()
        {
            if (weaponSlots == null || isUncontrolled)
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
            body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        public void ApplyRecoil(Vector3 impulse, Vector3 forcePosition)
        {
            body.AddForceAtPosition(impulse, forcePosition, ForceMode.Impulse);
            if (impulse.magnitude >= lossOfControlImpulse)
            {
                EnterUncontrolledState();
            }
        }

        private void EnterUncontrolledState()
        {
            if (isUncontrolled)
            {
                uncontrolledUntil = Mathf.Max(uncontrolledUntil, Time.time + minimumUncontrolledDuration);
                return;
            }

            isUncontrolled = true;
            uncontrolledUntil = Time.time + minimumUncontrolledDuration;
            jumpQueued = false;
            moveInput = Vector2.zero;
            body.constraints = RigidbodyConstraints.None;
        }

        private void TryRecoverControl()
        {
            if (Time.time < uncontrolledUntil || !isGrounded || body.angularVelocity.magnitude > recoveryAngularSpeed)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Quaternion uprightRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            body.angularVelocity = Vector3.zero;
            body.rotation = uprightRotation;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            yaw = uprightRotation.eulerAngles.y;
            pitch = 0f;
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            isUncontrolled = false;
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
