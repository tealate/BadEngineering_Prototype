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
        [SerializeField, Min(0f)] private float lossOfControlImpulse = 2.5f;
        [SerializeField, Min(0f)] private float collisionLossOfControlImpulse = 180f;
        [SerializeField, Min(0f)] private float minimumUncontrolledDuration = 0.6f;
        [SerializeField, Min(0f)] private float uncontrolledAngularDamping = 2f;

        [Header("Recovery")]
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
        private float normalAngularDamping;
        private float uncontrolledUntil;
        private float stableSince = -1f;

        public FirstPersonRigidbodyController.PhysicalState State { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool CanMove => State == FirstPersonRigidbodyController.PhysicalState.Normal;
        public event Action<FirstPersonRigidbodyController.PhysicalState> StateChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            normalAngularDamping = body.angularDamping;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (body.isKinematic)
            {
                return;
            }

            IsGrounded = CheckGrounded();
            if (State == FirstPersonRigidbodyController.PhysicalState.Uncontrolled)
            {
                TryStartRecovering();
            }
            else if (State == FirstPersonRigidbodyController.PhysicalState.Recovering)
            {
                ApplyRecoveryTorque();
            }
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 forcePosition)
        {
            body.AddForceAtPosition(impulse, forcePosition, ForceMode.Impulse);
            if (impulse.magnitude >= lossOfControlImpulse)
            {
                EnterUncontrolled();
            }
        }

        public void MarkAirborne()
        {
            IsGrounded = false;
        }

        public void EnterUncontrolled()
        {
            uncontrolledUntil = Time.time + minimumUncontrolledDuration;
            SetState(FirstPersonRigidbodyController.PhysicalState.Uncontrolled);
            stableSince = -1f;
            body.angularDamping = Mathf.Max(normalAngularDamping, uncontrolledAngularDamping);
            body.constraints &= ~(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (State == FirstPersonRigidbodyController.PhysicalState.Normal &&
                collision.impulse.magnitude >= collisionLossOfControlImpulse)
            {
                EnterUncontrolled();
            }
        }

        private void TryStartRecovering()
        {
            if (Time.time < uncontrolledUntil || !IsGrounded ||
                body.linearVelocity.magnitude > recoveryLinearSpeed ||
                body.angularVelocity.magnitude > recoveryAngularSpeed)
            {
                return;
            }

            SetState(FirstPersonRigidbodyController.PhysicalState.Recovering);
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
                    FinishRecovery();
                }
            }
            else
            {
                stableSince = -1f;
            }
        }

        private void FinishRecovery()
        {
            SetState(FirstPersonRigidbodyController.PhysicalState.Normal);
            body.angularDamping = normalAngularDamping;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            stableSince = -1f;
        }

        private void SetState(FirstPersonRigidbodyController.PhysicalState nextState)
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
    }
}
