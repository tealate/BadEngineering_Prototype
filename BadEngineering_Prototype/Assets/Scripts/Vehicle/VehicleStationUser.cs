using UnityEngine;

namespace BadEngineering.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class VehicleStationUser : MonoBehaviour
    {
        public VehicleInteractionPoint CurrentStation { get; private set; }
        public bool IsUsingStation => CurrentStation != null;

        public bool TryEnterStation(VehicleInteractionPoint station)
        {
            if (station == null || CurrentStation != null || !station.TryUse(gameObject))
            {
                return false;
            }

            CurrentStation = station;
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
<<<<<<< Updated upstream
=======
                if (station.StationType == VehicleStationType.Driver)
                {
                    station.Vehicle?.SetMovementInput(VehicleInput.None);
                }
>>>>>>> Stashed changes
                CurrentStation = null;
            }
        }

        private void OnDisable()
        {
            TryLeaveStation();
        }
    }
}
