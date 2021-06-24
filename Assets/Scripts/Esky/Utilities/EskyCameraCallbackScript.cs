using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace BEERLabs.ProjectEsky.Utilities{
    public class EskyCameraCallbackScript : MonoBehaviour
    {
        // Start is called before the first frame update
        public UnityEvent OnPreRenderEvents;
        // Update is called once per frame
        private void OnPreRender() {
            if(OnPreRenderEvents != null){
                OnPreRenderEvents.Invoke();
            }
        }
    }
}