using System;
using BadEngineering.Interaction;
using UnityEngine;

namespace BadEngineering.Vehicle
{
    public enum VehicleStationType
    {
        Driver,
        Crew
    }

    [DisallowMultipleComponent]
    public sealed class VehicleInteractionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private VehicleStationType stationType;
        [SerializeField] private Transform operatingPosition;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private Vector3 cameraOffset = new(0f, 0.7f, 0.3f);
        [SerializeField] private Transform exitPosition;

        private GameObject currentUser;

        public VehicleStationType StationType => stationType;
        public Transform OperatingPosition => operatingPosition != null ? operatingPosition : transform;
        public Transform CameraAnchor => cameraAnchor != null ? cameraAnchor : OperatingPosition;
        public Vector3 CameraOffset => cameraAnchor != null ? Vector3.zero : cameraOffset;
        public Transform ExitPosition => exitPosition != null ? exitPosition : transform;
        public VehiclePhysicsController Vehicle => GetComponentInParent<VehiclePhysicsController>();
        public GameObject CurrentUser => currentUser;
        public bool IsOccupied => currentUser != null;

        public event Action<VehicleInteractionPoint, GameObject> UserEntered;
        public event Action<VehicleInteractionPoint, GameObject> UserExited;

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null && (currentUser == null || currentUser == interactor);
        }

        public bool TryInteract(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            if (currentUser == interactor)
            {
                return TryRelease(interactor);
            }

            VehicleStationUser stationUser = interactor.GetComponent<VehicleStationUser>();
            return currentUser == null && stationUser != null && stationUser.TryEnterStation(this);
        }

        public bool TryUse(GameObject user)
        {
            if (!CanInteract(user) || currentUser == user)
            {
                return false;
            }

            currentUser = user;
            UserEntered?.Invoke(this, user);
            return true;
        }

        public bool TryRelease(GameObject user)
        {
            if (user == null || currentUser != user)
            {
                return false;
            }

            currentUser = null;
            user.GetComponent<VehicleStationUser>()?.NotifyStationReleased(this);
            UserExited?.Invoke(this, user);
            return true;
        }

        private void OnDisable()
        {
            // Releasing reparents the user. Unity does not allow that while this
            // GameObject or one of its parents is being deactivated.
            if (gameObject.activeInHierarchy && currentUser != null)
            {
                TryRelease(currentUser);
            }
        }
    }
}
