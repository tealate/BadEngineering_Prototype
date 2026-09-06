using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Player
{
    /// <summary>通常・運転・武器Aimのローカル表示。カメラを武器Host階層から独立させる。</summary>
    public sealed class PlayerViewController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float vehicleDistance = 8f;
        [SerializeField, Min(0f)] private float vehicleHeight = 5f;
        [SerializeField, Min(0.1f)] private float followSpeed = 10f;
        [SerializeField] private bool avoidObstacles = true;
        private Transform normalAnchor;
        private PlayerWeaponSlots slots;
        private VehicleStationUser station;
        public Camera ViewCamera { get; private set; }
        public bool IsAiming => slots != null && slots.EquippedWeapon is TestProjectileWeapon gun && gun.IsAiming;

        private void Start()
        {
            slots = GetComponent<PlayerWeaponSlots>();
            station = GetComponent<VehicleStationUser>();
            ViewCamera = GetComponentInChildren<Camera>();
            if (ViewCamera == null) return;
            normalAnchor = ViewCamera.transform.parent;
            Transform heldRoot = GetComponent<WeaponHost>().WeaponAttachRoot;
            if (heldRoot.IsChildOf(ViewCamera.transform)) heldRoot.SetParent(normalAnchor, true);
            ViewCamera.transform.SetParent(null, true);
        }

        private void LateUpdate()
        {
            if (ViewCamera == null || normalAnchor == null) return;
            if (IsAiming && slots.EquippedWeapon is TestProjectileWeapon gun)
            {
                Vector3 offset = gun.Settings != null ? gun.Settings.aimCameraOffset : new Vector3(0f, 0.3f, -0.35f);
                ViewCamera.transform.SetPositionAndRotation(gun.transform.position + gun.transform.rotation * offset, gun.transform.rotation);
                return;
            }
            if (!station.IsDriving)
            {
                ViewCamera.transform.SetPositionAndRotation(normalAnchor.position, normalAnchor.rotation);
                return;
            }
            Transform vehicle = station.CurrentStation.Vehicle.transform;
            Vector3 target = vehicle.position;
            Vector3 back = Vector3.ProjectOnPlane(normalAnchor.forward, Vector3.up).normalized;
            if (back.sqrMagnitude < 0.01f) back = vehicle.forward;
            Vector3 desired = target - back * vehicleDistance + Vector3.up * vehicleHeight;
            Vector3 offsetToCamera = desired - target;
            if (avoidObstacles && Physics.Raycast(target, offsetToCamera.normalized, out RaycastHit hit, offsetToCamera.magnitude,
                1 << 0, QueryTriggerInteraction.Ignore)) desired = hit.point + hit.normal * 0.3f;
            ViewCamera.transform.position = Vector3.Lerp(ViewCamera.transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            ViewCamera.transform.rotation = Quaternion.LookRotation(target - ViewCamera.transform.position, Vector3.up);
        }

        private void OnDestroy()
        {
            if (ViewCamera != null) Destroy(ViewCamera.gameObject);
        }
    }
}
