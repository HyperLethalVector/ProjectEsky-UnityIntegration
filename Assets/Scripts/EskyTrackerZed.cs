using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyTrackerZed : MonoBehaviour
    {
        public static EskyTrackerZed instance;
        delegate void EventCallback(int Result);
        delegate void MapDataCallback(IntPtr data, int Length);
        bool didInitializeTracker = false;
        Vector3 velocity = Vector3.zero;
        Vector3 velocityRotation = Vector3.zero;
        public float smoothing = 0.1f;
        public float smoothingRotation= 0.1f;
        float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        public Matrix4x4 TransformFromTrackerToCenter;
        public Transform RigCenter;
        Vector3 currentEuler = Vector3.zero;
        public TextAsset binaryToLoad;
        public void ObtainPose(){
            IntPtr ptr = GetLatestPoseZed();                
            Marshal.Copy(ptr, currentRealsensePose, 0, 7);
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition,            new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]),ref velocity,smoothing); 
            Quaternion q = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);            
            currentEuler = Vector3.SmoothDamp(transform.localRotation.eulerAngles,new Vector3(q.eulerAngles.x,q.eulerAngles.y,q.eulerAngles.z),ref velocityRotation,smoothingRotation);
            transform.localRotation = Quaternion.Euler(currentEuler);    

            Matrix4x4 m = Matrix4x4.TRS(transform.transform.position,transform.transform.rotation,Vector3.one);
                            m = m * TransformFromTrackerToCenter.inverse;
                            //RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
                            //RigCenter.transform.rotation = m.rotation;
        }    
        void Awake(){
            instance = this;

        }
        // Start is called before the first frame update
        void Start()
        {
            RegisterDebugCallback(OnDebugCallback);    
            ZedInitializeTrackerObject();
            RegisterBinaryMapCallback(OnMapCallback);
            RegisterLocalizationCallback(OnEventCallback);            
            Debug.Log("Updating map binaries");
            StartTrackerThreadZed(false);        
        }
        public bool updateOrigin = false;
        // Update is called once per frame
        void Update()
        {
            ObtainPose();
/*            if(updateOrigin){
                updateOrigin = false;
                SaveOriginPoseZed();
            }*/
        }
        [DllImport("libProjectEskyLLAPI")]
        private static extern void SaveOriginPoseZed();
        [DllImport("libProjectEskyLLAPI")]
        private static extern IntPtr GetLatestPoseZed();
        [DllImport("libProjectEskyLLAPI")]
        private static extern void ZedInitializeTrackerObject();

        [DllImport("libProjectEskyLLAPI")]
        private static extern void StartTrackerThreadZed(bool useLocalization);
  //      [DllImport("libProjectEskyLLAPI")]
//        private static extern void 
        [MonoPInvokeCallback(typeof(EventCallback))]
        static void OnEventCallback(int Response)
        {
            switch(Response){
                case 1:
                Debug.Log("We received the localization event!!");
                break;
            }
            //Ptr to string
        }
        [MonoPInvokeCallback(typeof(MapDataCallback))]
        static void OnMapCallback(IntPtr receivedData, int Length)
        {
            byte[] received = new byte[Length];
            Marshal.Copy(receivedData, received, 0, 7);
            Debug.Log("Received map data of length: " + Length);
            System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
        }
        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterLocalizationCallback(EventCallback cb);

        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterBinaryMapCallback(MapDataCallback cb);
        [DllImport("libProjectEskyLLAPI")]
        static extern void SetBinaryMapData(string inputBytesLocation);
        [DllImport("libProjectEskyLLAPI")]
        static extern void StopTrackers();
        void OnDestroy(){
            StopTrackers();
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

            UnityEngine.Debug.Log("Zed Tracker: " + debug_string);
        }

    }
}