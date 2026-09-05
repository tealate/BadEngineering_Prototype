using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BadEngineering.Editor
{
    [InitializeOnLoad]
    public static class PrototypePlayModeSmokeTest
    {
        private const string PendingKey = "BadEngineering.PrototypeSmokeTest.Pending";
        private const string ScenePath = "Assets/Scenes/PrototypeTest.unity";

        static PrototypePlayModeSmokeTest()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false) || state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            SessionState.SetBool(PendingKey, false);
            try
            {
                ValidateRuntimeLoop();
                Debug.Log("BadEngineering prototype Play Mode smoke test passed.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateRuntimeLoop()
        {
            PlayerWeaponSlots slots = RequireOne<PlayerWeaponSlots>();
            FirstPersonRigidbodyController player = RequireOne<FirstPersonRigidbodyController>();
            VehiclePhysicsController vehicle = RequireOne<VehiclePhysicsController>();
            VehicleStationUser stationUser = RequireOne<VehicleStationUser>();
            VehicleInteractionPoint[] stations = Object.FindObjectsByType<VehicleInteractionPoint>(FindObjectsSortMode.None);

            Weapon starter = slots.EquippedWeapon;
            Assert(starter != null, "Starter weapon was not selected.");
            Assert(starter.Owner == slots, "Starter weapon owner was not initialized.");
            Assert(starter.State == WeaponState.Held, "Starter weapon was not held.");
            Assert(starter.Host?.Body == player.GetComponent<Rigidbody>(), "Held weapon host is not Player.");

            WeaponHost vehicleHost = vehicle.GetComponent<WeaponHost>();
            float baseVehicleMass = vehicle.Body.mass;
            starter.AttachTo(vehicleHost, vehicle.transform.position + vehicle.transform.up, vehicle.transform.rotation, WeaponState.Attached);
            Assert(starter.Owner == slots, "Owner changed while attaching to Vehicle.");
            Assert(ReferenceEquals(starter.Host, vehicleHost) && starter.State == WeaponState.Attached,
                "Weapon did not attach to Vehicle.");
            Assert(vehicle.Body.mass > baseVehicleMass, "Attached weapon mass was not added to Vehicle.");

            starter.PrimaryPressed();
            Assert(Object.FindObjectsByType<BadEngineering.Combat.Projectile>(FindObjectsSortMode.None).Length > 0,
                "Weapon fire did not spawn a projectile.");

            Assert(starter.HoldByOwner(), "Attached weapon did not return to Player.");
            Assert(starter.State == WeaponState.Held && starter.Host?.Body == player.GetComponent<Rigidbody>(),
                "Recovered weapon has invalid state/host.");

            starter.Drop(player.transform.position + player.transform.forward, Vector3.zero);
            Assert(starter.State == WeaponState.Dropped && starter.Owner == null && starter.Host == null,
                "Dropped weapon retained owner or host.");
            Assert(starter.PickUp(slots) && starter.State == WeaponState.Held,
                "Dropped weapon could not be picked up again.");

            ValidateSecondOwnerOnSameVehicle(vehicleHost, vehicle.Body.mass);

            VehicleInteractionPoint driver = System.Array.Find(
                stations,
                station => station.StationType == VehicleStationType.Driver);
            Assert(driver != null && stationUser.TryEnterStation(driver), "Player could not enter Driver seat.");
            Assert(stationUser.IsDriving, "Driver state was not reported.");
            Assert(stationUser.TryLeaveStation() && !stationUser.IsUsingStation, "Player could not exit Driver seat.");

            VehicleInteractionPoint crew = System.Array.Find(
                stations,
                station => station.StationType == VehicleStationType.Crew);
            Assert(crew != null && stationUser.TryEnterStation(crew), "Player could not enter Crew seat.");
            Assert(stationUser.IsCrew && !stationUser.IsDriving, "Crew state was not reported correctly.");
            Assert(stationUser.TryLeaveStation(), "Player could not exit Crew seat.");

            starter.PrimaryPressed();
            Assert(player.CurrentPhysicalState == PlayerPhysicalState.Uncontrolled,
                "Firing a held weapon did not enter Uncontrolled state.");
        }

        private static void ValidateSecondOwnerOnSameVehicle(WeaponHost vehicleHost, float initialMass)
        {
            GameObject secondPlayer = new GameObject("Smoke Test Player B");
            secondPlayer.AddComponent<Rigidbody>();
            secondPlayer.AddComponent<WeaponHost>();
            PlayerWeaponSlots secondSlots = secondPlayer.AddComponent<PlayerWeaponSlots>();

            GameObject weaponObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObject.name = "Player B Weapon";
            Weapon secondWeapon = weaponObject.AddComponent<TestProjectileWeapon>();
            Assert(secondSlots.AddOwnedWeapon(secondWeapon), "Player B could not own a weapon.");
            Assert(secondWeapon.AttachTo(
                    vehicleHost,
                    vehicleHost.transform.position - vehicleHost.transform.right,
                    vehicleHost.transform.rotation,
                    WeaponState.Attached),
                "Player B weapon could not attach to shared Vehicle.");
            Assert(secondWeapon.Owner == secondSlots, "Player B ownership changed on shared Vehicle.");
            Assert(vehicleHost.Body.mass > initialMass, "Second owner's weapon did not contribute to shared Vehicle mass.");
            Assert(vehicleHost.Body.centerOfMass.x < 0f,
                "Off-center weapon did not shift shared Vehicle center of mass.");

            Object.DestroyImmediate(secondPlayer);
            Object.DestroyImmediate(weaponObject);
            vehicleHost.RefreshMassProperties();
        }

        private static T RequireOne<T>() where T : Object
        {
            T instance = Object.FindFirstObjectByType<T>();
            if (instance == null)
            {
                throw new System.InvalidOperationException($"Required runtime component missing: {typeof(T).Name}");
            }
            return instance;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }
    }
}
