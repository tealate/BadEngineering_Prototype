using System.IO;
using BadEngineering.Combat;
using BadEngineering.Network;
using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BadEngineering.Editor
{
    /// <summary>既存テストSceneを保存変更せず、次回LAN用SceneとPrefabを生成する。</summary>
    public static class PlaytestSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/PrototypeLan.unity";
        private const string Root = "Assets/PrototypePlaytest";
        [MenuItem("Bad Engineering/Prepare LAN Playtest")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Settings");
            AssetDatabase.Refresh();
            ConfigureLayers();
            Material preview = CreatePreviewMaterial();
            GameObject projectile = CreateProjectile();
            GameObject[] weapons = new GameObject[3];
            for (int i = 0; i < 3; i++) weapons[i] = CreateWeapon((WeaponAimMode)i, projectile);
            TireDefinition standard = AssetDatabase.LoadAssetAtPath<TireDefinition>("Assets/Data/Vehicle/PrototypeTire.asset");
            TireDefinition heavy = AssetDatabase.LoadAssetAtPath<TireDefinition>(Root + "/Settings/HeavyTire.asset");
            if (heavy == null)
            {
                heavy = Object.Instantiate(standard);
                heavy.name = "Heavy Tire";
                AssetDatabase.CreateAsset(heavy, Root + "/Settings/HeavyTire.asset");
                SetFloat(heavy, "mass", 30f); SetFloat(heavy, "grip", 4f);
            }
            GameObject tireItem = CreateTireItem(heavy);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrototypeTest.unity", OpenSceneMode.Single);
            // 元の床・ライト・車両調整を含むSceneから別名で保存する。
            foreach (SimpleEnemy enemy in Object.FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None)) Object.DestroyImmediate(enemy.gameObject);
            var player = Object.FindFirstObjectByType<FirstPersonRigidbodyController>();
            foreach (Weapon weapon in Object.FindObjectsByType<Weapon>(FindObjectsSortMode.None)) Object.DestroyImmediate(weapon.gameObject);
            player.gameObject.AddComponent<PlayerViewController>();
            SetReference(player.gameObject.AddComponent<WeaponPlacementController>(), "previewMaterial", preview);
            AddNetworkActor(player.gameObject);
            SetReference(player.gameObject.AddComponent<NetworkPlayer>(), "starterWeapon", weapons[0]);
            PrototypeCollision.SetLayer(player.gameObject, PrototypeCollision.Player);
            GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "Player Visual";
            Object.DestroyImmediate(avatar.GetComponent<Collider>());
            avatar.transform.SetParent(player.transform, false);
            GameObject playerPrefab = PrefabUtility.SaveAsPrefabAsset(player.gameObject, Root + "/Prefabs/Player.prefab");
            Object.DestroyImmediate(player.gameObject);
            var vehicle = Object.FindFirstObjectByType<VehiclePhysicsController>();
            PrototypeCollision.SetLayer(vehicle.gameObject, PrototypeCollision.Vehicle);
            AddNetworkActor(vehicle.gameObject);
            // Scene内の車体をPrefab化しても、元Sceneには変更を保存しない。
            for (int i = 0; i < weapons.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(weapons[i]);
                instance.transform.position = new Vector3(-3f + i * 3f, 0.8f, 4f);
            }
            GameObject tire = (GameObject)PrefabUtility.InstantiatePrefab(tireItem);
            tire.transform.position = new Vector3(-3f, 1f, 7f);
            GameObject sessionObject = new GameObject("LAN Session");
            NetworkManager session = sessionObject.AddComponent<NetworkManager>();
            UnityTransport transport = sessionObject.AddComponent<UnityTransport>();
            session.NetworkConfig.NetworkTransport = transport;
            session.NetworkConfig.PlayerPrefab = playerPrefab;
            session.NetworkConfig.TickRate = 20;
            session.NetworkConfig.EnableSceneManagement = true;
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            list.Add(new NetworkPrefab { Prefab = playerPrefab });
            list.Add(new NetworkPrefab { Prefab = projectile });
            list.Add(new NetworkPrefab { Prefab = tireItem });
            foreach (var weapon in weapons) list.Add(new NetworkPrefab { Prefab = weapon });
            string listPath = Root + "/Settings/NetworkPrefabs.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
            if (existing == null) { AssetDatabase.CreateAsset(list, listPath); existing = list; }
            else { EditorUtility.CopySerialized(list, existing); Object.DestroyImmediate(list); }
            session.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            session.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(existing);
            var connection = sessionObject.AddComponent<DirectIpSession>();
            var serialized = new SerializedObject(connection);
            var tires = serialized.FindProperty("tires"); tires.arraySize = 2;
            tires.GetArrayElementAtIndex(0).objectReferenceValue = standard;
            tires.GetArrayElementAtIndex(1).objectReferenceValue = heavy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var cameraObject = new GameObject("Lobby Camera", typeof(Camera));
            cameraObject.transform.SetPositionAndRotation(new Vector3(12f, 10f, -10f), Quaternion.Euler(25f, -25f, 0f));
            cameraObject.GetComponent<Camera>().depth = -10;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("LAN playtest assets prepared; original PrototypeTest preserved.");
        }

        private static Material CreatePreviewMaterial()
        {
            string path = Root + "/Settings/Preview.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetColor("_BaseColor", new Color(0.2f, 0.9f, 1f, 0.35f));
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject CreateProjectile()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Projectile"; root.transform.localScale = Vector3.one * 0.12f;
            root.AddComponent<Rigidbody>().mass = 0.1f;
            root.AddComponent<Projectile>(); AddNetworkActor(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Projectile.prefab");
            Object.DestroyImmediate(root); return prefab;
        }
        private static GameObject CreateWeapon(WeaponAimMode mode, GameObject projectile)
        {
            string settingsPath = Root + "/Settings/" + mode + ".asset";
            WeaponSettings settings = AssetDatabase.LoadAssetAtPath<WeaponSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WeaponSettings>();
                settings.aimMode = mode;
                settings.placementBasis = mode == WeaponAimMode.Fixed ? PlacementBasis.VehicleForward : PlacementBasis.SurfaceNormal;
                settings.projectilePrefab = projectile;
                settings.automatic = mode == WeaponAimMode.Direct;
                settings.cooldown = mode == WeaponAimMode.Heavy ? 1.2f : mode == WeaponAimMode.Direct ? 0.15f : 0.35f;
                settings.recoilImpulse = mode == WeaponAimMode.Heavy ? 650f : 65f;
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            GameObject root = new GameObject(mode + " Cannon");
            var gun = root.AddComponent<TestProjectileWeapon>();
            root.AddComponent<Rigidbody>().mass = mode == WeaponAimMode.Heavy ? 45f : 8f;
            var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0f, 0.12f, 0.4f); collider.size = new Vector3(0.3f, 0.25f, 0.8f);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual"; Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false); visual.transform.localPosition = collider.center; visual.transform.localScale = collider.size;
            GameObject muzzle = new GameObject("Muzzle"); muzzle.transform.SetParent(root.transform, false); muzzle.transform.localPosition = new Vector3(0f, 0.12f, 0.85f);
            SetReference(gun, "muzzle", muzzle.transform); SetReference(gun, "settings", settings);
            SetFloat(gun, "weaponMass", mode == WeaponAimMode.Heavy ? 45f : 8f);
            var serialized = new SerializedObject(gun); serialized.FindProperty("displayName").stringValue = mode + " Cannon"; serialized.ApplyModifiedPropertiesWithoutUndo();
            AddNetworkActor(root); PrototypeCollision.SetLayer(root, PrototypeCollision.Weapon);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + mode + "Cannon.prefab");
            Object.DestroyImmediate(root); return prefab;
        }
        private static GameObject CreateTireItem(TireDefinition tire)
        {
            GameObject root = new GameObject("Tire Item");
            root.AddComponent<Rigidbody>().mass = tire.Mass;
            SetReference(root.AddComponent<TireItem>(), "definition", tire);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(tire.VisualPrefab, root.transform); visual.name = "Visual";
            // 円筒の低ポリゴン凸形状を使い、World上で角箱のように停止せず転がれるようにする。
            var collider = visual.AddComponent<MeshCollider>();
            collider.sharedMesh = visual.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = true;
            AddNetworkActor(root); PrototypeCollision.SetLayer(root, PrototypeCollision.TireItem);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/TireItem.prefab");
            Object.DestroyImmediate(root); return prefab;
        }
        private static void AddNetworkActor(GameObject root)
        {
            var obj = root.AddComponent<NetworkObject>();
            obj.AutoObjectParentSync = false;
            root.AddComponent<NetworkActor>();
        }
        private static void ConfigureLayers()
        {
            var tagSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            string[] names = { "Player", "Vehicle", "Weapon", "MountedTire", "TireItem" };
            for (int i = 0; i < names.Length; i++) tagSettings.FindProperty("layers").GetArrayElementAtIndex(6 + i).stringValue = names[i];
            tagSettings.ApplyModifiedPropertiesWithoutUndo();
            Physics.IgnoreLayerCollision(8, 7); Physics.IgnoreLayerCollision(8, 8);
            Physics.IgnoreLayerCollision(8, 9); Physics.IgnoreLayerCollision(8, 10);
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(9, i, i != 6);
        }
        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/LanPlaytest");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/LanPlaytest/BadEngineering.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed: " + report.summary.result);
        }
        public static void PrepareValidateBuild()
        {
            Prepare();
            PlaytestValidation.ValidateAssets();
            BuildWindows();
        }
    }
}
