using BadEngineering.Player;
using BadEngineering.Interaction;
using BadEngineering.UI;
using BadEngineering.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BadEngineering.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeTest.unity";
        private const string TirePrefabPath = "Assets/Prefabs/Vehicle/PrototypeTire.prefab";
        private const string TireDefinitionPath = "Assets/Data/Vehicle/PrototypeTire.asset";

        [MenuItem("Bad Engineering/Build Prototype Test Scene")]
        public static void BuildScene()
        {
            TireDefinition tire = EnsurePrototypeTireAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGround();
            CreatePlayer();
<<<<<<< Updated upstream
=======
            CreateVehicle(tire);
            CreateDroppedWeapons();
            CreateTargets();
>>>>>>> Stashed changes
            CreateHud();
            CreateLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
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

            GameObject cameraObject = new GameObject("FirstPersonCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            CreateTestWeapon(player.transform);
            player.AddComponent<PlayerWeaponSlots>();
            player.AddComponent<FirstPersonRigidbodyController>();
            player.AddComponent<PlayerInteractor>();
        }

        private static void CreateTestWeapon(Transform playerTransform)
        {
            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "TestProjectileWeapon";
            weapon.transform.SetParent(playerTransform, false);
            weapon.transform.localPosition = new Vector3(0f, 1.1f, 0.35f);
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = new Vector3(0.16f, 0.16f, 0.55f);
            Object.DestroyImmediate(weapon.GetComponent<Collider>());

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(weapon.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 0.56f);

            var testWeapon = weapon.AddComponent<TestProjectileWeapon>();
            var serializedWeapon = new SerializedObject(testWeapon);
            serializedWeapon.FindProperty("displayName").stringValue = "Test Gun";
            serializedWeapon.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
<<<<<<< Updated upstream
=======
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

        private static void CreateVehicle(TireDefinition tire)
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
            VehiclePhysicsController controller = vehicle.AddComponent<VehiclePhysicsController>();
            WheelSystem wheelSystem = vehicle.AddComponent<WheelSystem>();
            SetObjectReference(controller, "movementSystem", wheelSystem);
            SetObjectReference(wheelSystem, "currentTire", tire);
            chassis.AddComponent<VehicleWeaponSurface>();

            CreateStation(vehicle.transform, "Driver Seat", VehicleStationType.Driver, new Vector3(-0.7f, 0.75f, 1f));
            CreateStation(vehicle.transform, "Crew Seat", VehicleStationType.Crew, new Vector3(0.7f, 0.75f, 1f));

            CreateWheelPoint(vehicle.transform, "WheelPoint_FL", new Vector3(-1.7f, 0f, 1.6f), true, tire);
            CreateWheelPoint(vehicle.transform, "WheelPoint_FR", new Vector3(1.7f, 0f, 1.6f), true, tire);
            CreateWheelPoint(vehicle.transform, "WheelPoint_RL", new Vector3(-1.7f, 0f, -1.6f), false, tire);
            CreateWheelPoint(vehicle.transform, "WheelPoint_RR", new Vector3(1.7f, 0f, -1.6f), false, tire);
        }

        private static void CreateStation(Transform vehicle, string name, VehicleStationType type, Vector3 localPosition)
        {
            GameObject station = new GameObject(name);
            station.name = name;
            station.transform.SetParent(vehicle, false);
            station.transform.localPosition = localPosition;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Interaction Marker";
            visual.transform.SetParent(station.transform, false);
            visual.transform.localScale = new Vector3(0.36f, 1.1f, 0.36f);

            GameObject operating = new GameObject("Operating Position");
            operating.transform.SetParent(station.transform, false);
            operating.transform.localPosition = Vector3.up * 0.45f;

            GameObject exit = new GameObject("Exit Position");
            exit.transform.SetParent(vehicle, false);
            exit.transform.localPosition = new Vector3(type == VehicleStationType.Driver ? -2.2f : 2.2f, 1.1f, 0f);

            VehicleInteractionPoint point = station.AddComponent<VehicleInteractionPoint>();
            SerializedObject serialized = new SerializedObject(point);
            serialized.FindProperty("stationType").enumValueIndex = (int)type;
            serialized.FindProperty("operatingPosition").objectReferenceValue = operating.transform;
            serialized.FindProperty("exitPosition").objectReferenceValue = exit.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWheelPoint(Transform vehicle, string name, Vector3 localPosition, bool canSteer, TireDefinition tire)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(vehicle, false);
            anchor.transform.localPosition = localPosition;
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(tire.VisualPrefab, anchor.transform);
            visual.name = "Visual";
            WheelPoint point = anchor.AddComponent<WheelPoint>();
            SerializedObject serialized = new SerializedObject(point);
            serialized.FindProperty("canSteer").boolValue = canSteer;
            serialized.FindProperty("canDrive").boolValue = true;
            serialized.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Bad Engineering/Create Prototype Tire Assets")]
        public static void CreatePrototypeTireAssets() => EnsurePrototypeTireAssets();

        private static TireDefinition EnsurePrototypeTireAssets()
        {
            EnsureFolder("Assets/Prefabs", "Vehicle");
            EnsureFolder("Assets/Data", "Vehicle");
            GameObject tirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TirePrefabPath);
            if (tirePrefab == null)
            {
                GameObject root = new GameObject("Prototype Tire");
                GameObject tread = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tread.name = "Tread";
                tread.transform.SetParent(root.transform, false);
                tread.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tread.transform.localScale = new Vector3(.48f, .18f, .48f);
                Object.DestroyImmediate(tread.GetComponent<Collider>());
                tirePrefab = PrefabUtility.SaveAsPrefabAsset(root, TirePrefabPath);
                Object.DestroyImmediate(root);
            }
            TireDefinition definition = AssetDatabase.LoadAssetAtPath<TireDefinition>(TireDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TireDefinition>();
                AssetDatabase.CreateAsset(definition, TireDefinitionPath);
            }
            SetObjectReference(definition, "visualPrefab", tirePrefab);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = System.IO.Path.GetDirectoryName(parent).Replace('\\', '/');
                string parentName = System.IO.Path.GetFileName(parent);
                if (!AssetDatabase.IsValidFolder(grandParent)) EnsureFolder("Assets", parentName);
                else AssetDatabase.CreateFolder(grandParent, parentName);
            }
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
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
>>>>>>> Stashed changes
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
