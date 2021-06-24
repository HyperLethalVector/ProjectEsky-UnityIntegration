using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    public class EskyHMDOrigin : MonoBehaviour
    {
        public bool isAR = false;
        public static EskyHMDOrigin instance;
        private void Awake() {
            instance = this;    
        }
    }
}