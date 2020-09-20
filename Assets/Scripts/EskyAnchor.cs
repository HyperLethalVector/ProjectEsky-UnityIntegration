using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyAnchor : MonoBehaviour
    {
        [HideInInspector]
        public Dictionary<string,EskyAnchorContent> myContent = new Dictionary<string, EskyAnchorContent>();
        public Transform contentOrigin;
        public static EskyAnchor instance;
        private void Awake() {
            instance = this;
            Debug.Log("Subscribing: origin");
            ProjectEsky.Tracking.EskyTracker.instance.SubscribeAnchor("origin",this.gameObject);    
        }
        public static void Subscribe(EskyAnchorContent contenttosubscribe){
            if(instance != null){
                if(!instance.myContent.ContainsKey(contenttosubscribe.ContentID)){
                    instance.myContent.Add(contenttosubscribe.ContentID,contenttosubscribe);
                    contenttosubscribe.transform.parent = instance.transform;                   
                }

            } 
        }
        public void RelocalizationCallback(){
            foreach(KeyValuePair<string,EskyAnchorContent> kvpeac in myContent){
                kvpeac.Value.OnLocalizedCallback();
            }
        }
    }
}