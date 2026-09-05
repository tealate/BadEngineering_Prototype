using UnityEngine;

namespace BadEngineering.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Health))]
    public sealed class SimpleEnemy : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveAcceleration = 8f;
        [SerializeField, Min(0f)] private float maximumSpeed = 3f;
        [SerializeField, Min(0f)] private float stopDistance = 1.5f;
        [SerializeField] private Transform target;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                FirstPersonTarget();
            }
            if (target == null)
            {
                return;
            }

            Vector3 toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            if (toTarget.magnitude <= stopDistance)
            {
                return;
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            if (horizontalVelocity.magnitude < maximumSpeed)
            {
                body.AddForce(toTarget.normalized * moveAcceleration, ForceMode.Acceleration);
            }
            body.MoveRotation(Quaternion.LookRotation(toTarget.normalized, Vector3.up));
        }

        private void FirstPersonTarget()
        {
            BadEngineering.Player.FirstPersonRigidbodyController player =
                FindFirstObjectByType<BadEngineering.Player.FirstPersonRigidbodyController>();
            target = player != null ? player.transform : null;
        }
    }
}
