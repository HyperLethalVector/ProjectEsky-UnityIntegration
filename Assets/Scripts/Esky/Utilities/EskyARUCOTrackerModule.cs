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
        [SerializeField]
        public SensorImageSource myImageSource;
        public int dictID;
        public int markerID;
        public float MarkerLengthInMeters = 0.05f;
        public float MarkerLengthInPixels = 200f;
        public string FileToSaveMarkerName = "Marker";
        public int borderBits;
    }
    
    public class EskyARUCOTrackerModule : MonoBehaviour
    {
        static float radians2Deg = 180.0f/3.14159265359f;
        public ARUCOModuleInfo myInfo;
        int instanceID;
        public static Dictionary<int,EskyARUCOTrackerModule> moduleInstances = new Dictionary<int, EskyARUCOTrackerModule>();
        public delegate void FuncPoseReceiveCallback(int instanceID, float tx, float ty, float tz, float rx, float ry, float rz);
        Vector3 LocalMarkerPosition = Vector3.zero;
        Quaternion LocalMarkerRotation = Quaternion.identity;
        public RGBSensorModuleCalibrations myCalibrations;
        public bool printMarker = false;
        public bool isWorldCenter = false;
        bool isInitialized = false;
        bool receivedFirstFrame = false;
        Matrix4x4 transformToMarker = Matrix4x4.identity;
        Matrix4x4 transfromFromMarker;
        // Start is called before the first frame update
        void Awake(){

            instanceID = myInfo.InstanceID;
            if(!moduleInstances.ContainsKey(instanceID)){
                moduleInstances.Add(instanceID,this);
            }else{
                Debug.LogError("Duplicate InstanceID: " + instanceID);
            }
            RegisterDebugCallback(OnDebugCallback);
            InitTrackerModule(myInfo.dictID, myInfo.markerID,myInfo.MarkerLengthInMeters,instanceID);
            SubscribeToPoseCallback(myInfo.InstanceID,ReceivePoseCallback);
        }
        bool hasSubscribed = false;
        void Start()
        {
            instanceID = myInfo.InstanceID;

        }

        public static void ReceiveImageCallback(int instanceID, IntPtr info, int lengthofarray, int width, int height, int pixelCount){
            Debug.Log("Processing Marker Image");
            if(!moduleInstances[instanceID].receivedFirstFrame){
                moduleInstances[instanceID].receivedFirstFrame = true;
                moduleInstances[instanceID].myCalibrations = moduleInstances[instanceID].myInfo.myImageSource.myCalibrations;
                InitARUCOTrackerParams(
                moduleInstances[instanceID].instanceID,
                moduleInstances[instanceID].myInfo.MarkerLengthInMeters,
                moduleInstances[instanceID].myCalibrations.fx,
                moduleInstances[instanceID].myCalibrations.fy,
                moduleInstances[instanceID].myCalibrations.cx,
                moduleInstances[instanceID].myCalibrations.cy,
                moduleInstances[instanceID].myCalibrations.d1,
                moduleInstances[instanceID].myCalibrations.d2,
                moduleInstances[instanceID].myCalibrations.d3,
                moduleInstances[instanceID].myCalibrations.d4);
            }else{            
                if(moduleInstances[instanceID].isInitialized){
                    ProcessImage(moduleInstances[instanceID].instanceID,info,lengthofarray,width,height,pixelCount);
                }
            }
        }
        [MonoPInvokeCallback(typeof(FuncPoseReceiveCallback))]
        static void ReceivePoseCallback(int instanceID, float tx, float ty, float tz, float rx, float ry, float rz){
            moduleInstances[instanceID].LocalMarkerPosition.x = -tx;
            moduleInstances[instanceID].LocalMarkerPosition.y = ty;
            moduleInstances[instanceID].LocalMarkerPosition.z = -tz;
            moduleInstances[instanceID].LocalMarkerRotation = Quaternion.Euler(-rx*radians2Deg,ry*radians2Deg,-rz*radians2Deg);

        }
        // Update is called once per frame

        void Update()
        {
            if(!hasSubscribed){
                hasSubscribed = true;
                if(myInfo.myImageSource != null){
                    myInfo.myImageSource.SubscribeCallback(instanceID,ReceiveImageCallback);
                    isInitialized = true;
                }
            }
            if(myInfo.myImageSource != null){
                transfromFromMarker.SetTRS(LocalMarkerPosition,LocalMarkerRotation,Vector3.one);
                transfromFromMarker = transformToMarker.inverse;
                if(isWorldCenter){
                    myInfo.myImageSource.transform.position = transformToMarker.MultiplyPoint(Vector3.zero);
                    myInfo.myImageSource.transform.rotation = transformToMarker.rotation;                    
                }else{
                    transform.position = myInfo.myImageSource.transform.worldToLocalMatrix.MultiplyPoint(transfromFromMarker.MultiplyPoint(Vector3.zero));
                    transform.rotation = myInfo.myImageSource.transform.rotation * transfromFromMarker.rotation;                    
                }

            }
            if(printMarker){
                printMarker = false;
                PrintMarker(instanceID,myInfo.FileToSaveMarkerName,myInfo.markerID,myInfo.MarkerLengthInPixels,myInfo.borderBits);
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