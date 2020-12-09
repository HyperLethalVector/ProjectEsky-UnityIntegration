using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProjectEsky.Tracking{

    public enum ComPort{
        COM0 = 0,
        COM1 = 1,
        COM2 = 2,
        COM3=  3,
        COM4 = 4,
        COM5 = 5,
        COM6 = 6,
        COM7 = 7,
        COM8 = 8,
        COM9 = 9,
        COM10 = 10
    }
    public class EskyTrackerIntel : EskyTracker
    {
        float rad2Deg = 180.0f/3.141592653589793238463f;
        public Camera previewCamera;
        public RenderTexture tex;
        public UnityEngine.UI.RawImage myImage;
        bool canRenderImages = false;     
        public bool UsesDeckXIntegrator;
        public ComPort comPort;
        void Start()
        {
            RegisterDebugCallback(OnDebugCallback);    

            LoadCalibration();
            InitializeTrackerObject();
            RegisterQuaternionConversionCallback(ConvertToQuaternion);            
            RegisterBinaryMapCallback(OnMapCallback);
            RegisterObjectPoseCallback(OnPoseReceivedCallback);
            if(UsesDeckXIntegrator){
                SetSerialComPort((int)comPort);
            }
            RegisterLocalizationCallback(OnEventCallback);            
            StartTrackerThread(false);        
            AfterInitialization();
            SetTextureInitializedCallback(OnTextureInitialized);            
        }
        public override void LoadEskyMap(EskyMap m){
            retEskyMap = m;
            if(File.Exists("temp.raw"))File.Delete("temp.raw");
            System.IO.File.WriteAllBytes("temp.raw",m.mapBLOB);
            SetMapData(new byte[]{},0);
        }
        public override void ObtainObjectPoses(){             
            ObtainObjectPoseInLocalizedMap("origin_of_map");
        }
        public override void AfterUpdate()
        {
            base.AfterUpdate();
            if(hasInitializedTexture){
                ChangeCameraParam(textureWidth,textureHeight,fx,fy,cx,cy,fovx,fovy);
                HookDeviceToIntel();
                Debug.Log("Creating texture with: " + textureChannels + " channels");
                hasInitializedTexture = false;
                if(textureChannels == 4){
//                    previewCamera.fieldOfView = cam_v_fov;

                    tex = new RenderTexture(textureWidth,textureHeight,0,RenderTextureFormat.BGRA32);
                    tex.Create();
                    SetRenderTexturePointer(tex.GetNativeTexturePtr());
                    if(myImage != null){
                        myImage.texture = tex;
                        myImage.gameObject.SetActive(true);
                    }
                    canRenderImages = true;
                    StartCoroutine(WaitEndFrameCameraUpdate());
                }else{
                    tex = new RenderTexture(textureWidth,textureHeight,0,RenderTextureFormat.R16);
                    tex.Create();
                    SetRenderTexturePointer(tex.GetNativeTexturePtr());
                    if(myImage != null){
                        myImage.texture = tex;
                        myImage.gameObject.SetActive(true);
                    }
                    canRenderImages = true;
                    StartCoroutine(WaitEndFrameCameraUpdate());                    
                }
            }
        }
        public override void ObtainPose(){
            if(ApplyPoses){
                IntPtr ptr = GetLatestPose();                 
                Marshal.Copy(ptr, currentRealsensePose, 0, 7);
                transform.position =  new Vector3(currentRealsensePose[0],currentRealsensePose[1],-currentRealsensePose[2]);//.Vector3.SmoothDamp(transform.position, new Vector3(currentRealsensePose[0],currentRealsensePose[1],-currentRealsensePose[2]),ref velocity,smoothing); 
                Vector3 eulerRet = new Vector3(-currentRealsensePose[5],-currentRealsensePose[4],currentRealsensePose[3]); 
                transform.rotation = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);
//                transform.rotation = Quaternion.Euler(eulerRet);

            }
        } 
        public override void SaveEskyMapInformation(){
            ObtainMap();
        }
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
            #if ZED_SDK
            UnityEngine.Debug.Log("ZED Tracker: " + debug_string);
            #else
            UnityEngine.Debug.Log("Realsense Tracker: " + debug_string);            
            #endif
        }
       
        [MonoPInvokeCallback(typeof(MapDataCallback))]
        static void OnMapCallback(IntPtr receivedData, int Length)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while(!File.Exists("temp.raw.area")){
                TimeSpan ts = sw.Elapsed;
                if(ts.TotalSeconds > 4)break;                
            }
            sw.Restart();
            bool didComplete = false;
            byte[] received = null;
            while(!didComplete){//this sucks, but it's the only way to wait til the zed has actually _finished_ writing the file
                try{
                    #if ZED_SDK
                    received = System.IO.File.ReadAllBytes("temp.raw.area");
                    #else
                    received = System.IO.File.ReadAllBytes("temp.raw");            
                    #endif

                didComplete = true;//will flag the pass is complete                    
                }catch(System.Exception e){
                    Debug.LogError(e);
                    TimeSpan ts = sw.Elapsed;
                    if(ts.TotalSeconds > 4)break; 
                    
                }
            }
            if(didComplete){
                UnityEngine.Debug.Log("Received map data of length: " + received.Length);
                if(instance != null){
                    EskyMap retEskyMap = new EskyMap();
                    retEskyMap.mapBLOB = received;
                    instance.SetEskyMapInstance(retEskyMap);
                    //I should collect the mesh data here
                }else{
                    UnityEngine.Debug.LogError("The instance of the tracker was null, cancelling data map export");
                }
            }else{
                UnityEngine.Debug.LogError("Problem exporting the map, *shrug*");
            }
            //System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
        }
        delegate void RenderTextureInitialized(int textureWidth, int textureHeight, int textureChannels,float fx, float fy, float cx, float cy, float fovx, float fovy, float focalLength);
        public void AddPoseFromCallback(EskyPoseCallbackData epcd){
            callbackEvents = epcd;
        }
        void OnDestroy(){
            StopTrackers();
        }
        public void ChangeCameraParam(float width, float height, float fx, float fy, float cx, float cy, float fovx, float fovy)
        {    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            double fovXScale = (2.0 * Mathf.Atan ((float)(width / (2.0 * fx)))) / (Mathf.Atan2 ((float)cx, (float)fx) + Mathf.Atan2 ((float)(width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan ((float)(height / (2.0 * fy)))) / (Mathf.Atan2 ((float)cy, (float)fy) + Mathf.Atan2 ((float)(height - cy), (float)fy));
            if (widthScale < heightScale) {
                previewCamera.fieldOfView = (float)(fovx* fovXScale);
            } else {
                previewCamera.fieldOfView = (float)(fovy * fovYScale);
            }
        }
        public delegate void ConvertToQuaternionCallback(IntPtr arrayToCopy, float eux, float euy, float euz);
        public static float[] quat = {0.0f,0.0f,0.0f,0.0f};
        public static Quaternion q = new Quaternion();
        [MonoPInvokeCallback(typeof(ConvertToQuaternionCallback))]
        public static void ConvertToQuaternion (IntPtr arrayToCopy, float eux, float euy, float euz){
            q = Quaternion.Euler(-euz,-euy,eux);
            quat[0] = q.x;
            quat[1] = q.y;
            quat[2] = q.z;     
            quat[3] = q.w;
            Marshal.Copy(quat,0,arrayToCopy,4);
        }

        [MonoPInvokeCallback(typeof(PoseReceivedCallback))]
        static void OnPoseReceivedCallback(string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            EskyPoseCallbackData epcd = new EskyPoseCallbackData();
            (Vector3, Quaternion) vq = instance.IntelPoseToUnity(tx,ty,tz,qx,qy,qz,qw);            
            epcd.PoseID = ObjectID;
            epcd.position = vq.Item1;
            epcd.rotation = vq.Item2;
            ((EskyTrackerIntel)instance).AddPoseFromCallback(epcd);
            UnityEngine.Debug.Log("Received a pose from the relocalization");
        }
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void RegisterQuaternionConversionCallback(ConvertToQuaternionCallback callback);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void HookDeviceToIntel();
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void SaveOriginPose();
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern IntPtr GetLatestPose();
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void InitializeTrackerObject();
        
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void SetSerialComPort(int port);

        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void StartTrackerThread(bool useLocalization);
 
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterDebugCallback(debugCallback cb);
 
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void StopTrackers();

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterObjectPoseCallback(PoseReceivedCallback poseReceivedCallback);

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterLocalizationCallback(EventCallback cb);

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterBinaryMapCallback(MapDataCallback cb);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetBinaryMapData(string inputBytesLocation);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetObjectPoseInLocalizedMap(string objectID,float tx, float ty, float tz, float qx, float qy, float qz, float qw);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void ObtainObjectPoseInLocalizedMap(string objectID);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void ObtainMap();

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetMapData(byte[] inputData, int Length);

        [MonoPInvokeCallback(typeof(RenderTextureInitialized))]
        static void OnTextureInitialized(int textureWidth, int textureHeight, int textureChannels,float fx,float fy, float cx, float cy, float fovx, float fovy, float aspectRatio){
            if(instance != null){
                Debug.Log("Received the texture initializaed callback");
                instance.textureWidth = textureWidth;
                instance.textureHeight = textureHeight;
                instance.textureChannels = textureChannels;
                instance.hasInitializedTexture = true;
                instance.fx = fx;
                instance.fx = fy;
                instance.cx = cx;
                instance.cy = cy;
                instance.fovx = fovx;
                instance.fovy = fovy;
                                                                
            }
        }
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetTextureInitializedCallback(RenderTextureInitialized callback);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetRenderTexturePointer(IntPtr texPointer);
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern IntPtr GetRenderEventFunc();        
        IEnumerator WaitEndFrameCameraUpdate(){
            while(true){
                yield return new WaitForEndOfFrame();
                if(canRenderImages){
                    GL.IssuePluginEvent(GetRenderEventFunc(), 1);
                }
            }
        }
    }
}