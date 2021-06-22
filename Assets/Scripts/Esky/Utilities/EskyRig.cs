using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky{
    public class EskyRig : MonoBehaviour
    {
        public static EskyRig instance;
        void Awake(){
            instance = this;
        }
    }
}