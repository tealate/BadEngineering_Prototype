using BadEngineering.Player;
using BadEngineering.Interaction;
using BadEngineering.UI;
using BadEngineering.Weapons;
using BadEngineering.Vehicle;
using BadEngineering.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BadEngineering.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeTest.unity";

        [MenuItem("Bad Engineering/Build Prototype Test Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGround();
            CreatePlayer();
            CreateVehicle();
            CreateDroppedWeapons();
            CreateTargets();
            CreateHud();
            CreateLighting();
            ValidateScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"Created prototype test scene: {ScenePath}");
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetPositionAndRotation(new Vector3(0f, -0.5f, 0f), Quaternion.identity);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.05f, 0f);

            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.45f;
            capsule.center = Vector3.zero;

            var body = player.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            GameObject headPivot = new GameObject("HeadPivot");
            headPivot.transform.SetParent(player.transform, false);
            headPivot.transform.localPosition = new Vector3(0f, 0.72f, 0f);

            GameObject cameraObject = new GameObject("FirstPersonCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(headPivot.transform, false);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            GameObject weaponAttachRoot = new GameObject("WeaponAttachRoot");
            weaponAttachRoot.transform.SetParent(cameraObject.transform, false);
            weaponAttachRoot.transform.localPosition = new Vector3(0.24f, -0.2f, 0.55f);

            CreateTestWeapon(weaponAttachRoot.transform, "Starter Gun", 8f, 65f);
            player.AddComponent<PlayerWeaponSlots>();
            player.AddComponent<FirstPersonRigidbodyController>();
            player.AddComponent<PlayerInteractor>();

            WeaponHost host = player.GetComponent<WeaponHost>();
            SetObjectReference(host, "weaponAttachRoot", weaponAttachRoot.transform);
        }

        private static TestProjectileWeapon CreateTestWeapon(
            Transform parent,
            string weaponName,
            float mass,
            float recoil)
        {
            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = weaponName;
            weapon.transform.SetParent(parent, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = new Vector3(0.16f, 0.16f, 0.55f);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(weapon.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 0.56f);

            var testWeapon = weapon.AddComponent<TestProjectileWeapon>();
            var serializedWeapon = new SerializedObject(testWeapon);
            serializedWeapon.FindProperty("displayName").stringValue = weaponName;
            serializedWeapon.FindProperty("weaponMass").floatValue = mass;
            serializedWeapon.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            serializedWeapon.FindProperty("recoilImpulse").floatValue = recoil;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            return testWeapon;
        }

        private static void CreateDroppedWeapons()
        {
            CreateDroppedWeapon(new Vector3(-3f, 0.8f, 5f), "Heavy Cannon", 45f, 650f);
            CreateDroppedWeapon(new Vector3(3f, 0.8f, 5f), "Kick Gun", 15f, 220f);
        }

        private static void CreateDroppedWeapon(Vector3 position, string name, float mass, float recoil)
        {
            TestProjectileWeapon weapon = CreateTestWeapon(null, name, mass, recoil);
            weapon.transform.position = position;
            Rigidbody body = weapon.gameObject.AddComponent<Rigidbody>();
            body.mass = mass;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private static void CreateVehicle()
        {
            GameObject vehicle = new GameObject("Prototype Vehicle");
            vehicle.name = "Prototype Vehicle";
            vehicle.transform.SetPositionAndRotation(new Vector3(0f, 1.1f, 10f), Quaternion.identity);

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis / Attachment Surface";
            chassis.transform.SetParent(vehicle.transform, false);
            chassis.transform.localScale = new Vector3(3.2f, 0.8f, 5f);

            Rigidbody body = vehicle.AddComponent<Rigidbody>();
            body.mass = 450f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            WeaponHost host = vehicle.AddComponent<WeaponHost>();
            SetBoolean(host, "includeAttachedWeaponMass", true);
            vehicle.AddComponent<VehiclePhysicsController>();
            chassis.AddComponent<VehicleWeaponSurface>();

            CreateStation(vehicle.transform, "Driver Seat", VehicleStationType.Driver, new Vector3(-0.7f, 0.75f, 1f));
            CreateStation(vehicle.transform, "Crew Seat", VehicleStationType.Crew, new Vector3(0.7f, 0.75f, 1f));

            CreateWheelVisual(vehicle.transform, new Vector3(-1.7f, -0.4f, 1.6f));
            CreateWheelVisual(vehicle.transform, new Vector3(1.7f, -0.4f, 1.6f));
            CreateWheelVisual(vehicle.transform, new Vector3(-1.7f, -0.4f, -1.6f));
            CreateWheelVisual(vehicle.transform, new Vector3(1.7f, -0.4f, -1.6f));
        }

        private static void CreateStation(Transform vehicle, string name, VehicleStationType type, Vector3 localPosition)
        {
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = name;
            station.transform.SetParent(vehicle, false);
            station.transform.localPosition = localPosition;
            station.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);

            GameObject operating = new GameObject("Operating Position");
            operating.transform.SetParent(station.transform, false);
            operating.transform.localPosition = Vector3.up * 0.2f;

            GameObject exit = new GameObject("Exit Position");
            exit.transform.SetParent(vehicle, false);
            exit.transform.localPosition = new Vector3(type == VehicleStationType.Driver ? -0.85f : 0.85f, 0.4f, 0f);

            VehicleInteractionPoint point = station.AddComponent<VehicleInteractionPoint>();
            SerializedObject serialized = new SerializedObject(point);
            serialized.FindProperty("stationType").enumValueIndex = (int)type;
            serialized.FindProperty("operatingPosition").objectReferenceValue = operating.transform;
            serialized.FindProperty("exitPosition").objectReferenceValue = exit.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWheelVisual(Transform vehicle, Vector3 localPosition)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel Visual";
            wheel.transform.SetParent(vehicle, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
            Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }

        private static void CreateTargets()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = $"Enemy {i + 1}";
                enemy.transform.position = new Vector3(-6f + i * 4f, 1f, 22f + (i % 2) * 3f);
                Rigidbody body = enemy.AddComponent<Rigidbody>();
                body.mass = 60f;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                enemy.AddComponent<Health>();
                enemy.AddComponent<SimpleEnemy>();
            }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateScene()
        {
            RequireExactlyOne<PlayerWeaponSlots>("Player weapon slots");
            RequireExactlyOne<FirstPersonRigidbodyController>("Player controller");
            RequireExactlyOne<VehiclePhysicsController>("Vehicle physics controller");

            if (Object.FindObjectsByType<VehicleInteractionPoint>(FindObjectsSortMode.None).Length < 2)
            {
                throw new System.InvalidOperationException("Prototype scene requires Driver and Crew stations.");
            }
            if (Object.FindObjectsByType<TestProjectileWeapon>(FindObjectsSortMode.None).Length < 3)
            {
                throw new System.InvalidOperationException("Prototype scene requires starter and dropped test weapons.");
            }
            if (Object.FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None).Length < 1)
            {
                throw new System.InvalidOperationException("Prototype scene requires at least one PvE target.");
            }
        }

        private static void RequireExactlyOne<T>(string label) where T : Object
        {
            int count = Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;
            if (count != 1)
            {
                throw new System.InvalidOperationException($"{label}: expected exactly one, found {count}.");
            }
        }

        private static void EnsureSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == ScenePath)
                {
                    scene.enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[^1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        private static void CreateHud()
        {
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObject.AddComponent<WeaponSlotHud>();
        }
    }
}
