using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
namespace ProjectEsky.Utilities{
    
    public class EskyMapSaverBasic : MonoBehaviour
    {
        public string MapName;
        public bool loadMap = false;
        public bool saveMap = false;
        
        // Start is called before the first frame update
        void Start()
        {
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
        }
        public void ReceiveFile(ProjectEsky.Tracking.EskyMap info){
            System.IO.File.WriteAllBytes(MapName,info.GetBytes());
        }
        public void SaveFile(){
            ProjectEsky.Tracking.EskyTracker.instance.SaveEskyMapInformation();
        }
        public void LoadFile(){
            byte[] dataInfo = System.IO.File.ReadAllBytes(MapName);
            ProjectEsky.Tracking.EskyMap myMap = ProjectEsky.Tracking.EskyMap.GetMapFromArray(dataInfo);
            if(myMap != null){
                if(ProjectEsky.Tracking.EskyAnchor.instance != null){
                    ProjectEsky.Tracking.EskyAnchor.instance.SetEskyMapInfo(myMap);
                }
                if(ProjectEsky.Tracking.EskyTracker.instance != null){
                    ProjectEsky.Tracking.EskyTracker.instance.LoadEskyMap(myMap);
                }
            }else{
                throw new System.Exception("Error loading file: " + MapName + " as EskyMap");
            }

        }
        public void OnDestroy(){
        }
    }
}