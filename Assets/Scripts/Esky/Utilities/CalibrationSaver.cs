using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Configurations{
    public class CalibrationSaver : MonoBehaviour
    {
        public EskyRig hookedRig;
        // Start is called before the first frame update

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.S)){
                hookedRig.SaveSettings();
            }
        }
    }
}