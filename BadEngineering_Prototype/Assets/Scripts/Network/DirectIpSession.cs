using System;
using BadEngineering.Vehicle;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadEngineering.Network
{
    /// <summary>LAN接続設定と仮接続UI。GameplayのAuthority処理はここに置かない。</summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class DirectIpSession : MonoBehaviour
    {
        [SerializeField] private TireDefinition[] tires;
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;
        public static DirectIpSession Instance { get; private set; }
        private NetworkManager session;
        private string status = "LAN / Direct IP";
        public int TireIndex(TireDefinition tire) => Array.IndexOf(tires, tire);
        public TireDefinition TireAt(int index) => index >= 0 && index < tires.Length ? tires[index] : null;
        private void Awake()
        {
            Instance = this;
            session = GetComponent<NetworkManager>();
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            session.OnClientConnectedCallback += Connected;
            session.OnClientDisconnectCallback += Disconnected;
        }
        private void Start()
        {
            gameObject.AddComponent<PrototypeAcceptanceProbe>();
            gameObject.AddComponent<PrototypeVisualProbe>();
            string[] arguments = Environment.GetCommandLineArgs();
            int hostIndex = Array.IndexOf(arguments, "-prototypeHost");
            int clientIndex = Array.IndexOf(arguments, "-prototypeClient");
            if (clientIndex >= 0 && clientIndex + 1 < arguments.Length) address = arguments[clientIndex + 1];
            if (hostIndex >= 0) StartHost();
            else if (clientIndex >= 0) StartClient();
        }
        public bool StartHost()
        {
            GetComponent<UnityTransport>().SetConnectionData(address, port, "0.0.0.0");
            bool started = session.StartHost();
            status = started ? "Hosting on UDP " + port : "Host start failed";
            return started;
        }
        public bool StartClient()
        {
            GetComponent<UnityTransport>().SetConnectionData(address, port);
            bool started = session.StartClient();
            status = started ? "Connecting to " + address : "Client start failed";
            return started;
        }
        private void Connected(ulong id) => status = "Connected: " + id;
        private void Disconnected(ulong id)
        {
            status = "Disconnected: " + id + " " + session.DisconnectReason;
            if (id == session.LocalClientId) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
        }
        private void OnGUI()
        {
            if (session.IsListening && Cursor.lockState == CursorLockMode.Locked) return;
            GUILayout.BeginArea(new Rect(20f, 20f, 340f, 210f), GUI.skin.box);
            GUILayout.Label("BadEngineering Prototype — LAN");
            GUILayout.Label(status);
            if (!session.IsListening)
            {
                address = GUILayout.TextField(address);
                GUILayout.Label("UDP Port: " + port);
                if (GUILayout.Button("Host")) StartHost();
                if (GUILayout.Button("Connect")) StartClient();
            }
            else if (GUILayout.Button("Disconnect")) session.Shutdown();
            GUILayout.Label("Esc: menu / mouse capture");
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (session == null) return;
            session.OnClientConnectedCallback -= Connected;
            session.OnClientDisconnectCallback -= Disconnected;
        }
    }
}
