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
    public class EskyTracker : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent ReLocalizationCallback;
        public UnityEngine.Events.UnityEvent<byte[],byte[]> mapCollectedCallback;
        Dictionary<string,GameObject> subscribedIDs = new Dictionary<string, GameObject>();
        public static EskyTracker instance;
        delegate void EventCallback(int Result);
        delegate void MapDataCallback(IntPtr data, int Length);
        delegate void PoseReceivedCallback(string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw);
        bool didInitializeTracker = false;
        Vector3 velocity = Vector3.zero;
        Vector3 velocityRotation = Vector3.zero;
        public float smoothing = 0.1f;
        public float smoothingRotation= 0.1f;
        float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        float[] currentRealsenseObject = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        public Matrix4x4 TransformFromTrackerToCenter;
        public Transform RigCenter;
        Vector3 currentEuler = Vector3.zero;
        EskyMap myCurrentMap;
        List<EskyPoseCallbackData> callbackEvents = new List<EskyPoseCallbackData>();
        // Start is called before the first frame update
        void Start()
        {
            RegisterDebugCallback(OnDebugCallback);    
            InitializeTrackerObject();
            RegisterBinaryMapCallback(OnMapCallback);
            RegisterObjectPoseCallback(OnPoseReceivedCallback);

            RegisterLocalizationCallback(OnEventCallback);            
            Debug.Log("Updating map binaries");
            StartTrackerThread(false);        
        }
        EskyMap em;
        public void LoadEskyMapInformation(byte[] eskyMapData,byte[] eskyMapInfo){
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(eskyMapInfo);
            em  = (EskyMap)bf.Deserialize(ms);
            SetMapData(eskyMapData,eskyMapData.Length);
        }
        public bool ShouldGrabMapTest= false;
        public void ObtainPose(){
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
            instance = this;

        }
        public void SubscribeAnchor(string ID, GameObject gameObject){
            if(!subscribedIDs.ContainsKey(ID)){
                subscribedIDs.Add(ID,gameObject);
            }else{
                subscribedIDs[ID] = gameObject;
            }
        }
        public void UnSubscribeAnchor(string ID, GameObject gameObject){
            if(!subscribedIDs.ContainsKey(ID)){
                subscribedIDs.Remove(ID);
            }
        }
        public void SaveObjectPoses(){
            foreach(KeyValuePair<string, GameObject> kvpgo in subscribedIDs){
                (Vector3,Quaternion) ppqq = UnityPoseToIntel(kvpgo.Value.transform.position,kvpgo.Value.transform.rotation);
                Debug.Log("Start Saving Object: " + kvpgo.Key);
                SetObjectPoseInLocalizedMap(kvpgo.Key,ppqq.Item1.x,ppqq.Item1.y,ppqq.Item2.z,ppqq.Item2.x,ppqq.Item2.y,ppqq.Item2.z,ppqq.Item2.w);
                Debug.Log("Done Saving Object: " + kvpgo.Key);                
                //Should add some form of callback scripts
            }
        }
        public void SaveTheMap(){
            SaveObjectPoses();
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
            if(callbackEvents.Count > 0){
                while(callbackEvents.Count > 0){
                    EskyPoseCallbackData epcd = callbackEvents[0];
                    callbackEvents.RemoveAt(0);
                    Debug.Log("Received Callback and processing for: " + epcd.PoseID);
                    if(subscribedIDs.ContainsKey(epcd.PoseID)){
                        subscribedIDs[epcd.PoseID].transform.position = epcd.position;
                        subscribedIDs[epcd.PoseID].transform.rotation = epcd.rotation;                        
                    }
                }
            }
        }
        public void ObtainObjectPoses(){
             foreach(KeyValuePair<string,GameObject> kvp in subscribedIDs){
                try{
                    ObtainObjectPoseInLocalizedMap(kvp.Key);                                    
                }catch(System.Exception e){
                    Debug.LogError(e);
                }
            }
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
        [DllImport("libProjectEskyLLAPI")]
        private static extern void SaveOriginPose();
        [DllImport("libProjectEskyLLAPI")]
        private static extern IntPtr GetLatestPose();
        [DllImport("libProjectEskyLLAPI")]
        private static extern void InitializeTrackerObject();

        [DllImport("libProjectEskyLLAPI")]
        private static extern void StartTrackerThread(bool useLocalization);
  //      [DllImport("libProjectEskyLLAPI")]
//        private static extern void 
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
        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
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
        [DllImport("libProjectEskyLLAPI")]
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
                myMap.objectIDs = new List<string>();
                foreach(KeyValuePair<string,GameObject> pairs in instance.subscribedIDs){
                    myMap.objectIDs.Add(pairs.Key);
                }

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
            callbackEvents.Add(epcd);
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

        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterObjectPoseCallback(PoseReceivedCallback poseReceivedCallback);
        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterLocalizationCallback(EventCallback cb);

        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterBinaryMapCallback(MapDataCallback cb);
        [DllImport("libProjectEskyLLAPI")]
        static extern void SetBinaryMapData(string inputBytesLocation);
        [DllImport("libProjectEskyLLAPI")]
        static extern void SetObjectPoseInLocalizedMap(string objectID,float tx, float ty, float tz, float qx, float qy, float qz, float qw);
        [DllImport("libProjectEskyLLAPI")]
        static extern void ObtainObjectPoseInLocalizedMap(string objectID);
        [DllImport("libProjectEskyLLAPI")]
        static extern void ObtainMap();
        [DllImport("libProjectEskyLLAPI")]
        static extern void SetMapData(byte[] inputData, int Length);
    }
}