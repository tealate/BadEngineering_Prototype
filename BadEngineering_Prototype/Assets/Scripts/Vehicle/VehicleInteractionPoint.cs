using System;
using BadEngineering.Interaction;
using UnityEngine;

namespace BadEngineering.Vehicle
{
    public enum VehicleStationType
    {
        Driver,
        Gunner
    }

    [DisallowMultipleComponent]
    public sealed class VehicleInteractionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private VehicleStationType stationType;
        [SerializeField] private Transform operatingPosition;

        private GameObject currentUser;

        public VehicleStationType StationType => stationType;
        public Transform OperatingPosition => operatingPosition != null ? operatingPosition : transform;
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
            if (currentUser != null)
            {
                TryRelease(currentUser);
            }
        }
    }
}
