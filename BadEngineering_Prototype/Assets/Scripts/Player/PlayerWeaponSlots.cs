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

        private MonoBehaviour[] slots;
        private int equippedSlot = -1;

        public MonoBehaviour EquippedItem => slots != null && equippedSlot >= 0 && equippedSlot < slots.Length ? slots[equippedSlot] : null;
        public Weapon EquippedWeapon => EquippedItem as Weapon;
        public MonoBehaviour GetItem(int index) => slots != null && index >= 0 && index < slots.Length ? slots[index] : null;
        public int IndexOf(MonoBehaviour item) => slots != null ? Array.IndexOf(slots, item) : -1;
        public void SetReplicaItem(int index, MonoBehaviour item)
        {
            if (index >= 0 && index < slots.Length) slots[index] = item;
        }
        public void SetReplicaSelection(int index) => equippedSlot = index;
        public int SlotCount => slots != null ? slots.Length : slotCount;
        public int EquippedSlotIndex => equippedSlot;
        public event Action SlotsChanged;

        public Weapon GetWeapon(int slotIndex)
        {
            return GetItem(slotIndex) as Weapon;
        }

        private void Awake()
        {
            slots = new MonoBehaviour[slotCount];

            Weapon[] discoveredWeapons = GetComponentsInChildren<Weapon>(true);
            foreach (Weapon weapon in discoveredWeapons)
            {
                AddOwnedWeapon(weapon);
            }

            SelectSlot(0);
        }

        public bool AddOwnedWeapon(Weapon weapon)
        {
            if (weapon == null || weapon.Owner != null || weapon.State != WeaponState.Dropped ||
                Array.IndexOf(slots, weapon) >= 0)
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
            if (!weapon.HoldByOwner())
            {
                weapon.SetOwner(null);
                slots[emptySlot] = null;
                return false;
            }
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
            RemoveItem(weapon);
        }

        public void RemoveItem(MonoBehaviour item)
        {
            int index = Array.IndexOf(slots, item);
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
            if (EquippedItem is Vehicle.TireItem oldPart) oldPart.SetSelected(false);
            equippedSlot = slots[slotIndex] != null ? slotIndex : -1;
            EquippedWeapon?.SetSelected(true);
            if (EquippedItem is Vehicle.TireItem newPart) newPart.SetSelected(true);
            SlotsChanged?.Invoke();
        }

        public void EquipSlot(int slotIndex) => SelectSlot(slotIndex);

        public void DropSelectedWeapon()
        {
            if (EquippedItem is Vehicle.TireItem part)
            {
                part.Drop(transform.position + transform.forward * dropDistance + Vector3.up * 0.5f, GetComponent<Rigidbody>().linearVelocity);
                return;
            }
            Weapon weapon = EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * 0.5f;
            Vector3 inheritedVelocity = weapon.Host?.Body != null
                ? weapon.Host.Body.GetPointVelocity(position)
                : body.linearVelocity;
            weapon.Drop(position, inheritedVelocity);
        }

        public void PrimaryPressed() => EquippedWeapon?.PrimaryPressed();
        public void PrimaryReleased() => EquippedWeapon?.PrimaryReleased();
        public void SecondaryPressed() => EquippedWeapon?.SecondaryPressed();
        public void SecondaryReleased() => EquippedWeapon?.SecondaryReleased();

        public bool AddPart(Vehicle.TireItem part)
        {
            if (part == null || part.Carrier != null || Array.IndexOf(slots, part) >= 0) return false;
            int index = Array.FindIndex(slots, item => item == null);
            if (index < 0) return false;
            slots[index] = part;
            part.Hold(this);
            if (equippedSlot < 0) SelectSlot(index);
            part.SetSelected(equippedSlot == index);
            SlotsChanged?.Invoke();
            return true;
        }
    }
}
