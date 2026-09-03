using UnityEngine;
using UnityEngine.InputSystem;

namespace BadEngineering.Interaction
{
    [RequireComponent(typeof(Vehicle.VehicleStationUser))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private Vehicle.VehicleStationUser stationUser;

        private void Awake()
        {
            stationUser = GetComponent<Vehicle.VehicleStationUser>();
            if (interactionCamera == null)
            {
                interactionCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Interact();
            }
        }

        public bool Interact()
        {
            if (stationUser.CurrentStation != null)
            {
                return stationUser.TryLeaveStation();
            }

            if (interactionCamera == null ||
                !Physics.Raycast(
                    interactionCamera.transform.position,
                    interactionCamera.transform.forward,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionLayers,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            Vehicle.VehicleWeaponSurface surface = hit.collider.GetComponentInParent<Vehicle.VehicleWeaponSurface>();
            if (surface != null && surface.CanInteract(gameObject))
            {
                return surface.TryAttach(gameObject, hit.point, hit.normal);
            }

            IInteractable interactable = FindInteractable(hit.collider.transform);
            return interactable != null && interactable.CanInteract(gameObject) && interactable.TryInteract(gameObject);
        }

        private static IInteractable FindInteractable(Transform hitTransform)
        {
            MonoBehaviour[] behaviours = hitTransform.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }
    }
}
