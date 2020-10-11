using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
namespace ProjectEsky.Networking{
    [System.Serializable]
    public class NetworkConnectionCallback: UnityEvent<NetworkConnection>{

    }
    public class EskyNetworkManager : NetworkManager
    {
        public bool AutoStartServer = false;
        bool isConnected = false;
        public static EskyNetworkManager instance;
        public UnityEvent OnStartedServer;
        public UnityEvent OnStoppedServer;
        public NetworkConnectionCallback OnClientConnected;
        public NetworkConnectionCallback OnClientDisconnected;
        // Start is called before the first frame update
        public override void Awake()
        {
            base.Awake();
            if(instance != null)
            {
                DestroyImmediate(this.gameObject);
                return;
            }
            else
            {
                instance = this;
            }
        }
        public override void Start()
        {
            base.Start();

        }
        bool connectClient = false;
        private void Update() {
            if(connectClient){
                connectClient = false;
                StartClient();
            }
            if(AutoStartServer){
                AutoStartServer = false;
                StartServer();
            }
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            //need to run a web api instance to collate all the known gameobjects in the scene
            Debug.Log("Started Server");      
            if(OnStartedServer != null){
                OnStartedServer.Invoke();
            }
        }
        public override void OnStopServer()
        {
            base.OnStopServer();
            Debug.Log("Stopped Server");
            if(OnStoppedServer != null){
                OnStoppedServer.Invoke();
            }            
        }
        public override void OnStartHost()
        {
            base.OnStartHost();

        }
        public override void OnServerReady(NetworkConnection conn)
        {

            base.OnServerReady(conn);

        }
        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);

        }
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            if(OnClientConnected != null){
                OnClientConnected.Invoke(conn);
            }
            Debug.Log("Connected to server!");
            isConnected = true;
        }
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            Debug.Log("OnServerDisconnect");
        }
        public override void OnStopHost()
        {
            base.OnStopHost();
            Debug.Log("Stopped Host");
        }
        int connectionAttempts = 0;
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            isConnected = false;
            Debug.Log("OnClientDisconnect");            
            Debug.Log("Disconnected from server!");
            connectionAttempts++;
            StartCoroutine(ConnectAfterSeconds(2));
        }

        public IEnumerator LoadAfterSeconds(int seconds)
        {
            yield return new WaitForSeconds(seconds);

        }
        public void StartServerUp()
        {
            StartServer();
        }
        public void ConnectClient()
        {   
            StartCoroutine(ConnectAfterSeconds(2));
        }
        public IEnumerator ConnectAfterSeconds(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            Debug.Log("Connecting attempt now");
            connectClient = true;
        }
        public IEnumerator BeginTostartClient()
        {
            Debug.Log("Connecting attempt now on main thread");
            connectClient = true;
            yield return null;
        }

    }
}