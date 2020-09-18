using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyAnchor : MonoBehaviour
    {
        public string ID;
        private void Awake() {
            Debug.Log("Subscribing: " + ID);
            ProjectEsky.Tracking.EskyTracker.instance.SubscribeAnchor(ID,this.gameObject);    
        }
    }
}