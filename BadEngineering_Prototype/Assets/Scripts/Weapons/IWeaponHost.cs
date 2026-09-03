using UnityEngine;

namespace BadEngineering.Weapons
{
    /// <summary>A physical object that can carry a weapon and receive its recoil.</summary>
    public interface IWeaponHost
    {
        Transform WeaponAttachRoot { get; }
        Rigidbody Body { get; }
        MonoBehaviour HostBehaviour { get; }
    }
}
