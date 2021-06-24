using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace BEERLabs.ProjectEsky{
    public class EskyRigMRTKMediator : MonoBehaviour
    {
        public UnityEvent<string> MRTKConfigReceivers;
        public void SetValues(string serializedprofileToUse){
            MRTKConfigReceivers.Invoke(serializedprofileToUse);
        }
    }
}