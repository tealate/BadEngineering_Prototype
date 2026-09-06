using System;
using System.Collections;
using BadEngineering.Player;
using BadEngineering.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace BadEngineering.Network
{
    /// <summary>指定時のみ固定した検証視点のPNGを保存する。</summary>
    public sealed class PrototypeVisualProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-prototypeCapture");
            if (index < 0 || index + 1 >= arguments.Length) { Destroy(this); yield break; }
            yield return new WaitForSeconds(3f);
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<FirstPersonRigidbodyController>();
            player.enabled = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
            player.transform.SetPositionAndRotation(new Vector3(0f, 2f, 6.7f), Quaternion.identity);
            player.HeadPivot.localRotation = Quaternion.Euler(25f, 0f, 0f);
            yield return null;
            var preview = player.GetComponent<WeaponPlacementController>();
            preview.Refresh(player.PlayerCamera, Vector2.zero, false);
            Debug.Log("VISUAL PREVIEW: " + preview.HasPreview);
            // Batch Modeでは画面のバックバッファがないため、カメラから明示的に描画する。
            yield return null;
            var camera = player.PlayerCamera;
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            System.IO.File.WriteAllBytes(arguments[index + 1], pixels.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(pixels);
            Debug.Log("VISUAL CAPTURE PASSED");
            if (!preview.HasPreview) throw new InvalidOperationException("Placement preview was not visible.");
            var ghost = GameObject.Find("Weapon Ghost Preview");
            if (ghost.GetComponentsInChildren<Collider>().Length != 0 || ghost.GetComponentsInChildren<Rigidbody>().Length != 0)
                throw new InvalidOperationException("Preview contains physics components.");
            for (int axis = 1; axis <= 3; axis++)
            {
                preview.Refresh(camera, new Vector2(30f, 0f), true);
                if ((int)preview.Mode != axis) throw new InvalidOperationException("Placement mode cycle failed.");
            }
            var vehicle = preview.Surface.Host.transform;
            Vector3 position = preview.Position;
            Quaternion rotation = preview.Rotation;
            var command = new PlayerCommand
            {
                hasPlacement = true,
                rayOrigin = camera.transform.position,
                rayDirection = camera.transform.forward,
                vehicleId = vehicle.GetComponent<NetworkObject>().NetworkObjectId,
                placementPosition = vehicle.InverseTransformPoint(position),
                placementRotation = Quaternion.Inverse(vehicle.rotation) * rotation
            };
            var weapon = player.GetComponent<PlayerWeaponSlots>().EquippedWeapon;
            if (!player.GetComponent<BadEngineering.Interaction.PlayerInteractor>().PlaceCommand(command) ||
                weapon.State != WeaponState.Attached || Vector3.Distance(weapon.transform.position, position) > 0.001f ||
                Quaternion.Angle(weapon.transform.rotation, rotation) > 0.1f)
                throw new InvalidOperationException("Placement command did not preserve preview pose.");
            Debug.Log("PLACEMENT COMMAND PASSED");
            Application.Quit();
        }
    }
}
