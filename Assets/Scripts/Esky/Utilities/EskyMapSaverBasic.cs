using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities{
    
    public class EskyMapSaverBasic : MonoBehaviour
    {
        public int HookedTrackerID;
        public string MapName;
        public bool loadMap = false;
        public bool saveMap = false;
        public bool loadBlob = false;

        void Start(){
            try{
                BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].mapCollectedCallback.AddListener(ReceiveFile);
            }catch(System.Exception e){
                Debug.LogError("Couldn't auto attach to the tracker:" + e.Message);
            }
        }
        // Update is called once per frame
        void Update()
        {
            if(loadMap){
                loadMap = false;
                LoadFile();
            }
            if(saveMap){
                saveMap = false;
                SaveFile();
            }
            if(loadBlob){

                loadBlob = false;
                LoadFileBlob();
            }
        }
        public void ReceiveFile(BEERLabs.ProjectEsky.Tracking.EskyMap info){
            System.IO.File.WriteAllBytes(MapName,info.GetBytes());
        }
        public void SaveFile(){
            BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].SaveEskyMapInformation();
        }
        public void LoadFile(){
            byte[] dataInfo = System.IO.File.ReadAllBytes(MapName);
            BEERLabs.ProjectEsky.Tracking.EskyMap myMap = BEERLabs.ProjectEsky.Tracking.EskyMap.GetMapFromArray(dataInfo);
            if(myMap != null){
                if(BEERLabs.ProjectEsky.Tracking.EskyAnchor.instances[HookedTrackerID] != null){
                    BEERLabs.ProjectEsky.Tracking.EskyAnchor.instances[HookedTrackerID].SetEskyMapInfo(myMap);
                }
                if(BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID] != null){
                    BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].LoadEskyMap(myMap);
                }
            }else{
                throw new System.Exception("Error loading file: " + MapName + " as EskyMap");
            }
        }
        public void LoadFileBlob(){
            byte[] dataInfo = System.IO.File.ReadAllBytes("temp.raw");
            BEERLabs.ProjectEsky.Tracking.EskyMap myMap = new BEERLabs.ProjectEsky.Tracking.EskyMap();
            myMap.mapBLOB = dataInfo;
            BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].LoadEskyMap(myMap);
        }
    }
}