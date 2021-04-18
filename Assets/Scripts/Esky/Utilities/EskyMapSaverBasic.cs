using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
namespace ProjectEsky.Utilities{
    
    public class EskyMapSaverBasic : MonoBehaviour
    {
        public ProjectEsky.Tracking.EskyTrackerIntel myAttachedTracker;
        public int HookedTrackerID;
        public string MapName;
        public bool loadMap = false;
        public bool saveMap = false;
        public bool loadBlob = false;
        
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
        public void ReceiveFile(ProjectEsky.Tracking.EskyMap info){
            System.IO.File.WriteAllBytes(MapName,info.GetBytes());
        }
        public void SaveFile(){
            ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].SaveEskyMapInformation();
        }
        public void LoadFile(){
            byte[] dataInfo = System.IO.File.ReadAllBytes(MapName);
            ProjectEsky.Tracking.EskyMap myMap = ProjectEsky.Tracking.EskyMap.GetMapFromArray(dataInfo);
            if(myMap != null){
                if(ProjectEsky.Tracking.EskyAnchor.instances[HookedTrackerID] != null){
                    ProjectEsky.Tracking.EskyAnchor.instances[HookedTrackerID].SetEskyMapInfo(myMap);
                }
                if(ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID] != null){
                    ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID].LoadEskyMap(myMap);
                }
            }else{
                throw new System.Exception("Error loading file: " + MapName + " as EskyMap");
            }
        }
        public void LoadFileBlob(){
            byte[] dataInfo = System.IO.File.ReadAllBytes("temp.raw");
            ProjectEsky.Tracking.EskyMap myMap = new ProjectEsky.Tracking.EskyMap();
            myMap.mapBLOB = dataInfo;
            myAttachedTracker.LoadEskyMap(myMap);
        }
    }
}