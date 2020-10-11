using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyHMDOrigin : MonoBehaviour
    {
        public static EskyHMDOrigin instance;
        private void Awake() {
            instance = this;    
        }
    }
}