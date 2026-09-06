using System;
using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Network
{
    public struct ActorSnapshot : INetworkSerializable, IEquatable<ActorSnapshot>
    {
        public Vector3 position, velocity, angularVelocity, localPosition;
        public Quaternion rotation, localRotation, headRotation;
        public ulong owner, host;
        public int state, slot, selection, seat, tire;
        public bool aim;
        public float zero, mass;
        public float steering;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref position); s.SerializeValue(ref rotation); s.SerializeValue(ref velocity); s.SerializeValue(ref angularVelocity);
            s.SerializeValue(ref localPosition); s.SerializeValue(ref localRotation); s.SerializeValue(ref headRotation);
            s.SerializeValue(ref owner); s.SerializeValue(ref host); s.SerializeValue(ref state); s.SerializeValue(ref slot);
            s.SerializeValue(ref selection); s.SerializeValue(ref seat); s.SerializeValue(ref tire);
            s.SerializeValue(ref aim); s.SerializeValue(ref zero); s.SerializeValue(ref mass);
            s.SerializeValue(ref steering);
        }
        public bool Equals(ActorSnapshot other) => position == other.position && rotation == other.rotation && velocity == other.velocity &&
            angularVelocity == other.angularVelocity && localPosition == other.localPosition && localRotation == other.localRotation &&
            headRotation == other.headRotation && owner == other.owner && host == other.host && state == other.state &&
            slot == other.slot && selection == other.selection && seat == other.seat && tire == other.tire && aim == other.aim && zero == other.zero && mass == other.mass && steering == other.steering;
    }

    /// <summary>Host確定状態の同期。親変更はGameplay状態に従って再構築し、Clientでは物理を実行しない。</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkActor : NetworkBehaviour
    {
        public const ulong NoObject = ulong.MaxValue;
        [SerializeField, Min(1f)] private float interpolationRate = 18f;
        private readonly NetworkVariable<ActorSnapshot> snapshot = new();
        private Rigidbody body;
        private Weapon weapon;
        private TireItem part;
        private PlayerWeaponSlots slots;
        private VehicleStationUser station;
        private float nextPublish;
        private bool received;
        public ActorSnapshot CurrentSnapshot => snapshot.Value;
        private void Awake()
        {
            body = GetComponent<Rigidbody>(); weapon = GetComponent<Weapon>(); part = GetComponent<TireItem>();
            slots = GetComponent<PlayerWeaponSlots>(); station = GetComponent<VehicleStationUser>();
        }
        public override void OnNetworkSpawn()
        {
            if (IsServer) snapshot.Value = Capture();
            else MakeKinematic();
        }
        private void Update()
        {
            if (!IsSpawned) return;
            if (IsServer)
            {
                if (Time.unscaledTime >= nextPublish) { snapshot.Value = Capture(); nextPublish = Time.unscaledTime + 0.05f; }
                return;
            }
            Apply(snapshot.Value);
        }
        private static ulong Id(Component component)
        {
            var obj = component != null ? component.GetComponent<NetworkObject>() : null;
            return obj != null && obj.IsSpawned ? obj.NetworkObjectId : NoObject;
        }
        private T Find<T>(ulong id) where T : Component => id != NoObject && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj) ? obj.GetComponent<T>() : null;
        private ActorSnapshot Capture()
        {
            var result = new ActorSnapshot { position = transform.position, rotation = transform.rotation,
                localPosition = transform.localPosition, localRotation = transform.localRotation,
                owner = NoObject, host = NoObject, seat = -1, slot = -1, selection = -1, tire = -1,
                velocity = body != null ? body.linearVelocity : Vector3.zero,
                angularVelocity = body != null ? body.angularVelocity : Vector3.zero, mass = body != null ? body.mass : 0f };
            if (weapon != null)
            {
                result.owner = Id(weapon.Owner); result.host = Id(weapon.Host?.HostBehaviour);
                result.state = (int)weapon.State; result.slot = weapon.Owner != null ? weapon.Owner.IndexOf(weapon) : -1;
                if (weapon is TestProjectileWeapon gun) { result.aim = gun.IsAiming; result.zero = gun.ZeroDistance; }
            }
            if (part != null)
            {
                result.owner = Id(part.Carrier); result.slot = part.Carrier != null ? part.Carrier.IndexOf(part) : -1;
                result.tire = DirectIpSession.Instance.TireIndex(part.Definition);
            }
            var wheels = GetComponentInChildren<WheelSystem>();
            if (wheels != null && slots == null)
            {
                result.tire = DirectIpSession.Instance.TireIndex(wheels.CurrentTire);
                result.steering = GetComponent<VehiclePhysicsController>().MovementInput.Steering;
            }
            if (slots != null)
            {
                result.selection = slots.EquippedSlotIndex;
                result.headRotation = GetComponent<FirstPersonRigidbodyController>().HeadPivot.localRotation;
                if (station.CurrentStation != null)
                { result.host = Id(station.CurrentStation.Vehicle); result.seat = (int)station.CurrentStation.StationType; }
            }
            return result;
        }
        private void Apply(ActorSnapshot value)
        {
            if (weapon != null)
            {
                var owner = Find<PlayerWeaponSlots>(value.owner);
                var host = Find<WeaponHost>(value.host);
                if (value.owner != NoObject && owner == null || value.host != NoObject && host == null) return;
                weapon.ApplyReplica(owner, host, (WeaponState)value.state);
                owner?.SetReplicaItem(value.slot, weapon);
                weapon.SetSelected(owner != null && owner.EquippedSlotIndex == value.slot);
                if (weapon is TestProjectileWeapon gun) gun.SetReplicaAim(value.aim, value.zero);
            }
            if (part != null)
            {
                var owner = Find<PlayerWeaponSlots>(value.owner);
                if (value.owner != NoObject && owner == null) return;
                part.ApplyReplica(owner, DirectIpSession.Instance.TireAt(value.tire));
                owner?.SetReplicaItem(value.slot, part);
                part.SetSelected(owner == null || owner.EquippedSlotIndex == value.slot);
            }
            if (station != null)
            {
                var vehicle = Find<VehiclePhysicsController>(value.host);
                if (value.seat >= 0 && vehicle == null) return;
                var desired = value.seat < 0 ? null : Array.Find(vehicle.GetComponentsInChildren<VehicleInteractionPoint>(), seat => (int)seat.StationType == value.seat);
                if (station.CurrentStation != desired)
                {
                    station.TryLeaveStation();
                    if (desired != null) station.TryEnterStation(desired);
                }
                GetComponent<FirstPersonRigidbodyController>().HeadPivot.localRotation = value.headRotation;
                slots.SetReplicaSelection(value.selection);
            }
            var wheels = GetComponentInChildren<WheelSystem>();
            if (wheels != null && slots == null)
            {
                wheels.ReplaceTire(DirectIpSession.Instance.TireAt(value.tire));
                wheels.ApplyInput(new VehicleInput(0f, value.steering, 0f));
            }
            MakeKinematic();
            if (body != null && value.mass > 0f) body.mass = value.mass;
            float blend = received ? 1f - Mathf.Exp(-interpolationRate * Time.deltaTime) : 1f;
            if (transform.parent != null && (weapon != null || part != null || station != null && station.IsUsingStation))
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, value.localPosition, blend);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, value.localRotation, blend);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, value.position, blend);
                transform.rotation = Quaternion.Slerp(transform.rotation, value.rotation, blend);
            }
            received = true;
        }
        private void MakeKinematic()
        {
            body = GetComponent<Rigidbody>();
            if (body == null) return;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
        }
    }
}
