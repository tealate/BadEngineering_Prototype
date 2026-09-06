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
        private readonly Vector3[] blockingNormals = new Vector3[8];
        private int blockingNormalCount;

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

        private bool fireHeld, aimHeld;
        private float brakeInput;
        private float lastCommandTime;
        public Camera PlayerCamera => playerCamera;
        public Transform HeadPivot => headPivot;

        private void Update()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-prototypeProbe") >= 0) return;
            if (!BadEngineering.Network.GameplayAuthority.IsLocal(gameObject)) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;
            var placement = GetComponent<WeaponPlacementController>();
            placement?.Refresh(playerCamera, mouse.delta.ReadValue(), mouse.middleButton.wasPressedThisFrame);
            var command = new PlayerCommand
            {
                move = new Vector2(ReadAxis(keyboard.aKey, keyboard.dKey), ReadAxis(keyboard.sKey, keyboard.wKey)),
                look = placement != null && placement.ConsumesLook ? Vector2.zero : mouse.delta.ReadValue(),
                scroll = mouse.scroll.ReadValue().y,
                jump = keyboard.spaceKey.wasPressedThisFrame, brake = keyboard.spaceKey.isPressed,
                fire = mouse.leftButton.isPressed, aim = mouse.rightButton.isPressed,
                drop = keyboard.qKey.wasPressedThisFrame, interact = keyboard.eKey.wasPressedThisFrame,
                place = keyboard.fKey.wasPressedThisFrame,
                slot = keyboard.digit1Key.wasPressedThisFrame ? 0 : keyboard.digit2Key.wasPressedThisFrame ? 1 : keyboard.digit3Key.wasPressedThisFrame ? 2 : -1,
                rayOrigin = playerCamera.transform.position, rayDirection = playerCamera.transform.forward
            };
            if (placement != null && placement.HasPreview)
            {
                command.hasPlacement = true;
                command.placementPosition = placement.Surface.Host.transform.InverseTransformPoint(placement.Position);
                command.placementRotation = Quaternion.Inverse(placement.Surface.Host.transform.rotation) * placement.Rotation;
                var vehicleObject = placement.Surface.Host.GetComponent<Unity.Netcode.NetworkObject>();
                command.vehicleId = vehicleObject != null ? vehicleObject.NetworkObjectId : 0;
            }
            var network = GetComponent<BadEngineering.Network.NetworkPlayer>();
            if (network != null && network.IsSpawned) network.Submit(command);
            else ApplyCommand(command);
        }

        /// <summary>ローカル・ネットワーク双方が使うHost側の操作入口。</summary>
        public void ApplyCommand(PlayerCommand command)
        {
            lastCommandTime = Time.time;
            moveInput = Vector2.ClampMagnitude(command.move, 1f);
            brakeInput = command.brake ? 1f : 0f;
            jumpQueued |= command.jump && !stationUser.IsUsingStation;
            if (!stationUser.IsDriving)
            {
                if (command.slot >= 0) weaponSlots.SelectSlot(command.slot);
                if (command.drop) weaponSlots.DropSelectedWeapon();
                if (command.aim != aimHeld)
                {
                    if (command.aim) weaponSlots.SecondaryPressed(); else weaponSlots.SecondaryReleased();
                }
                if (command.fire != fireHeld)
                {
                    if (command.fire) weaponSlots.PrimaryPressed(); else weaponSlots.PrimaryReleased();
                }
                aimHeld = command.aim; fireHeld = command.fire;
            }
            else { fireHeld = aimHeld = false; }
            var gun = weaponSlots.EquippedWeapon as BadEngineering.Weapons.TestProjectileWeapon;
            if (gun != null && gun.IsAiming) gun.ApplyAim(command.look, command.scroll);
            else ApplyLook(command.look);
            var interactor = GetComponent<BadEngineering.Interaction.PlayerInteractor>();
            if (command.interact) interactor.InteractRay(command.rayOrigin, command.rayDirection);
            if (command.place) interactor.PlaceCommand(command);
        }

        public void StopInput()
        {
            moveInput = Vector2.zero; brakeInput = 0f; jumpQueued = false;
            fireHeld = aimHeld = false;
            weaponSlots.PrimaryReleased(); weaponSlots.SecondaryReleased();
        }

        private void ApplyLook(Vector2 delta)
        {
            Vector2 lookDelta = Vector2.ClampMagnitude(delta, 500f) * mouseSensitivity;
            if (playerPhysics.CanMove && !stationUser.IsUsingStation) yaw += lookDelta.x;
            else freeLookYaw += lookDelta.x;
            pitch = Mathf.Clamp(pitch - lookDelta.y, -verticalLookLimit, verticalLookLimit);
            headPivot.localRotation = Quaternion.Euler(pitch, freeLookYaw, 0f);
        }

        private void FixedUpdate()
        {
            if (!BadEngineering.Network.GameplayAuthority.CanSimulate) return;
            if (Time.time - lastCommandTime > 0.5f) StopInput();
            if (stationUser != null && stationUser.IsUsingStation)
            {
                if (stationUser.IsDriving)
                {
                    float brake = brakeInput;
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
            blockingNormalCount = 0;
        }

        private void OnCollisionStay(Collision collision)
        {
            int remaining = blockingNormals.Length - blockingNormalCount;
            int count = Mathf.Min(collision.contactCount, remaining);
            for (int i = 0; i < count; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.y) < 0.7f)
                    blockingNormals[blockingNormalCount++] = normal;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                LockCursor();
            }
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
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
            Vector3 supportVelocity = playerPhysics.IsGrounded
                ? Vector3.ProjectOnPlane(playerPhysics.GroundVelocity, Vector3.up)
                : Vector3.zero;
            Vector3 desiredVelocity = supportVelocity +
                (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
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

            for (int i = 0; i < blockingNormalCount; i++)
            {
                Vector3 normal = blockingNormals[i];
                float intoContact = Vector3.Dot(accelerationVector, normal);
                if (intoContact < 0f)
                    accelerationVector -= normal * intoContact;
            }
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
