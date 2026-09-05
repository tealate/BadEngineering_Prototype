using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using BadEngineering.Vehicle;

namespace BadEngineering.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerPhysicsController))]
    public sealed class FirstPersonRigidbodyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform headPivot;
        [SerializeField] private PlayerWeaponSlots weaponSlots;
        [SerializeField] private PlayerPhysicsController playerPhysics;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float groundAcceleration = 35f;
        [SerializeField, Min(0f)] private float groundDeceleration = 45f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.25f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float verticalLookLimit = 85f;

        private Rigidbody body;
        private Vector2 moveInput;
        private float yaw;
        private float pitch;
        private float freeLookYaw;
        private bool jumpQueued;
        private VehicleStationUser stationUser;
        private Transform originalHeadParent;
        private Vector3 originalHeadLocalPosition;
        private Quaternion originalHeadLocalRotation;

        public bool IsUncontrolled => CurrentPhysicalState == PlayerPhysicalState.Uncontrolled;
        public PlayerPhysicalState CurrentPhysicalState => playerPhysics != null ? playerPhysics.State : PlayerPhysicalState.Normal;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            stationUser = GetComponent<VehicleStationUser>();
            if (playerPhysics == null)
            {
                playerPhysics = GetComponent<PlayerPhysicsController>();
            }

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
                headPivot = playerCamera.transform.parent != null
                    ? playerCamera.transform.parent
                    : playerCamera.transform;
            }

            if (headPivot != null)
            {
                originalHeadParent = headPivot.parent;
                originalHeadLocalPosition = headPivot.localPosition;
                originalHeadLocalRotation = headPivot.localRotation;
            }

            yaw = transform.eulerAngles.y;
            playerPhysics.StateChanged += OnPhysicalStateChanged;
            LockCursor();
        }

        private void OnDestroy()
        {
            if (playerPhysics != null)
            {
                playerPhysics.StateChanged -= OnPhysicalStateChanged;
            }
        }

        private void OnPhysicalStateChanged(PlayerPhysicalState state)
        {
            if (state == PlayerPhysicalState.Normal)
            {
                yaw = transform.eulerAngles.y;
            }
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
                    float brake = Keyboard.current != null && Keyboard.current.spaceKey.isPressed ? 1f : 0f;
                    stationUser.CurrentStation.Vehicle?.SetMovementInput(
                        new VehicleInput(moveInput.y, moveInput.x, brake));
                }
                return;
            }

            if (!playerPhysics.CanMove)
            {
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
            if (!playerPhysics.CanMove)
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
            if (playerPhysics.CanMove && (stationUser == null || !stationUser.IsUsingStation))
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

        public void EnterVehicleView(Transform viewAnchor, Vector3 localOffset)
        {
            if (headPivot == null || viewAnchor == null)
            {
                return;
            }

            freeLookYaw = 0f;
            pitch = 0f;
            headPivot.SetParent(viewAnchor, false);
            headPivot.SetLocalPositionAndRotation(localOffset, Quaternion.identity);
        }

        public void ExitVehicleView()
        {
            if (headPivot == null || originalHeadParent == null)
            {
                return;
            }

            freeLookYaw = 0f;
            pitch = 0f;
            headPivot.SetParent(originalHeadParent, false);
            headPivot.SetLocalPositionAndRotation(originalHeadLocalPosition, originalHeadLocalRotation);
        }

        public void ApplyRecoil(Vector3 impulse, Vector3 forcePosition)
        {
            playerPhysics.NotifyWeaponFired();
            ApplyImpulse(impulse, forcePosition);
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 forcePosition)
        {
            jumpQueued = false;
            moveInput = Vector2.zero;
            playerPhysics.ApplyImpulse(impulse, forcePosition);
        }

        private void ApplyMovement()
        {
            Vector3 desiredVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
            Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 velocityChange = desiredVelocity - currentHorizontalVelocity;

            float acceleration = moveInput.sqrMagnitude > 0f ? groundAcceleration : groundDeceleration;
            if (!playerPhysics.IsGrounded)
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
            if (!playerPhysics.IsGrounded)
            {
                return;
            }

            float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            Vector3 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, 0f) + jumpSpeed;
            body.linearVelocity = velocity;
            playerPhysics.MarkAirborne();
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
