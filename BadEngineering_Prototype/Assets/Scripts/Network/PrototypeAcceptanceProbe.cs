using System;
using System.Collections;
using System.Linq;
using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Network
{
    /// <summary>明示的な起動引数がある場合だけ動く、複数プロセスの受入検証。</summary>
    public sealed class PrototypeAcceptanceProbe : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (!Environment.GetCommandLineArgs().Contains("-prototypeProbe")) { Destroy(this); yield break; }
            Application.logMessageReceived += OnLog;
            float deadline = Time.realtimeSinceStartup + 45f;
            var session = NetworkManager.Singleton;
            while (!session.IsConnectedClient && Time.realtimeSinceStartup < deadline) yield return null;
            if (!session.IsConnectedClient) { Fail("Connection timeout"); yield break; }
            if (session.IsServer) yield return ServerChecks(session);
            else yield return ClientChecks(session);
        }
        private IEnumerator ServerChecks(NetworkManager session)
        {
            float deadline = Time.realtimeSinceStartup + 40f;
            while (session.ConnectedClients.Count < 2 && Time.realtimeSinceStartup < deadline) yield return null;
            if (session.ConnectedClients.Count < 2) { Fail("Second player timeout"); yield break; }
            yield return new WaitForSeconds(2f);
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            var host = players.First(p => p.OwnerClientId == session.LocalClientId);
            var client = players.First(p => p.OwnerClientId != session.LocalClientId);
            var vehicle = FindFirstObjectByType<VehiclePhysicsController>();
            var seats = vehicle.GetComponentsInChildren<VehicleInteractionPoint>();
            Check(client.GetComponent<VehicleStationUser>().TryEnterStation(seats.First(s => s.StationType == VehicleStationType.Driver)), "Client enters Driver");
            Check(host.GetComponent<VehicleStationUser>().TryEnterStation(seats.First(s => s.StationType == VehicleStationType.Crew)), "Host enters Crew");
            var slots = host.GetComponent<PlayerWeaponSlots>();
            var weapon = slots.EquippedWeapon;
            var vehicleHost = vehicle.GetComponent<WeaponHost>();
            Vector3 attachment = vehicle.transform.position + vehicle.transform.up + vehicle.transform.right;
            Check(weapon.AttachTo(vehicleHost, attachment, vehicle.transform.rotation, WeaponState.Attached), "Attach");
            Check(!weapon.AttachTo(vehicleHost, attachment, vehicle.transform.rotation, WeaponState.Attached), "Attached reposition rejected");
            Check(!weapon.CanInteract(client.gameObject), "Non-owner recovery rejected");
            Check(slots.GetWeapon(0) == weapon && weapon.Owner == slots, "Owner and slot retained");
            Vector3 beforeDrive = vehicle.transform.position;
            yield return new WaitForSeconds(3f);
            Check(Vector3.Distance(beforeDrive, vehicle.transform.position) > 0.3f, "Client input drives Host Rigidbody");
            var gun = (TestProjectileWeapon)weapon;
            Check(gun.TryFire(), "Host fire");
            Check(FindObjectsByType<Combat.Projectile>(FindObjectsSortMode.None).Length > 0, "Network projectile spawned");
            yield return new WaitForSeconds(0.2f);
            Check(weapon.HoldByOwner(), "Recover");
            slots.DropSelectedWeapon();
            Check(weapon.Owner == null && weapon.Host == null && weapon.State == WeaponState.Dropped, "Drop clears ownership");
            Check(weapon.PickUp(slots), "Pickup again");
            var part = FindFirstObjectByType<TireItem>();
            var oldTire = vehicle.GetComponentInChildren<WheelSystem>().CurrentTire;
            Check(slots.AddPart(part), "Tire uses common slot");
            var newTire = part.Definition;
            Check(part.Install(vehicle.GetComponentInChildren<WheelSystem>()), "Whole vehicle tire exchange");
            Check(vehicle.GetComponentInChildren<WheelSystem>().CurrentTire == newTire && part.Definition == oldTire, "Old tire returned to slot");
            client.GetComponent<VehicleStationUser>().TryLeaveStation();
            host.GetComponent<VehicleStationUser>().TryLeaveStation();
            Check(host.GetComponent<VehicleStationUser>().TryEnterStation(seats.First(s => s.StationType == VehicleStationType.Driver)), "Host takes Driver");
            Check(client.GetComponent<VehicleStationUser>().TryEnterStation(seats.First(s => s.StationType == VehicleStationType.Crew)), "Client takes Crew");
            var clientSlots = client.GetComponent<PlayerWeaponSlots>();
            var heavy = FindObjectsByType<TestProjectileWeapon>(FindObjectsSortMode.None).First(w => w.State == WeaponState.Dropped && w.Settings.aimMode == WeaponAimMode.Heavy);
            Check(heavy.PickUp(clientSlots), "Client acquires Heavy");
            clientSlots.SelectSlot(clientSlots.IndexOf(heavy));
            Check(heavy.AttachTo(vehicleHost, vehicle.transform.position + vehicle.transform.up, vehicle.transform.rotation, WeaponState.Attached), "Client Heavy attached");
            Quaternion beforeAim = heavy.transform.localRotation;
            yield return new WaitForSeconds(3f);
            Check(heavy.IsAiming && Quaternion.Angle(beforeAim, heavy.transform.localRotation) > 1f, "Client Aim rotates Heavy on Host");
            Check(heavy.ZeroDistance > 30f, "Client zero distance reaches Host");
            Debug.Log(failed ? "PROBE SERVER FAILED" : "PROBE SERVER PASSED");
            yield return new WaitForSeconds(7f);
            Application.Quit(failed ? 1 : 0);
        }
        private IEnumerator ClientChecks(NetworkManager session)
        {
            bool sawDriver = false, sawAttached = false, sawProjectile = false, sawTireExchange = false, sawHeavyAim = false;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var playerObject = session.LocalClient.PlayerObject;
                if (playerObject != null)
                {
                    var player = playerObject.GetComponent<NetworkPlayer>();
                    sawDriver |= player.GetComponent<VehicleStationUser>().IsDriving;
                    bool crew = player.GetComponent<VehicleStationUser>().IsCrew;
                    player.Submit(new PlayerCommand { move = new Vector2(0f, crew ? 0f : 0.5f), slot = -1,
                        aim = crew, fire = crew, look = crew ? new Vector2(1f, 0f) : Vector2.zero, scroll = crew ? 1f : 0f });
                }
                sawAttached |= FindObjectsByType<Weapon>(FindObjectsSortMode.None).Any(w => w.State == WeaponState.Attached && w.Owner != null && w.Host != null);
                sawProjectile |= FindObjectsByType<Combat.Projectile>(FindObjectsSortMode.None).Length > 0;
                var wheels = FindFirstObjectByType<WheelSystem>();
                sawTireExchange |= wheels != null && wheels.CurrentTire.Mass == 30f;
                sawHeavyAim |= FindObjectsByType<TestProjectileWeapon>(FindObjectsSortMode.None).Any(w => w.Settings.aimMode == WeaponAimMode.Heavy && w.IsAiming && w.ZeroDistance > 30f);
                yield return null;
            }
            Check(sawDriver, "Client seat replicated"); Check(sawAttached, "Weapon owner/host/attached replicated");
            Check(sawProjectile, "Projectile replicated"); Check(sawTireExchange, "Tire definition replicated");
            Check(sawHeavyAim, "Heavy Aim and zero replicated");
            Debug.Log(failed ? "PROBE CLIENT FAILED" : "PROBE CLIENT PASSED");
            Application.Quit(failed ? 1 : 0);
        }
        private void Check(bool success, string label)
        {
            if (!success) { failed = true; Debug.LogError("PROBE FAIL: " + label); }
            else Debug.Log("PROBE PASS: " + label);
        }
        private void Fail(string label) { Check(false, label); Application.Quit(1); }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) failed = true;
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
