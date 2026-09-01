using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerWeaponSlots : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int slotCount = 3;

        private Weapon[] slots;
        private Rigidbody ownerBody;
        private int equippedSlot = -1;

        public Weapon EquippedWeapon => equippedSlot >= 0 ? slots[equippedSlot] : null;
        public int SlotCount => slots != null ? slots.Length : slotCount;
        public int EquippedSlotIndex => equippedSlot;

        public Weapon GetWeapon(int slotIndex)
        {
            return slots != null && slotIndex >= 0 && slotIndex < slots.Length ? slots[slotIndex] : null;
        }

        private void Awake()
        {
            ownerBody = GetComponent<Rigidbody>();
            slots = new Weapon[slotCount];

            Weapon[] discoveredWeapons = GetComponentsInChildren<Weapon>(true);
            for (int i = 0; i < discoveredWeapons.Length && i < slots.Length; i++)
            {
                slots[i] = discoveredWeapons[i];
                slots[i].SetOwner(transform, ownerBody);
                slots[i].SetHeld(false);
            }

            EquipSlot(0);
        }

        public void EquipSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || equippedSlot == slotIndex)
            {
                return;
            }

            if (EquippedWeapon != null)
            {
                EquippedWeapon.SetHeld(false);
            }

            equippedSlot = slotIndex;
            if (EquippedWeapon != null)
            {
                EquippedWeapon.SetHeld(true);
            }
        }

        public void PrimaryPressed()
        {
            EquippedWeapon?.PrimaryPressed();
        }

        public void PrimaryReleased()
        {
            EquippedWeapon?.PrimaryReleased();
        }

        public void SecondaryPressed()
        {
            EquippedWeapon?.SecondaryPressed();
        }

        public void SecondaryReleased()
        {
            EquippedWeapon?.SecondaryReleased();
        }
    }
}
