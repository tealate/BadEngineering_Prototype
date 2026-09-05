using UnityEngine;
using BadEngineering.Player;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class VehicleStationUser : MonoBehaviour
    {
        private Rigidbody body;
        private Collider playerCollider;
        private Transform originalParent;
        private FirstPersonRigidbodyController firstPersonController;
        private Renderer[] playerRenderers;
        private bool[] rendererStates;

        public VehicleInteractionPoint CurrentStation { get; private set; }
        public bool IsUsingStation => CurrentStation != null;
        public bool IsDriving => CurrentStation != null && CurrentStation.StationType == VehicleStationType.Driver;
        public bool IsCrew => CurrentStation != null && CurrentStation.StationType == VehicleStationType.Crew;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            originalParent = transform.parent;
            firstPersonController = GetComponent<FirstPersonRigidbodyController>();
            playerRenderers = GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[playerRenderers.Length];
        }

        public bool TryEnterStation(VehicleInteractionPoint station)
        {
            if (station == null || CurrentStation != null || !station.TryUse(gameObject))
            {
                return false;
            }

            CurrentStation = station;
            if (station.StationType == VehicleStationType.Driver)
            {
                PlayerWeaponSlots slots = GetComponent<PlayerWeaponSlots>();
                slots?.PrimaryReleased();
                slots?.SecondaryReleased();
            }
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }
            transform.SetParent(station.OperatingPosition, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            firstPersonController?.EnterVehicleView(station.CameraAnchor, station.CameraOffset);
            SetPlayerVisible(false);
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
                if (station.StationType == VehicleStationType.Driver)
                {
                    station.Vehicle?.SetMovementInput(VehicleInput.None);
                }
                CurrentStation = null;
                transform.SetParent(originalParent, true);
                transform.SetPositionAndRotation(station.ExitPosition.position, station.ExitPosition.rotation);
                firstPersonController?.ExitVehicleView();
                SetPlayerVisible(true);
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

        private void SetPlayerVisible(bool visible)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                Renderer playerRenderer = playerRenderers[i];
                if (playerRenderer == null) continue;
                if (!visible)
                {
                    rendererStates[i] = playerRenderer.enabled;
                    playerRenderer.enabled = false;
                }
                else
                {
                    playerRenderer.enabled = rendererStates[i];
                }
            }
        }

        private void OnDisable()
        {
            // SetParent is forbidden while Unity is activating/deactivating an
            // ancestor (for example when exiting Play mode). Keep the station
            // relationship in that case; it remains valid if the hierarchy is
            // enabled again and Unity will discard it when objects are destroyed.
            if (gameObject.activeInHierarchy)
            {
                TryLeaveStation();
            }
        }
    }
}
