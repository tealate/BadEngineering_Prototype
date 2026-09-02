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

        [MenuItem("Bad Engineering/Build Prototype Test Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGround();
            CreatePlayer();
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
