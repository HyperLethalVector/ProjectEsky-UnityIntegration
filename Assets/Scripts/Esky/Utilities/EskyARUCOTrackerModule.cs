using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine;

namespace BEERLabs.ProjectEsky.Extras.Modules{
    [System.Serializable]
    public class ARUCOModuleInfo{
        [SerializeField]
        public int InstanceID;
        [HideInInspector]public SensorImageSource myImageSource;
        public int dictID;
        public int markerID;
        public float MarkerLengthInMeters = 0.05f;
        public float MarkerLengthInPixels = 200f;
        public int borderBits;
    }
    
    public class EskyARUCOTrackerModule : MonoBehaviour
    {
        public SensorSourceType sourceType;
        static float radians2Deg = 180.0f/3.14159265359f;
        public ARUCOModuleInfo markerInfo;
        int instanceID;
        public static Dictionary<int,EskyARUCOTrackerModule> moduleInstances = new Dictionary<int, EskyARUCOTrackerModule>();
        public delegate void FuncPoseReceiveCallback(int instanceID, float tx, float ty, float tz, float rx, float ry, float rz);
        [SerializeField]Vector3 LocalMarkerPosition = Vector3.zero;
        [SerializeField]Quaternion LocalMarkerRotation = Quaternion.identity;
        RGBSensorModuleCalibrations myCalibrations;
        public bool printMarker = false;
        public bool isWorldCenter = false;
        bool isInitialized = false;
        bool receivedFirstFrame = false;
        Matrix4x4 transformToMarker = Matrix4x4.identity;
        Matrix4x4 transfromFromMarker;
        // Start is called before the first frame update
        void Awake(){

            instanceID = markerInfo.InstanceID;
            if(!moduleInstances.ContainsKey(instanceID)){
                moduleInstances.Add(instanceID,this);
            }else{
                Debug.LogError("Duplicate InstanceID: " + instanceID);
            }
            RegisterDebugCallback(OnDebugCallback);
            InitTrackerModule(markerInfo.dictID, markerInfo.markerID,markerInfo.MarkerLengthInMeters,instanceID);
            SubscribeToPoseCallback(markerInfo.InstanceID,ReceivePoseCallback);
        }
        bool hasSubscribed = false;
        void Start()
        {
            instanceID = markerInfo.InstanceID;

        }
        float TimeoutBeforeLocking = 0.01f;
        float DetectionLeft = 0.01f;
        [MonoPInvokeCallback(typeof(FuncPoseReceiveCallback))]
        static void ReceivePoseCallback(int instanceID, float tx, float ty, float tz, float rx, float ry, float rz){
            moduleInstances[instanceID].DetectionLeft = moduleInstances[instanceID].TimeoutBeforeLocking;
            moduleInstances[instanceID].LocalMarkerPosition.x = tx;
            moduleInstances[instanceID].LocalMarkerPosition.y = -ty;
            moduleInstances[instanceID].LocalMarkerPosition.z = tz;
            moduleInstances[instanceID].LocalMarkerRotation = Quaternion.Inverse(Quaternion.Euler(
                (rx*radians2Deg)+180,
                (-rz*radians2Deg)+180,
                -ry*radians2Deg//rx*radians2Deg*flipZZ
                ));
  //          moduleInstances[instanceID].transformToMarker.SetTRS(moduleInstances[instanceID].LocalMarkerPosition,moduleInstances[instanceID].LocalMarkerRotation,Vector3.one);
//            moduleInstances[instanceID].transformToMarker = moduleInstances[instanceID].transformToMarker.inverse;
        }
        public void ReceiveImage(ImageData id){
           // Debug.Log("Receiving Image");
            if(!receivedFirstFrame){
            //    Debug.Log("Receiving First Image");                
                receivedFirstFrame = true;
                myCalibrations = markerInfo.myImageSource.myCalibrations;
                InitARUCOTrackerParams(
                instanceID,
                markerInfo.MarkerLengthInMeters,
                myCalibrations.fx,
                myCalibrations.fy,
                myCalibrations.cx,
                myCalibrations.cy,
                myCalibrations.d1,
                myCalibrations.d2,
                myCalibrations.d3,
                myCalibrations.d4);
            }else{            
//                Debug.Log("Receiving Subsequent Image");                                    
                ProcessImage(instanceID,id.info,id.lengthOfArray,id.width,id.height,id.pixelCount);
            }            
        }
        // Update is called once per frame
        void FixedUpdate()
        {
            if(markerInfo.myImageSource == null){
               markerInfo.myImageSource = sourceType == SensorSourceType.RGB?SensorImageSource.RGBImageSource:SensorImageSource.GrayscaleImageSource;
                if(markerInfo.myImageSource != null){
                 //   Debug.Log("Subscribing");
                   markerInfo.myImageSource.SubscribeImageCallback(ReceiveImage);
                }
            }            
        }
        void Update()
        {
            
            if(!hasSubscribed){
                hasSubscribed = true;
            }
            if(markerInfo.myImageSource != null){
                transfromFromMarker.SetTRS(LocalMarkerPosition,LocalMarkerRotation,Vector3.one);
                transfromFromMarker = transformToMarker.inverse;
                if(isWorldCenter){
                    markerInfo.myImageSource.transform.position = transformToMarker.MultiplyPoint(Vector3.zero);
                    markerInfo.myImageSource.transform.rotation = transformToMarker.rotation;                    
                }else{
                    if(DetectionLeft > 0f){
                        DetectionLeft -= Time.deltaTime;
                        transform.position = markerInfo.myImageSource.transform.TransformPoint(LocalMarkerPosition);
                        transform.rotation = markerInfo.myImageSource.transform.rotation * LocalMarkerRotation;                    
                    }
                }

            }
            if(printMarker){
                printMarker = false;
                PrintMarker(instanceID,
                "Marker_"+markerInfo.markerID+"_"+markerInfo.dictID+"_"+markerInfo.borderBits+"_"+markerInfo.MarkerLengthInPixels+"x"+markerInfo.MarkerLengthInPixels+".png",
                markerInfo.markerID,
                markerInfo.MarkerLengthInPixels,
                markerInfo.borderBits);
            }
        }
        [DllImport("libProjectEskyARUCOTrackerModule")]
        static extern void InitTrackerModule(int dictID, int markerID, float markerLength, int InstanceID);

        [DllImport("libProjectEskyARUCOTrackerModule")]
        static extern void SubscribeToPoseCallback(int InstanceID, FuncPoseReceiveCallback callback);
        [DllImport("libProjectEskyARUCOTrackerModule")]
        static extern void ProcessImage(int InstanceID, IntPtr imagedataRaw,int totalLength, int imageWidth,int imageHeight, int channels);

        [DllImport("libProjectEskyARUCOTrackerModule")]
        static extern void InitARUCOTrackerParams(int InstanceID, float marker_size, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);
        [DllImport("libProjectEskyARUCOTrackerModule")]
        static extern void PrintMarker(int InstanceID, string imageName, int markerID, float markerSize, int borderBits);
        [DllImport("libProjectEskyARUCOTrackerModule")]
        public static extern void RegisterDebugCallback(FuncCallBack callback);
        public delegate void FuncCallBack(IntPtr message, int color, int size);
          [MonoPInvokeCallback(typeof(FuncCallBack))]
        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            //Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);

            //Add Specified Color
            debug_string =
                String.Format("{0}{1}{2}{3}{4}",
                "<color=",
                ((Color)color).ToString(),
                ">",
                debug_string,
                "</color>"
                );
            UnityEngine.Debug.Log("RGB Module: " + debug_string);            
        }
    }
}