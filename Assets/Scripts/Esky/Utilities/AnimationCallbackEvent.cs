using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace BEERLabs.ProjectEsky.Utilities{    
    public class AnimationCallbackEvent : MonoBehaviour
    {
        public UnityEvent callbackEvent;
        // Start is called before the first frame update
        public void PerformCallback(){
            if(callbackEvent != null){
                callbackEvent.Invoke();
            }
        }
        public void TriggerAction(){
            GetComponent<Animator>().SetTrigger("PerformAction");
        }

    }
}
