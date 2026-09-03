using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class VehicleStationUser : MonoBehaviour
    {
        private Rigidbody body;
        private Collider playerCollider;
        private Transform originalParent;

        public VehicleInteractionPoint CurrentStation { get; private set; }
        public bool IsUsingStation => CurrentStation != null;
        public bool IsDriving => CurrentStation != null && CurrentStation.StationType == VehicleStationType.Driver;
        public bool IsCrew => CurrentStation != null && CurrentStation.StationType == VehicleStationType.Crew;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            originalParent = transform.parent;
        }

        public bool TryEnterStation(VehicleInteractionPoint station)
        {
            if (station == null || CurrentStation != null || !station.TryUse(gameObject))
            {
                return false;
            }

            CurrentStation = station;
            if (body != null)
            {
                body.isKinematic = true;
                body.detectCollisions = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }
            transform.SetParent(station.OperatingPosition, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return true;
        }

        public bool TryLeaveStation()
        {
            VehicleInteractionPoint station = CurrentStation;
            return station != null && station.TryRelease(gameObject);
        }

        internal void NotifyStationReleased(VehicleInteractionPoint station)
        {
            if (CurrentStation == station)
            {
                CurrentStation = null;
                transform.SetParent(originalParent, true);
                transform.SetPositionAndRotation(station.ExitPosition.position, station.ExitPosition.rotation);
                if (playerCollider != null)
                {
                    playerCollider.enabled = true;
                }
                if (body != null)
                {
                    body.isKinematic = false;
                    body.detectCollisions = true;
                    body.linearVelocity = station.Vehicle != null ? station.Vehicle.Body.GetPointVelocity(transform.position) : Vector3.zero;
                }
            }
        }

        private void OnDisable()
        {
            TryLeaveStation();
        }
    }
}
