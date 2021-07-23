using System.Net.Mime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace BEERLabs.ProjectEsky{
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
    public delegate void ReceiveSensorImageCallbackWithInstanceID(int instanceID, IntPtr info, int lengthofarray, int width, int height, int pixelCount);
    public enum SensorSourceType{
        GrayScale,
        RGB
    }
    public class ImageData{
        public int TrackerID;
        public IntPtr info; 
        public int lengthOfArray; 
        public int width; 
        public int height; 
        public int pixelCount;
    }
    
    public abstract class SensorImageSource : MonoBehaviour{
        public RGBSensorModuleCalibrations myCalibrations;
        public UnityEngine.Events.UnityEvent<ImageData> imageDataCallbacks;
        public static SensorImageSource RGBImageSource;
        public static SensorImageSource GrayscaleImageSource;
        
        public virtual void SubscribeCallback(int instanceID, ReceiveSensorImageCallbackWithInstanceID callback){

        }
        public void SendImageData(IntPtr info, int lengthOfArray, int width, int height, int pixelCount){
           // Debug.Log("Sending image data to callbacks from " + (this==RGBImageSource?"RGB":"Grayscale") + " image source");
            ImageData d = new ImageData();
            d.info = info;
            d.lengthOfArray = lengthOfArray;
            d.width = width;
            d.height = height;
            d.pixelCount = pixelCount;
            imageDataCallbacks?.Invoke(d);
        }
        public void SubscribeImageCallback(UnityAction<ImageData> callback){
            Debug.Log("Subscribing Image Callback");
            imageDataCallbacks.AddListener(callback);
        }
    }
}