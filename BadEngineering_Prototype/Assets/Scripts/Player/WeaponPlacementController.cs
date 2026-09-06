using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Player
{
    public enum PlacementMode { Position, RotateX, RotateY, RotateZ }

    /// <summary>VisualだけのGhostを生成し、設置候補をVehicleローカル座標で保持する。</summary>
    public sealed class WeaponPlacementController : MonoBehaviour
    {
        [SerializeField] private Material previewMaterial;
        [SerializeField, Min(0.1f)] private float reach = 3f;
        [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.3f;
        [SerializeField, Min(0f), Tooltip("0なら連続回転。それ以外は角度刻み（度）。")] private float snapDegrees;
        private PlayerWeaponSlots slots;
        private GameObject ghost;
        private Weapon previewWeapon;
        private VehicleWeaponSurface surface;
        private Vector3 localPosition, angles;
        private Quaternion baseRotation;
        public PlacementMode Mode { get; private set; }
        public bool HasPreview => ghost != null && ghost.activeSelf;
        public bool ConsumesLook => HasPreview && Mode != PlacementMode.Position;
        public Vector3 Position => surface.transform.TransformPoint(localPosition);
        public Quaternion Rotation => surface.transform.rotation * baseRotation * Quaternion.Euler(SnappedAngles());
        public VehicleWeaponSurface Surface => surface;

        private void Awake() => slots = GetComponent<PlayerWeaponSlots>();

        public void Refresh(Camera camera, Vector2 mouseDelta, bool cycle)
        {
            Weapon selected = slots.EquippedWeapon;
            if (selected == null || selected.State != WeaponState.Held ||
                selected is TestProjectileWeapon gun && gun.IsAiming || GetComponent<VehicleStationUser>().IsDriving)
            { Clear(); return; }
            if (previewWeapon != selected) Clear();
            if (camera == null) return;
            if (Mode == PlacementMode.Position || surface == null)
            {
                if (!Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Collide) ||
                    hit.collider.GetComponentInParent<Weapon>() != null ||
                    (surface = hit.collider.GetComponent<VehicleWeaponSurface>()) == null)
                { Clear(); return; }
                localPosition = surface.transform.InverseTransformPoint(hit.point + hit.normal * 0.05f);
                WeaponSettings settings = selected.Settings;
                Quaternion basis = surface.Host.transform.rotation;
                if (settings != null && settings.placementBasis == PlacementBasis.SurfaceNormal)
                {
                    Vector3 forward = Vector3.ProjectOnPlane(surface.Host.transform.forward, hit.normal);
                    if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(surface.Host.transform.up, hit.normal);
                    basis = Quaternion.LookRotation(forward, hit.normal);
                }
                baseRotation = Quaternion.Inverse(surface.transform.rotation) * basis * Quaternion.Euler(settings != null ? settings.rotationOffset : Vector3.zero);
            }
            if (Vector3.Distance(transform.position, Position) > reach + 1f) { Clear(); return; }
            if (ghost == null) BuildGhost(selected);
            if (cycle) Mode = (PlacementMode)(((int)Mode + 1) % 4);
            if (Mode != PlacementMode.Position) angles[(int)Mode - 1] += mouseDelta.x * rotationSensitivity;
            ghost.transform.SetPositionAndRotation(Position, Rotation);
        }

        public bool Confirm()
        {
            if (!HasPreview || surface == null) return false;
            bool result = surface.ConfirmPlacement(gameObject, Position, Rotation);
            Clear();
            return result;
        }

        private Vector3 SnappedAngles()
        {
            if (snapDegrees <= 0f) return angles;
            return new Vector3(Mathf.Round(angles.x / snapDegrees), Mathf.Round(angles.y / snapDegrees), Mathf.Round(angles.z / snapDegrees)) * snapDegrees;
        }

        private void BuildGhost(Weapon weapon)
        {
            previewWeapon = weapon;
            ghost = new GameObject("Weapon Ghost Preview");
            foreach (MeshFilter source in weapon.GetComponentsInChildren<MeshFilter>(true))
            {
                GameObject visual = new GameObject("Preview Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                visual.transform.SetParent(ghost.transform, false);
                visual.transform.localPosition = Vector3.Scale(weapon.transform.InverseTransformPoint(source.transform.position), weapon.transform.lossyScale);
                visual.transform.localRotation = Quaternion.Inverse(weapon.transform.rotation) * source.transform.rotation;
                visual.transform.localScale = source.transform.lossyScale;
                visual.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = visual.GetComponent<MeshRenderer>();
                var materials = new Material[source.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = previewMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void Clear()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null; previewWeapon = null; surface = null;
            Mode = PlacementMode.Position; angles = Vector3.zero;
        }
        private void OnDisable() => Clear();
    }
}
