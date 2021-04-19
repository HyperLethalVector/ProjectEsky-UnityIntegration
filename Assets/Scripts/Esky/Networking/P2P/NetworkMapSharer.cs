using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProjectEsky.Networking.WebRTC.Discovery;
using ProjectEsky.Tracking;
using UnityEngine;
using UnityEngine.Networking;
using UniWebServer;
namespace ProjectEsky.Networking{
    [RequireComponent(typeof(EmbeddedWebServerComponent))]
    public class NetworkMapSharer : MonoBehaviour, IWebResource
    {
        public static NetworkMapSharer instance;
        bool receivedMap = false;
        byte[] mapBytes;
        public void Awake(){
            instance = this;
        }
        public ProjectEsky.Tracking.EskyTrackerIntel myAttachedTracker;
        public void ObtainMap(){
            StartCoroutine(GetMap());
        }
        public void SendMap(EskyMap m){
            Debug.Log("Sending map of size: " + m.mapBLOB.Length);
            WebRTC.WebRTCPacket p = new WebRTC.WebRTCPacket();
            p.packetType = WebRTC.WebRTCPacketType.MapBLOBShare;
            WebRTC.WebRTCDataStreamManager.instance.SendPacketReliable(p);            
        }
        public void TriggerObtainMap(){
            myAttachedTracker.SaveEskyMapInformation();
        }
        EmbeddedWebServerComponent server;
        
        void Start()
        {
            server = GetComponent<EmbeddedWebServerComponent>();
            server.AddResource("getMap", this);
            server.SubscribeResourceAndStart(HandleRequest);
        }

        void FixedUpdate(){
            if(receivedMap){
                receivedMap = false;
                EskyMap m = new EskyMap();
                m.mapBLOB = mapBytes;
                myAttachedTracker.LoadEskyMap(m);
            }
        }
        IEnumerator GetMap() {
                string mapLoc = "http://"+WebRTCAutoDiscoveryHandler.instance.HostingIP+":"+server.port+"/temp.raw";
                Debug.Log("Obtaining map from: " + mapLoc);
                UnityWebRequest www = UnityWebRequest.Get(mapLoc);
                yield return www.SendWebRequest();        
                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.Log(www.error);
                    StartCoroutine(GetMap());
                }
                else {
                    // Show results as text
                    Debug.Log("Received map! Now to load to the tracker");        
                    // Or retrieve results as binary data
                    mapBytes = www.downloadHandler.data;
                    receivedMap = true;
                }
            }

        public void HandleRequest(Request request, Response response)
        {
            // check if file exist at folder (need to assume a base local root)
            // not found
            if (!File.Exists("temp.raw")) {
                response.statusCode = 404;
                response.message = "Not Found";
                return;
            }

            // serve the file
            response.statusCode = 200;
            response.message = "OK";
            response.headers.Add("Content-Type", MimeTypeMap.GetMimeType(".raw"));

            // read file and set bytes
            using (FileStream fs = File.OpenRead("temp.raw"))
            {
                int length = (int)fs.Length;
                byte[] buffer;
                // add content length
                response.headers.Add("Content-Length", length.ToString());

                // use binary for mostly all except text
                using (BinaryReader br = new BinaryReader(fs))
                {
                    buffer = br.ReadBytes(length);
                }
                response.SetBytes(buffer);
            }
        }
    }
}
