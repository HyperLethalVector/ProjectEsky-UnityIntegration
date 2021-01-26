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
        public delegate void ConvertToQuaternionCallback(IntPtr arrayToCopy, float eux, float euy, float euz);
        public delegate void DeltaPoseUpdateCallback (int TrackerID, IntPtr deltaLeft, IntPtr deltaRight);        
        delegate void RenderTextureInitialized(int TrackerID, int textureWidth, int textureHeight, int textureChannels,float fx, float fy, float cx, float cy, float fovx, float fovy, float focalLength, float d1, float d2, float d3, float d4, float d5);
        public bool UseExternalCameraPreview = false;
        public bool resetPose = false;
        public Camera previewCamera;
        public RenderTexture tex;
        public UnityEngine.UI.RawImage myImage;
        bool canRenderImages = false;     
        public bool UsesDeckXIntegrator;
        public ComPort comPort;
        public ProjectEsky.Rendering.EskyNativeDxRenderer attachedRenderer;

        public override void AfterAwake()
        {
            RegisterDebugCallback(OnDebugCallback);    
            LoadCalibration();
            InitializeTrackerObject(TrackerID);       
            RegisterBinaryMapCallback(TrackerID,OnMapCallback);
            RegisterObjectPoseCallback(TrackerID, OnLocalizationPoseReceivedCallback);
            if(UsesDeckXIntegrator){
                SetSerialComPort(TrackerID, (int)comPort);
            }
            if(attachedRenderer != null)
            RegisterDeltaPoseUpdate(TrackerID, DeltaMatrixCallback);
            RegisterLocalizationCallback(TrackerID, OnLocalization);            
            RegisterMatrixDeltaConvCallback(TrackerID, DeltaMatrixConvCallback);
            StartTrackerThread(TrackerID, false);    
            AfterInitialization();     
//            SetTextureInitializedCallback(TrackerID, OnTextureInitialized);     
        }
        public override void AfterStart()
        {
            if(attachedRenderer != null){
                for(int i = 0; i < 16; i++){
                        leftEyeTransform[i] = ProjectEsky.Rendering.EskyNativeDxRenderer.leftEyeTransform[i];
                        rightEyeTransform[i] = ProjectEsky.Rendering.EskyNativeDxRenderer.rightEyeTransform[i];                                                
                }
                SetLeftRightEyeTransform(TrackerID,leftEyeTransform,rightEyeTransform);
            }
        }
        public override void LoadEskyMap(EskyMap m){
            retEskyMap = m;
            if(File.Exists("temp.raw"))File.Delete("temp.raw");
            System.IO.File.WriteAllBytes("temp.raw",m.mapBLOB);
            FlagMapImport(TrackerID);
        }
        public override void ObtainObjectPoses(){             
            ObtainOriginInLocalizedMap(TrackerID);
        }
        bool doesSubscribe = true;
        public override void AfterUpdate()
        {

            base.AfterUpdate();
            if(UseExternalCameraPreview){
                if(hasInitializedTexture){
                    ChangeCameraParam(textureWidth,textureHeight);
                    HookDeviceToIntel(TrackerID);
                    hasInitializedTexture = false;
                    hasInitializedTracker = true;
                    if(textureChannels == 4){
                        tex = new RenderTexture(textureWidth,textureHeight,0,RenderTextureFormat.BGRA32);
                        tex.Create();
                        SetRenderTexturePointer(TrackerID, tex.GetNativeTexturePtr());
                        if(myImage != null){
                            myImage.texture = tex;
                            myImage.gameObject.SetActive(true);
                        }
                        canRenderImages = true;
                        StartCoroutine(WaitEndFrameCameraUpdate());
                    }else{
                        tex = new RenderTexture(textureWidth,textureHeight,0,RenderTextureFormat.R16);
                        tex.Create();
                        SetRenderTexturePointer(TrackerID, tex.GetNativeTexturePtr());
                        if(myImage != null){
                            myImage.texture = tex;
                            myImage.gameObject.SetActive(true);
                        }
                        canRenderImages = true;
                        StartCoroutine(WaitEndFrameCameraUpdate());                    
                    }
                    if(doesSubscribe){
                        doesSubscribe = false;
                        SubscribeCallback(TrackerID,GetImage);  
                    }
                }
            }
        }
        public override void ObtainPose(){
            if(ApplyPoses){
                IntPtr ptr = GetLatestPose(TrackerID);                 
                Marshal.Copy(ptr, currentRealsensePose, 0, 7);
                transform.position =  new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]);
                Vector3 eulerRet = new Vector3(currentRealsensePose[5],currentRealsensePose[4],currentRealsensePose[3]); 
                transform.rotation = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]); 
            }
        } 
        public override void SaveEskyMapInformation(){
            ObtainMap(TrackerID);
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
        static void OnMapCallback(int TrackerID, IntPtr receivedData, int Length)
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
                    received = System.IO.File.ReadAllBytes("temp.raw");            
                    didComplete = true;//will flag the pass is complete                    
                }catch(System.Exception e){
                    Debug.LogError(e);
                    TimeSpan ts = sw.Elapsed;
                    if(ts.TotalSeconds > 4)break;                     
                }
            }
            if(didComplete){
                UnityEngine.Debug.Log("Received map data of length: " + received.Length);
                if(instances[TrackerID] != null){
                    EskyMap retEskyMap = new EskyMap();
                    retEskyMap.mapBLOB = received;
                    instances[TrackerID].SetEskyMapInstance(retEskyMap);
                }else{
                    UnityEngine.Debug.LogError("The instance of the tracker was null, cancelling data map export");
                }
            }else{
                UnityEngine.Debug.LogError("Problem exporting the map, *shrug*");
            }
        }

        public void AddPoseFromCallback(EskyPoseCallbackData epcd){
            callbackEvents = epcd;
        }
        void OnDestroy(){
            StopTrackers(TrackerID);
        }
        public void ChangeCameraParam(float width, float height)
        {    
            float f = 35.0f;            
            float ax, ay, sizeX, sizeY;
            float x0, y0, shiftX, shiftY;
            ax = myCalibrations.fx; 
            ay = myCalibrations.fy;
            x0 = myCalibrations.cx;
            y0 = myCalibrations.cy;

            sizeX = f * width / ax;
            sizeY = f * height / ay;

            //PlayerSettings.defaultScreenWidth = width;
            //PlayerSettings.defaultScreenHeight = height;

            shiftX = -(x0 - width / 2.0f) / width; 
            shiftY = (y0 - height / 2.0f) / height;
            previewCamera.sensorSize = new Vector2(sizeX, sizeY);     // in mm, mx = 1000/x, my = 1000/y
            previewCamera.focalLength = f;                            // in mm, ax = f * mx, ay = f * my
            previewCamera.lensShift = new Vector2(shiftX, shiftY);    // W/2,H/w for (0,0), 1.0 shift in full W/H in image plane
        }

        public static float[] quat = {0.0f,0.0f,0.0f,0.0f};
        public static float[] deltaPoseLeft = {1,0,0,0,
                                     0,1,0,0, 
                                     0,0,1,0,
                                     0,0,0,1};
        public static float[] deltaPoseInvLeft = {1,0,0,0,
                                     0,1,0,0, 
                                     0,0,1,0,
                                     0,0,0,1};
        public static float[] deltaPoseRight = {1,0,0,0,
                                     0,1,0,0, 
                                     0,0,1,0,
                                     0,0,0,1};
        public static float[] deltaPoseInvRight = {1,0,0,0,
                                     0,1,0,0, 
                                     0,0,1,0,
                                     0,0,0,1};                                                                          
        public static Quaternion q = new Quaternion();
        [MonoPInvokeCallback(typeof(ConvertToQuaternionCallback))]
        public static void ConvertToQuaternion (IntPtr arrayToCopy, float eux, float euy, float euz){
            q = Quaternion.Euler(euz,euy,eux);
            quat[0] = q.x;
            quat[1] = q.y;
            quat[2] = q.z;     
            quat[3] = q.w;
            Marshal.Copy(quat,0,arrayToCopy,4);
        }
        [MonoPInvokeCallback(typeof(LocalizationPoseReceivedCallback))]
        static void OnLocalizationPoseReceivedCallback(int TrackerID, string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            EskyPoseCallbackData epcd = new EskyPoseCallbackData();
            (Vector3, Quaternion) vq = instances[TrackerID].IntelPoseToUnity(tx,ty,tz,qx,qy,qz,qw);            
            epcd.PoseID = ObjectID;
            epcd.position = vq.Item1;
            epcd.rotation = vq.Item2;
            ((EskyTrackerIntel)instances[TrackerID]).AddPoseFromCallback(epcd);
            UnityEngine.Debug.Log("Received a pose from the relocalization");
        }
        static Vector3 translateA = new Vector3();
        static Vector3 translateB = new Vector3();
        static Quaternion rotationA = Quaternion.identity;
        static Quaternion rotationB = Quaternion.identity;
        static Matrix4x4 A = new Matrix4x4();
        static Matrix4x4 B = new Matrix4x4();
        static Matrix4x4 Delta = new Matrix4x4();
        static Matrix4x4 DeltaInv = new Matrix4x4();
        static float[] deltaPoseReadbackLeft= new float[]{ 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

        static float[] deltaPoseReadbackRight= new float[]{ 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

        [MonoPInvokeCallback(typeof(DeltaMatrixConvertCallback))]
        static void DeltaMatrixConvCallback(int TrackerID, IntPtr writebackArray,bool isLeft, float tx_A, float ty_A, float tz_A, float qx_A, float qy_A, float qz_A, float qw_A, float tx_B, float ty_B, float tz_B, float qx_B, float qy_B, float qz_B, float qw_B){
            
            //set translations
            translateA.x = ty_A;            
            translateA.y = -tx_A;
            translateA.z = tz_A;
            
            translateB.x = ty_B;
            translateB.y = -tx_B;
            translateB.z = tz_B; 
            //set rotations
            rotationA.x = qy_A;
            rotationA.y = -qx_A; 
            rotationA.z = qz_A; 
            rotationA.w = qw_A;
            //
            rotationB.x = qy_B;
            rotationB.y = -qx_B; 
            rotationB.z = qz_B; 
            rotationB.w = qw_B;

            if(qw_A != 0 && qw_B != 0){
            //set matricies
                try{
                    A.SetTRS(translateA,rotationA,Vector3.one);
                    B.SetTRS(translateB,rotationB,Vector3.one);              
                    // Relove delta B -> A (final - initial)
                    if(isLeft){
                        Delta = ProjectEsky.Rendering.EskyNativeDxRenderer.leftEyeTransform.inverse * A.inverse * B * ProjectEsky.Rendering.EskyNativeDxRenderer.leftEyeTransform;
                    }else{
                        Delta = ProjectEsky.Rendering.EskyNativeDxRenderer.rightEyeTransform.inverse * A.inverse * B * ProjectEsky.Rendering.EskyNativeDxRenderer.rightEyeTransform;
                    }
                    DeltaInv = Delta.inverse;                        
                    for(int y = 0; y < 4; y++){
                        for(int x = 0; x < 4; x++){
                            if(isLeft){
                                deltaPoseReadbackLeft[y * 4 + x] = Delta[y,x];  
                            }else{
                                deltaPoseReadbackRight[y * 4 + x] = Delta[y,x];
                            }
                        }
                    }
                    if(isLeft){
                        Marshal.Copy(deltaPoseReadbackLeft,0,writebackArray,15);                   
                    }else{
                        Marshal.Copy(deltaPoseReadbackRight,0,writebackArray,15);                     
                    }
                }catch(System.Exception e){

                }
            }
        }
        [MonoPInvokeCallback(typeof(DeltaPoseUpdateCallback))]
        static void DeltaMatrixCallback(int TrackerID, IntPtr deltaPoseLeft, IntPtr deltaPoseRight){
            if( ((EskyTrackerIntel)instances[TrackerID]).attachedRenderer != null){         
                ((EskyTrackerIntel)instances[TrackerID]).attachedRenderer.SetDeltas(deltaPoseLeft,deltaPoseRight);
            }
        }

        [MonoPInvokeCallback(typeof(RenderTextureInitialized))]
        static void OnTextureInitialized(int TrackerID, int textureWidth, int textureHeight, int textureChannels,float fx,float fy, float cx, float cy, float fovx, float fovy, float aspectRatio, float d1, float d2, float d3, float d4, float d5){
            if(instances[TrackerID] != null){
                instances[TrackerID].textureWidth = textureWidth;
                instances[TrackerID].textureHeight = textureHeight;
                instances[TrackerID].textureChannels = textureChannels;
                instances[TrackerID].myCalibrations.fx = fx; 
                instances[TrackerID].myCalibrations.fy = fy;
                instances[TrackerID].myCalibrations.cx = cx;
                instances[TrackerID].myCalibrations.cy = cy;
                instances[TrackerID].myCalibrations.d1 = d1;
                instances[TrackerID].myCalibrations.d2 = d2;
                instances[TrackerID].myCalibrations.d3 = d3;
                instances[TrackerID].myCalibrations.d4 = d4;
                instances[TrackerID].d5 = d5;
                instances[TrackerID].fovx = fovx;
                instances[TrackerID].fovy = fovy;
                instances[TrackerID].hasInitializedTexture = true;                                                                
            }
        }
        public void RenderResetFlag(){
            if(hasInitializedTracker){
                PostRenderReset(TrackerID);
            }
        }
        IEnumerator WaitEndFrameCameraUpdate(){
            while(true){
                yield return new WaitForEndOfFrame();
                if(canRenderImages){
                    GL.IssuePluginEvent(GetRenderEventFunc(), TrackerID);
                }
            }
        }
        [MonoPInvokeCallback(typeof(ReceiveSensorImageCallbackWithInstanceID))]        
        public static void GetImage(int TrackerID, IntPtr info, int lengthofarray, int width, int height, int pixelCount){
        }        
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void RegisterDeltaPoseUpdate(int TrackerID, DeltaPoseUpdateCallback callback);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void HookDeviceToIntel(int TrackerID);
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern IntPtr GetLatestPose(int TrackerID);
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void InitializeTrackerObject(int TrackerID);
        
 
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void SetSerialComPort(int TrackerID, int port);

        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern void StartTrackerThread(int TrackerID, bool useLocalization);
 
        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterDebugCallback(debugCallback cb);
 
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void StopTrackers(int TrackerID);

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterObjectPoseCallback(int TrackerID, LocalizationPoseReceivedCallback poseReceivedCallback);

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterLocalizationCallback(int TrackerID, LocalizationEventCallback cb);

        [DllImport("libProjectEskyLLAPIIntel", CallingConvention = CallingConvention.Cdecl)]        
        static extern void RegisterBinaryMapCallback(int TrackerID, MapDataCallback cb);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetBinaryMapData(int TrackerID, string inputBytesLocation);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void RegisterMatrixDeltaConvCallback(int TrackerID, DeltaMatrixConvertCallback callback);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void ObtainOriginInLocalizedMap(int TrackerID);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void ObtainMap(int TrackerID);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void FlagMapImport(int TrackerID); 

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void PostRenderReset(int ID);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetTextureInitializedCallback(int TrackerID, RenderTextureInitialized callback);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetRenderTexturePointer(int TrackerID, IntPtr texPointer);
        [DllImport("libProjectEskyLLAPIIntel")]
        public static extern IntPtr GetRenderEventFunc();        
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetLeftRightEyeTransform(int iD, float[] leftEyeTransform, float[] rightEyeTransform);
           
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SubscribeCallbackImageWithID(int InstanceID, int camID,ReceiveSensorImageCallbackWithInstanceID callback);        
        public override void SubscribeCallback(int instanceID, ReceiveSensorImageCallbackWithInstanceID callbackWithInstanceID){
            SubscribeCallbackImageWithID(instanceID,myCalibrations.camID,callbackWithInstanceID);
        }
    }
}