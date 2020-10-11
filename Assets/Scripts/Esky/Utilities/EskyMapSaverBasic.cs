using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyMapSaverBasic : MonoBehaviour
    {
        public string MapName;
        public bool loadMap = false;
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
        }
        public void ReceiveFile(byte[] dataMap,byte[] dataInfo){
            #if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel(
            "Save map data",
            "",
             "Area.binary",
            "binary");
            System.IO.File.WriteAllBytes(path,dataMap);
            System.IO.File.WriteAllBytes(path+".info",dataInfo);
            #endif
        }
        public void LoadFile(){
            byte[] data = System.IO.File.ReadAllBytes(MapName);
            byte[] dataInfo = System.IO.File.ReadAllBytes(MapName+".info");
            EskyTracker.instance.LoadEskyMapInformation(data,dataInfo);
        }
        public void OnDestroy(){
        }
    }
}