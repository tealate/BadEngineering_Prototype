using System;
using UnityEngine;

namespace BadEngineering.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerPhysicsController : MonoBehaviour
    {
        [Header("Grounding")]
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormal = 0.6f;

        [Header("Loss of Control")]
        [SerializeField, Min(0f)] private float uncontrolledDuration = 0.6f;
        [SerializeField, Min(0f)] private float uncontrolledAngularDamping = 2f;

        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private Rigidbody body;
        private CapsuleCollider capsule;
        private float normalAngularDamping;
        private RigidbodyConstraints normalConstraints;
        private float uncontrolledUntil;
        private PhysicsMaterial contactMaterial;

        public PlayerPhysicalState State { get; private set; } = PlayerPhysicalState.Normal;
        public bool IsGrounded { get; private set; }
        public Rigidbody GroundBody { get; private set; }
        public Vector3 GroundVelocity { get; private set; }
        public bool CanMove => State == PlayerPhysicalState.Normal;
        public event Action<PlayerPhysicalState> StateChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            contactMaterial = CreateFrictionlessMaterial("Player Contact");
            capsule.sharedMaterial = contactMaterial;
            body.maxDepenetrationVelocity = 3f;
            normalAngularDamping = body.angularDamping;
            // Normal movement owns the player's facing through MoveRotation.
            // Leaving Y rotation dynamic makes collision torque fight that target rotation forever.
            normalConstraints = body.constraints | RigidbodyConstraints.FreezeRotation;
            body.constraints = normalConstraints;
        }

        private static PhysicsMaterial CreateFrictionlessMaterial(string materialName)
        {
            return new PhysicsMaterial(materialName)
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        private void OnDestroy()
        {
            if (contactMaterial != null)
                Destroy(contactMaterial);
        }

        private void FixedUpdate()
        {
            if (body.isKinematic)
            {
                return;
            }

            IsGrounded = CheckGrounded();
            if (State == PlayerPhysicalState.Uncontrolled && Time.time >= uncontrolledUntil)
            {
                FinishRecovery();
            }
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 forcePosition)
        {
            if (body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(impulse, forcePosition, ForceMode.Impulse);
        }

        public void MarkAirborne()
        {
            IsGrounded = false;
        }

        public void NotifyWeaponFired()
        {
            uncontrolledUntil = Time.time + uncontrolledDuration;
            SetState(PlayerPhysicalState.Uncontrolled);
            body.angularDamping = Mathf.Max(normalAngularDamping, uncontrolledAngularDamping);
            body.constraints = normalConstraints & ~RigidbodyConstraints.FreezeRotation;
        }

        private void FinishRecovery()
        {
            Vector3 eulerAngles = body.rotation.eulerAngles;
            body.rotation = Quaternion.Euler(0f, eulerAngles.y, 0f);
            body.angularVelocity = Vector3.zero;
            SetState(PlayerPhysicalState.Normal);
            body.angularDamping = normalAngularDamping;
            body.constraints = normalConstraints;
        }

        private void SetState(PlayerPhysicalState nextState)
        {
            if (State == nextState)
            {
                return;
            }
            State = nextState;
            StateChanged?.Invoke(State);
        }

        private bool CheckGrounded()
        {
            GroundBody = null;
            GroundVelocity = Vector3.zero;
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

            bool foundGround = false;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.collider != capsule &&
                    Vector3.Dot(hit.normal, Vector3.up) >= minimumGroundNormal &&
                    hit.distance < closestDistance)
                {
                    foundGround = true;
                    closestDistance = hit.distance;
                    GroundBody = hit.rigidbody;
                    GroundVelocity = GroundBody != null
                        ? GroundBody.GetPointVelocity(hit.point)
                        : Vector3.zero;
                }
            }
            return foundGround;
        }

    }
}
