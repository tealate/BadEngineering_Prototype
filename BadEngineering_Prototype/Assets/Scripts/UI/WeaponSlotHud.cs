using BadEngineering.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadEngineering.UI
{
    public sealed class WeaponSlotHud : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSlots weaponSlots;

        private readonly SlotView[] slotViews = new SlotView[3];
        private readonly Color selectedColor = new Color(0.95f, 0.62f, 0.12f, 0.95f);
        private readonly Color normalColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);

        private void Awake()
        {
            BuildLayout();
        }

        private void Update()
        {
            if (weaponSlots == null)
            {
                weaponSlots = FindFirstObjectByType<PlayerWeaponSlots>();
            }

            Refresh();
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
        }

        private void WeaponSlotText(int index, out string label, out bool selected)
        {
            selected = weaponSlots != null && weaponSlots.EquippedSlotIndex == index;
            if (weaponSlots == null || index >= weaponSlots.SlotCount)
            {
                label = $"{index + 1}: Empty";
                return;
            }

            var weapon = weaponSlots.GetWeapon(index);
            label = weapon == null ? $"{index + 1}: Empty" : $"{index + 1}: {weapon.DisplayName}";
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
