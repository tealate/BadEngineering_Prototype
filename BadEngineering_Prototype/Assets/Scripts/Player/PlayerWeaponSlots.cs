using System;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(WeaponHost))]
    public sealed class PlayerWeaponSlots : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int slotCount = 3;
        [SerializeField, Min(0f)] private float dropDistance = 1.2f;

        private Weapon[] slots;
        private int equippedSlot = -1;

        public Weapon EquippedWeapon => equippedSlot >= 0 && equippedSlot < slots.Length ? slots[equippedSlot] : null;
        public int SlotCount => slots != null ? slots.Length : slotCount;
        public int EquippedSlotIndex => equippedSlot;
        public event Action SlotsChanged;

        public Weapon GetWeapon(int slotIndex)
        {
            return slots != null && slotIndex >= 0 && slotIndex < slots.Length ? slots[slotIndex] : null;
        }

        private void Awake()
        {
            slots = new Weapon[slotCount];

            Weapon[] discoveredWeapons = GetComponentsInChildren<Weapon>(true);
            foreach (Weapon weapon in discoveredWeapons)
            {
                AddOwnedWeapon(weapon);
            }

            SelectSlot(0);
        }

        public bool AddOwnedWeapon(Weapon weapon)
        {
            if (weapon == null || Array.IndexOf(slots, weapon) >= 0)
            {
                return false;
            }

            int emptySlot = Array.FindIndex(slots, item => item == null);
            if (emptySlot < 0)
            {
                return false;
            }

            slots[emptySlot] = weapon;
            weapon.SetOwner(this);
            weapon.HoldByOwner();
            if (equippedSlot < 0)
            {
                SelectSlot(emptySlot);
            }
            else
            {
                weapon.SetSelected(emptySlot == equippedSlot);
            }
            SlotsChanged?.Invoke();
            return true;
        }

        public void RemoveOwnedWeapon(Weapon weapon)
        {
            int index = Array.IndexOf(slots, weapon);
            if (index < 0)
            {
                return;
            }

            slots[index] = null;
            if (equippedSlot == index)
            {
                equippedSlot = -1;
            }
            SlotsChanged?.Invoke();
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            EquippedWeapon?.SetSelected(false);
            equippedSlot = slots[slotIndex] != null ? slotIndex : -1;
            EquippedWeapon?.SetSelected(true);
            SlotsChanged?.Invoke();
        }

        public void EquipSlot(int slotIndex) => SelectSlot(slotIndex);

        public void DropSelectedWeapon()
        {
            Weapon weapon = EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            Vector3 position = weapon.State == WeaponState.Attached
                ? weapon.transform.position + Vector3.up * 0.2f
                : transform.position + transform.forward * dropDistance + Vector3.up * 0.5f;
            Vector3 inheritedVelocity = weapon.Host?.Body != null
                ? weapon.Host.Body.GetPointVelocity(position)
                : body.linearVelocity;
            weapon.Drop(position, inheritedVelocity);
        }

        public void PrimaryPressed() => EquippedWeapon?.PrimaryPressed();
        public void PrimaryReleased() => EquippedWeapon?.PrimaryReleased();
        public void SecondaryPressed() => EquippedWeapon?.SecondaryPressed();
        public void SecondaryReleased() => EquippedWeapon?.SecondaryReleased();
    }
}
