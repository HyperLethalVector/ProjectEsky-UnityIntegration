using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    [System.Serializable]
    public enum Hand{
        Left = 0,
        Right = 1
    }
    public class EskyHandOrigin : MonoBehaviour
    {
        public static EskyHandOrigin instanceLeft;
        // Start is called before the first frame update
        public static EskyHandOrigin instanceRight;
        [SerializeField]
        public Hand handedness;
        void Awake(){
            switch(handedness){
                case Hand.Left:
                if(instanceLeft != null){Debug.LogError("Warning! There was already a left hand tracked in scene!");}
                instanceLeft = this;
                break;
                case Hand.Right:
                if(instanceRight != null){Debug.LogError("Warning! There was already a right hand tracked in scene!");}                                
                instanceRight= this;
                break;
            }
        }
    }
}