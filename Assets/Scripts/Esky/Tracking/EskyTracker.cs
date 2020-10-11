using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProjectEsky.Tracking{
    
    [System.Serializable]
    public class EskyMap{
        public byte[] meshDataArray;
        public List<string> objectIDs;
        public byte[] extraInformation;
        public string FileLocation;
    }
    public class EskyPoseCallbackData{
        public string PoseID;
        public Quaternion rotation;
        public Vector3 position;
    }
    [System.Serializable]
    public class MapSavedCallback : UnityEngine.Events.UnityEvent<byte[],byte[]>{

    }
    public class EskyTracker : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent ReLocalizationCallback;
        [SerializeField]
        public MapSavedCallback mapCollectedCallback;
        public GameObject subscribedAnchor;// = new Dictionary<string, GameObject>();
        public static EskyTracker instance;
        delegate void EventCallback(int Result);
        delegate void MapDataCallback(IntPtr data, int Length);
        delegate void PoseReceivedCallback(string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw);
        [HideInInspector]        
        public Vector3 velocity = Vector3.zero;
        [HideInInspector]
        public Vector3 velocityRotation = Vector3.zero;
        public float smoothing = 0.1f;
        public float smoothingRotation= 0.1f;
        [HideInInspector]
        public float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        [HideInInspector]        
        public float[] currentRealsenseObject = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        public Matrix4x4 TransformFromTrackerToCenter;
        public Transform RigCenter;
        [HideInInspector]
        public Vector3 currentEuler = Vector3.zero;
        EskyMap myCurrentMap;
        EskyPoseCallbackData callbackEvents = null;
        // Start is called before the first frame update
        void Start()
        {
            
            InitializeTrackerObject();
            RegisterBinaryMapCallback(OnMapCallback);
            RegisterObjectPoseCallback(OnPoseReceivedCallback);

            RegisterLocalizationCallback(OnEventCallback);            
            StartTrackerThread(false);        
            AfterInitialization();
        }
        public virtual void AfterInitialization(){

        }
        EskyMap em;
        public void LoadEskyMapInformation(byte[] eskyMapData,byte[] eskyMapInfo){
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(eskyMapInfo);
            em  = (EskyMap)bf.Deserialize(ms);
            SetMapData(eskyMapData,eskyMapData.Length);
        }
        public bool ShouldGrabMapTest= false;
        public virtual void ObtainPose(){
            IntPtr ptr = GetLatestPose();                
            Marshal.Copy(ptr, currentRealsensePose, 0, 7);
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, new Vector3(currentRealsensePose[0],currentRealsensePose[1],-currentRealsensePose[2]),ref velocity,smoothing); 
            Quaternion q = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);            
            currentEuler = Vector3.SmoothDamp(transform.localRotation.eulerAngles,new Vector3(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z),ref velocityRotation,smoothingRotation);
            transform.localRotation = Quaternion.Euler(currentEuler);    

            Matrix4x4 m = Matrix4x4.TRS(transform.transform.position,transform.transform.rotation,Vector3.one);
            m = m * TransformFromTrackerToCenter.inverse;
            if(RigCenter != null){
                RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
                RigCenter.transform.rotation = m.rotation;
            }
        }    
        void Awake(){
            RegisterDebugCallback(OnDebugCallback);    
            instance = this;
        }

        public void SaveTheMap(){
            ObtainMap();
        }
        // Update is called once per frame
        void Update()
        {
            ObtainPose();
            if(ShouldGrabMapTest){
                ShouldGrabMapTest = false;
                SaveTheMap();
            }
            if(UpdateLocalizationCallback){
                UpdateLocalizationCallback = false;
                ObtainObjectPoses();
                if(instance.ReLocalizationCallback != null){
                    instance.ReLocalizationCallback.Invoke();
                }                
            }
            if(ShouldCallBackMap){                
                if(instance.mapCollectedCallback != null){
                    instance.mapCollectedCallback.Invoke(callbackMemoryMap,callbackMemoryMapInfo);
                }
                ShouldCallBackMap = false;
            }
            if(callbackEvents != null){
                if(EskyAnchor.instance != null){
                    EskyAnchor.instance.transform.position = callbackEvents.position;
                    EskyAnchor.instance.transform.rotation = callbackEvents.rotation;                        
                }
                callbackEvents = null;                
            }
            AfterUpdate();
        }
        public virtual void AfterUpdate(){

        }
        public void ObtainObjectPoses(){             
            ObtainObjectPoseInLocalizedMap("origin_of_map");                                    
        }
        public (Vector3,Quaternion) IntelPoseToUnity(float[] inputPose){
            Quaternion q = new Quaternion(inputPose[3],inputPose[4],inputPose[5],inputPose[6]);
            Vector3 p = new Vector3(inputPose[0],inputPose[1],-inputPose[2]);
            q = Quaternion.Euler(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z);
            return (p,q);
        }
        public (Vector3,Quaternion) IntelPoseToUnity(float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            Quaternion q = new Quaternion(qx,qy,qz,qw);
            Vector3 p = new Vector3(tx,ty,-tz);
            q = Quaternion.Euler(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z);
            return (p,q);
        }
        public (Vector3,Quaternion) UnityPoseToIntel(Vector3 position, Quaternion orientation){
            Quaternion qq = Quaternion.Euler(-orientation.eulerAngles.x,-orientation.eulerAngles.y,orientation.eulerAngles.z);
            Vector3 pp = new Vector3(position.x,position.y,-position.z);
            return (pp,qq);
        }
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        public static extern void SaveOriginPose();
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        public static extern IntPtr GetLatestPose();
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        public static extern void InitializeTrackerObject();

        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        public static extern void StartTrackerThread(bool useLocalization);
        bool UpdateLocalizationCallback = false;
        [MonoPInvokeCallback(typeof(EventCallback))]
        static void OnEventCallback(int Response)
        {
            switch(Response){
                case 1:
                Debug.Log("We received the localization event!!");
                if(instance != null){
                    instance.UpdateLocalizationCallback = true;
                }
                break;
                default:
                Debug.Log("We received a callback I'm unfamiliar with, sorry!: " + Response);
                break;
            }
            //Ptr to string
        }
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        #else
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        #endif
        static extern void RegisterDebugCallback(debugCallback cb);
        delegate void debugCallback(IntPtr request, int color, int size);
         enum Color { red, green, blue, black, white, yellow, orange };
        [MonoPInvokeCallback(typeof(debugCallback))]
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

            UnityEngine.Debug.Log("Realsense Tracker: " + debug_string);
        }
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void StopTrackers();
        void OnDestroy(){
            StopTrackers();
        }
        byte[] callbackMemoryMap;
        byte[] callbackMemoryMapInfo;
        bool ShouldCallBackMap= false;

        [MonoPInvokeCallback(typeof(MapDataCallback))]
        static void OnMapCallback(IntPtr receivedData, int Length)
        {
            byte[] received = new byte[Length];
            Marshal.Copy(receivedData, received, 0, Length);
            Debug.Log("Received map data of length: " + Length);
            if(instance != null){
                EskyMap myMap = new EskyMap();
                instance.callbackMemoryMap = received;
                //I should collect the mesh data here
                BinaryFormatter b = new BinaryFormatter();
                MemoryStream memoryStream= new MemoryStream();
                b.Serialize(memoryStream,myMap);
                instance.callbackMemoryMapInfo = memoryStream.ToArray();
                instance.ShouldCallBackMap = true;
            }else{
                Debug.LogError("The instance of the tracker was null, cancelling data map export");
            }
            //System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
        }
        public void AddPoseFromCallback(EskyPoseCallbackData epcd){
            callbackEvents = epcd;
        }
        [MonoPInvokeCallback(typeof(PoseReceivedCallback))]
        static void OnPoseReceivedCallback(string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            EskyPoseCallbackData epcd = new EskyPoseCallbackData();
            (Vector3, Quaternion) vq = instance.IntelPoseToUnity(tx,ty,tz,qx,qy,qz,qw);            
            epcd.PoseID = ObjectID;
            epcd.position = vq.Item1;
            epcd.rotation = vq.Item2;
            instance.AddPoseFromCallback(epcd);
            Debug.Log("Received a pose from the relocalization");
        }
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        #else
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        #endif
        static extern void RegisterObjectPoseCallback(PoseReceivedCallback poseReceivedCallback);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        #else
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        #endif
        static extern void RegisterLocalizationCallback(EventCallback cb);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        #else
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        #endif
        static extern void RegisterBinaryMapCallback(MapDataCallback cb);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void SetBinaryMapData(string inputBytesLocation);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void SetObjectPoseInLocalizedMap(string objectID,float tx, float ty, float tz, float qx, float qy, float qz, float qw);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void ObtainObjectPoseInLocalizedMap(string objectID);
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void ObtainMap();
        #if ZED_SDK
        [DllImport("libProjectEskyLLAPIZED")]        
        #else
        [DllImport("libProjectEskyLLAPIIntel")]
        #endif
        static extern void SetMapData(byte[] inputData, int Length);
    }
}