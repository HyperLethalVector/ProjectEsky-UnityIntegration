using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace ProjectEsky{
    [System.Serializable]
    public class RGBSensorModuleCalibrations{
        [SerializeField]
        public int camID;
        [SerializeField]        
        public float fx;
        [SerializeField]        
        public float fy;
        [SerializeField]        
        public float cx;
        [SerializeField]        
        public float cy;
        [SerializeField]        
        public float d1;
        [SerializeField]        
        public float d2;
        [SerializeField]        
        public float d3;
        [SerializeField]        
        public float d4;        
        [SerializeField]
        public int SensorWidth;
        [SerializeField]
        public int SensorHeight;
        [SerializeField]
        public int SensorChannels;
        [SerializeField]
        public float SensorFoV;
    }
    public delegate void ReceiveSensorImageCallback(IntPtr info, int lengthofarray, int width, int height, int pixelCount);
    public delegate void ReceiveSensorImageCallbackWithInstanceID(int instanceID, IntPtr info, int lengthofarray, int width, int height, int pixelCount);

    
    public abstract class SensorImageSource : MonoBehaviour{
        public RGBSensorModuleCalibrations myCalibrations;
        public virtual void SubscribeCallback(ReceiveSensorImageCallback callback){

        }
        public virtual void SubscribeCallback(int instanceID, ReceiveSensorImageCallbackWithInstanceID callback){

        }
    }
}