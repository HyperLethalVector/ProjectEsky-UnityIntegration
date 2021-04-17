using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace ProjectEsky.Networking{
    [System.Serializable]
    public class NetworkConnectionCallback{

    }
    public class EskyNetworkManager : MonoBehaviour
    {
        public bool AutoStartServer = false;
        public static EskyNetworkManager instance;
        public UnityEvent OnStartedServer;
        public UnityEvent OnStoppedServer;
        public NetworkConnectionCallback OnClientConnected;
        public NetworkConnectionCallback OnClientDisconnected;
        // Start is called before the first frame update
        public void Awake()
        {
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
        public void Start()
        {

        }
        bool connectClient = false;
        private void Update() {
        }
        public void OnStartServer()
        {
            //need to run a web api instance to collate all the known gameobjects in the scene
            if(OnStartedServer != null){
                OnStartedServer.Invoke();
            }
        }
        public void OnStopServer()
        {
            Debug.Log("Stopped Server");
            if(OnStoppedServer != null){
                OnStoppedServer.Invoke();
            }            
        }
        public void OnStartHost()
        {

        }
        public void OnStopHost()
        {
        }
        int connectionAttempts = 0;
        public IEnumerator LoadAfterSeconds(int seconds)
        {
            yield return new WaitForSeconds(seconds);

        }
        public void StartServerUp()
        {
            //StartServer();
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