using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Networking{
    public class EskyTrackingOrigin : MonoBehaviour
    {
        public static Dictionary<string,EskyTrackingOrigin> OriginsInScene;
        public string originID;
        // Start is called before the first frame update
        void Awake()
        {
            if(!OriginsInScene.ContainsKey(originID)){
                OriginsInScene.Add(originID,this);
            }else{
                Debug.LogError("There's two of the same originIDs in scene, this should never occur!");
            }
        }
        void OnDestroy(){
            OriginsInScene.Remove(originID);
        }
    }
}