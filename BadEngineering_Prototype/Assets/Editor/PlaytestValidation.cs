using System;
using BadEngineering.Player;
using BadEngineering.Vehicle;
using BadEngineering.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BadEngineering.Editor
{
    public static class PlaytestValidation
    {
        public static void ValidateAssets()
        {
            EditorSceneManager.OpenScene(PlaytestSceneBuilder.ScenePath, OpenSceneMode.Single);
            Require(UnityEngine.Object.FindObjectsByType<Combat.SimpleEnemy>(FindObjectsSortMode.None).Length == 0, "Enemy excluded");
            Require(UnityEngine.Object.FindObjectsByType<VehicleInteractionPoint>(FindObjectsSortMode.None).Length == 2, "Driver and Crew");
            var weapons = UnityEngine.Object.FindObjectsByType<TestProjectileWeapon>(FindObjectsSortMode.None);
            Require(weapons.Length == 3, "Three weapon types");
            foreach (var weapon in weapons)
            {
                Require(weapon.Settings != null && weapon.Settings.projectilePrefab != null, "Weapon settings and projectile");
                Require(weapon.transform.localScale == Vector3.one, "Weapon root scale");
            }
            var tires = UnityEngine.Object.FindObjectsByType<TireItem>(FindObjectsSortMode.None);
            Require(tires.Length == 1 && tires[0].GetComponentInChildren<Collider>() != null && tires[0].GetComponent<Rigidbody>() != null, "Physical tire item");
            Require(Physics.GetIgnoreLayerCollision(PrototypeCollision.MountedTire, 0), "Mounted tire ignores World");
            Require(!Physics.GetIgnoreLayerCollision(PrototypeCollision.MountedTire, PrototypeCollision.Player), "Mounted tire hits Player");
            Require(!Physics.GetIgnoreLayerCollision(PrototypeCollision.TireItem, 0), "Dropped tire hits World");
            Require(Physics.GetIgnoreLayerCollision(PrototypeCollision.Weapon, PrototypeCollision.Vehicle), "Weapon ignores Vehicle");
            Require(BallisticZero.TryElevation(40f, 28f, 9.81f, out float angle), "Reachable zero");
            float range = 28f * 28f * Mathf.Sin(2f * angle * Mathf.Deg2Rad) / 9.81f;
            Require(Mathf.Abs(range - 40f) < 0.01f, "Ballistic range matches selected distance");
            Require(!BallisticZero.TryElevation(100f, 28f, 9.81f, out _), "Unreachable zero rejected");
            var tire = AssetDatabase.LoadAssetAtPath<TireDefinition>("Assets/Data/Vehicle/PrototypeTire.asset");
            Require(tire.TreadFriction == 3f, "User grip preserved");
            Require(tire.FrictionForNormal(Vector3.right, Vector3.right) == tire.SidewallFriction, "Sidewall classification");
            Require(tire.FrictionForNormal(Vector3.up, Vector3.right) == tire.TreadFriction, "Tread classification");
            Debug.Log("PLAYTEST ASSET CHECKS PASSED (17 checks plus weapon checks)");
        }
        private static void Require(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
        }
    }
}
