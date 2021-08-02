using System.Collections;
using System.Collections.Generic;
using System.IO;
using BEERLabs.ProjectEsky.Networking.WebRTC.Discovery;
using BEERLabs.ProjectEsky.Tracking;
using UnityEngine;
using UnityEngine.Networking;
using BEERLabs.ProjectEsky.Networking;
using BEERLabs.ProjectEsky.Networking.WebAPI;
using BEERLabs.ProjectEsky.Networking.WebRTC;

namespace BEERLabs.ProjectEsky.Networking{
    public class NetworkMapSharer : MonoBehaviour
    {
        public static NetworkMapSharer instance;
        public int HookedTrackerID;
        BEERLabs.ProjectEsky.Tracking.EskyTracker myAttachedTracker;
        bool receivedMap = false;
        byte[] mapBytes;
        public void Awake(){
            instance = this;
        }

        void Start(){
            try{
                myAttachedTracker = BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID];
                myAttachedTracker.mapCollectedCallback.AddListener(SendMap);
            }catch(System.Exception e){
                Debug.LogError("Couldn't auto attach to the tracker:" + e.Message);
            }
            SubscribeEvent();            
        }
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
            if(WebRTCAutoDiscoveryHandler.instance.isHosting){
                myAttachedTracker.SaveEskyMapInformation();
            }
        }
//        EmbeddedWebServerComponent server;
        
        void FixedUpdate(){
            if(receivedMap){ 
                receivedMap = false;
                EskyMap m = new EskyMap();
                m.mapBLOB = mapBytes;
                myAttachedTracker.LoadEskyMap(m);
            }
            if(myAttachedTracker == null){
                try{
                    myAttachedTracker = BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID];
                    myAttachedTracker.mapCollectedCallback.AddListener(SendMap);
                }catch(System.Exception e){
                    Debug.LogError("Couldn't auto attach to the tracker:" + e.Message);
                }
            }
        }
        IEnumerator GetMap() {
                string mapLoc = "http://"+WebRTCAutoDiscoveryHandler.instance.HostingIP+":"+WebAPIInterface.instance.port+"/";

                Debug.Log("Obtaining map from: " + mapLoc);
                WWWForm form = new WWWForm();
                form.AddField("EventID","GetMap");                 
                UnityWebRequest www = UnityWebRequest.Post(mapLoc,form);
                
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
                yield return null;
            }
        public void SubscribeEvent(){
            WebAPIInterface.instance.SubscribeEvent(HandleRequest);
        }
        public bool HandleRequest(Request request,  Response response){
            Debug.Log("Handling Request");
            try{
                string s = request.formData["EventID"].Value.Trim();
                switch(s){
                    case "GetMap":
                        if (!File.Exists("temp.raw")) {
                            response.statusCode = 404;
                            response.message = "Not Found";
                            return true;
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
                        return true;
                }
                Debug.Log("Handling External Request");
            }catch(System.Exception e){
                Debug.LogError(e);
            }
            return false;
        }
        private void OnDestroy() {
            WebAPIInterface.instance.UnSubscribeEvent(HandleRequest);
        }
    }
}
