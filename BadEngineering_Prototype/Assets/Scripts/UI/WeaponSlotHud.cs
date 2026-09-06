using BadEngineering.Player;
using BadEngineering.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadEngineering.UI
{
    public sealed class WeaponSlotHud : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSlots weaponSlots;
        [SerializeField] private FirstPersonRigidbodyController playerController;
        [SerializeField] private VehicleStationUser stationUser;
        [SerializeField] private VehiclePhysicsController vehicle;

        private readonly SlotView[] slotViews = new SlotView[3];
        private readonly Color selectedColor = new Color(0.95f, 0.62f, 0.12f, 0.95f);
        private readonly Color normalColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        private TextMeshProUGUI statusLabel;

        private void Awake()
        {
            BuildLayout();
            BuildStatus();
        }

        private void Update()
        {
            if (weaponSlots == null)
            {
                foreach (var candidate in FindObjectsByType<PlayerWeaponSlots>(FindObjectsSortMode.None))
                    if (BadEngineering.Network.GameplayAuthority.IsLocal(candidate.gameObject)) { weaponSlots = candidate; break; }
            }
            if (playerController == null)
            {
                playerController = weaponSlots != null ? weaponSlots.GetComponent<FirstPersonRigidbodyController>() : null;
            }
            if (stationUser == null && weaponSlots != null)
            {
                stationUser = weaponSlots.GetComponent<VehicleStationUser>();
            }
            if (vehicle == null)
            {
                vehicle = FindFirstObjectByType<VehiclePhysicsController>();
            }

            Refresh();
        }

        private void BuildStatus()
        {
            var status = new GameObject("Status and Controls", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = status.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(800f, 200f);
            statusLabel = status.GetComponent<TextMeshProUGUI>();
            statusLabel.font = TMP_Settings.defaultFontAsset;
            statusLabel.fontSize = 20f;
            statusLabel.color = Color.white;
        }

        private void BuildLayout()
        {
            var container = new GameObject("SlotContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.SetParent(transform, false);
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, 36f);
            containerRect.sizeDelta = new Vector2(540f, 72f);

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < slotViews.Length; i++)
            {
                slotViews[i] = CreateSlot(container.transform, i);
            }
        }

        private SlotView CreateSlot(Transform parent, int index)
        {
            var slot = new GameObject($"Slot {index + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slot.transform.SetParent(parent, false);
            slot.GetComponent<LayoutElement>().preferredWidth = 174f;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.SetParent(slot.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);

            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return new SlotView(slot.GetComponent<Image>(), text);
        }

        private void Refresh()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                WeaponSlotText(i, out string label, out bool selected);
                slotViews[i].Background.color = selected ? selectedColor : normalColor;
                slotViews[i].Label.text = label;
            }

            string physical = playerController != null ? playerController.CurrentPhysicalState.ToString() : "Unknown";
            string station = stationUser == null || !stationUser.IsUsingStation
                ? "On Foot"
                : stationUser.CurrentStation.StationType.ToString();
            string vehiclePhysics = vehicle != null
                ? $"Vehicle: {vehicle.Body.mass:0.#} kg | COM {vehicle.Body.centerOfMass:F2}"
                : "Vehicle: unavailable";
            statusLabel.text = $"State: {physical} | Station: {station}\n" +
                               vehiclePhysics + "\n" +
                               "WASD Move/Drive  Space Jump  Mouse Look/Fire\n" +
                               "E Pickup/Seat/Exit  F Attach/Recover  Q Drop  1-3 Select\n" +
                               "Middle: Placement axis  RMB: Aim  Wheel: Zero distance";
        }

        private void WeaponSlotText(int index, out string label, out bool selected)
        {
            selected = weaponSlots != null && weaponSlots.EquippedSlotIndex == index;
            if (weaponSlots == null || index >= weaponSlots.SlotCount)
            {
                label = $"{index + 1}: Empty";
                return;
            }

            var item = weaponSlots.GetItem(index);
            var weapon = item as BadEngineering.Weapons.Weapon;
            label = item == null
                ? $"{index + 1}: Empty"
                : weapon != null ? $"{index + 1}: {weapon.DisplayName} [{weapon.State}]" : $"{index + 1}: Tire";
        }

        private void OnGUI()
        {
            if (weaponSlots == null) return;
            float x = Screen.width * 0.5f, y = Screen.height * 0.5f;
            GUI.Label(new Rect(x - 6f, y - 12f, 30f, 30f), "+");
            var placement = weaponSlots.GetComponent<WeaponPlacementController>();
            if (placement != null && placement.HasPreview)
                GUI.Label(new Rect(x + 25f, y + 30f, 240f, 25f), "Placement: " + placement.Mode);
            var gun = weaponSlots.EquippedWeapon as BadEngineering.Weapons.TestProjectileWeapon;
            if (gun == null || !gun.IsAiming || gun.Settings == null || gun.Settings.aimMode != BadEngineering.Weapons.WeaponAimMode.Heavy) return;
            for (int i = -2; i <= 2; i++)
            {
                float distance = gun.ZeroDistance + i * gun.Settings.zeroDistanceStep;
                if (distance < gun.Settings.zeroDistanceRange.x || distance > gun.Settings.zeroDistanceRange.y) continue;
                GUI.Label(new Rect(x + 30f, y - i * 24f, 160f, 24f), (i == 0 ? "> " : "  ") + distance.ToString("0") + " m");
            }
            if (!gun.ZeroReachable) GUI.Label(new Rect(x + 30f, y + 80f, 200f, 25f), "Out of ballistic range");
        }

        private readonly struct SlotView
        {
            public readonly Image Background;
            public readonly TextMeshProUGUI Label;

            public SlotView(Image background, TextMeshProUGUI label)
            {
                Background = background;
                Label = label;
            }
        }
    }
}
