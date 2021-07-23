using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BEERLabs.ProjectEsky.Tracking{

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
    [System.Serializable]
    public class DollaryDooFilterParams{
        
        [SerializeField]

        public double Frequency;
        [SerializeField,Range(0.01f,10f)]
        public double MinimumCutoff;
        [SerializeField, Range(0.1f,100f) ]        
        public double Beta;
        [SerializeField, Range(0f,10f)]        
        public double DCutoff;
        [SerializeField]
        public bool Enabled = true;
        double _freq = 20.0;
        double _mincutoff = 1.0;//double _freq, double _mincutoff = 1.0, double _beta = 0.0, double _dcutoff = 1.0
        double _beta = 0.0;
        double _dcutoff = 1.0;
        bool _enabled = false;
        public bool CheckUpdate(){
            
            bool didChange = false;
            if(Frequency!= _freq){_freq = Frequency; didChange = true;}
            if(MinimumCutoff != _mincutoff){_mincutoff = MinimumCutoff; didChange = true;}
            if(Beta != _beta){_beta = Beta; didChange = true;}            
            if(DCutoff != _dcutoff){_dcutoff = DCutoff; didChange = true;}        
            if(Enabled != _enabled){_enabled = Enabled; didChange = true;}
            return didChange;    
        }        

    }
    [System.Serializable]
    public class KalmanFilterParams{
        [SerializeField,Range(0.001f,1)]
        public double Q;
        [SerializeField, Range(0.001f,10f) ]        
        public double R;

        double _q= 1.0;
        double _r = 0.0;
        bool _enabled = false;
        public bool Enabled = true;
        public bool CheckUpdate(){
            
            bool didChange = false;
            if(Q!= _q){_q = Q; didChange = true;}
            if(R != _r){_r = R; didChange = true;}
            if(Enabled != _enabled){_enabled = Enabled; didChange = true;}
            return didChange;    
        }        
    }
    [System.Serializable]
    public enum TrackingSystemUsed{
        OLD = 0,
        NEW = 1
    }
    [System.Serializable]
    public class OldTrackingSystemFilters{
        [SerializeField]

        public bool FilterEnabled = true;
        [SerializeField]        
        public DollaryDooFilterParams TranslationFilterParameters;
        [SerializeField]        
        public DollaryDooFilterParams RotationFilterParameters;        
        [SerializeField]        
        public KalmanFilterParams KTranslationFilterParams; 
        [SerializeField]        
        public KalmanFilterParams KRrotationFilterParams;
        public bool UseDollaryDooFilter = true;
        public bool UseKalmanFilter = true;
    }
    
    [System.Serializable]
    public class NewDollaryDooFilters{
        [SerializeField]        
        public DollaryDooFilterParams TranslationFilterParams; 
        [SerializeField]        
        public DollaryDooFilterParams RotationFilterParams;

        [SerializeField]        
        public DollaryDooFilterParams VelocityFilterParams;
        [SerializeField]        
        public DollaryDooFilterParams AccelerationFilterParams;

        [SerializeField]        
        public DollaryDooFilterParams RotationVelocityFilterParams;
        [SerializeField]        
        public DollaryDooFilterParams RotationAccelerationFilterParams;   
        public bool HadUpdatedValues(){
            return TranslationFilterParams.CheckUpdate() || RotationFilterParams.CheckUpdate() || VelocityFilterParams.CheckUpdate() || AccelerationFilterParams.CheckUpdate() || RotationVelocityFilterParams.CheckUpdate() || RotationAccelerationFilterParams.CheckUpdate();
        }
    }

    [System.Serializable]
    public class NewKalmanFilters{
        [SerializeField]        
        public KalmanFilterParams TranslationFilterParams; 
        [SerializeField]        
        public KalmanFilterParams RotationFilterParams;

        [SerializeField]        
        public KalmanFilterParams VelocityFilterParams;
        [SerializeField]        
        public KalmanFilterParams AccelerationFilterParams;

        [SerializeField]        
        public KalmanFilterParams RotationVelocityFilterParams;
        [SerializeField]        
        public KalmanFilterParams RotationAccelerationFilterParams;
        public bool HadUpdatedValues(){
            return TranslationFilterParams.CheckUpdate() || RotationFilterParams.CheckUpdate() || VelocityFilterParams.CheckUpdate() || AccelerationFilterParams.CheckUpdate() || RotationVelocityFilterParams.CheckUpdate() || RotationAccelerationFilterParams.CheckUpdate();
        }        
    }
    [System.Serializable]
    public class NewTrackingSystemFilters{
        [SerializeField]
        public NewDollaryDooFilters dollaryDooFilterParams;
        [SerializeField]        
        public NewKalmanFilters kalmanFilterParameters;
        [SerializeField]
        public bool slamDFilterEnabled = true;
        [SerializeField]
        public bool slamKFilterEnabled = true;
        [SerializeField]
        public bool DoUpdateValues = false;
        bool hasUpdatedValuesInitially = false;
        public bool ShouldUpdateValues(){
            if(dollaryDooFilterParams.HadUpdatedValues() || kalmanFilterParameters.HadUpdatedValues()){
                return true;
            }
            if(!hasUpdatedValuesInitially){
                hasUpdatedValuesInitially = true;
                return true;
            }
            if(DoUpdateValues){
                DoUpdateValues = false;
                return true;
            }
            return false;
        }
    }
    [System.Serializable]
    public class TrackingSystemFilters{
        [SerializeField]
        public OldTrackingSystemFilters oldTrackingSystemFilters;
        [SerializeField]        
        public NewTrackingSystemFilters newTrackingSystemFilters;
        public TrackingSystemUsed trackingSystemUsed;
    }
    [System.Serializable]
    public class HeadPosePredictionSettings{
        [Range(0,1000),SerializeField]
        public float HeadPosePredictionOffset = 0f;
    }
    public class EskyTrackerIntel : EskyTracker
    {
        public static EskyTrackerIntel instance;
        float currentHeadPosePredict = -1;
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
        public BEERLabs.ProjectEsky.Rendering.EskyNativeDxRenderer attachedRenderer;

        public HeadPosePredictionSettings HeadPosePredictionOffsets;
        public TrackingSystemFilters trackingSystemFilters;
        bool setParametersFilterFirstTime = false;
        public bool UseAsyncPosePredictor = false;
        
        bool _useDollaryDooFilter = false;
        bool _useKalmanFilter = false;
        bool _filterEnabled;        
        public override void AfterAwake()
        {
            instance = this;
            GrayscaleImageSource = this;
            RegisterDebugCallback(OnDebugCallback);    
            LoadCalibration();
            Debug.Log("Initializing track");
            InitializeTrackerObject(TrackerID);       
            Debug.Log("Done initializing tracker object");            
//            UseAsyncHeadPosePredictor(TrackerID,UseAsyncPosePredictor);            
            RegisterBinaryMapCallback(TrackerID,OnMapCallback);
            RegisterObjectPoseCallback(TrackerID, OnLocalizationPoseReceivedCallback);
            if(UsesDeckXIntegrator){
                SetSerialComPort(TrackerID, (int)comPort);
            }
            if(attachedRenderer != null)
            RegisterDeltaPoseUpdate(TrackerID, DeltaMatrixCallback);
            RegisterLocalizationCallback(TrackerID, OnLocalization);            
            EnablePassthrough(TrackerID,UseExternalCameraPreview);
            StartTrackerThread(TrackerID, false);    
            AfterInitialization();     
            SetTextureInitializedCallback(TrackerID, OnTextureInitialized);     
            UpdateNewFilterValues();            
        }
        public override void AfterStart()
        {
            if(attachedRenderer != null){
                for(int i = 0; i < 16; i++){
                        leftEyeTransform[i] = BEERLabs.ProjectEsky.Rendering.EskyNativeDxRenderer.leftEyeTransform[i];
                        rightEyeTransform[i] = BEERLabs.ProjectEsky.Rendering.EskyNativeDxRenderer.rightEyeTransform[i];                                                
                }
                SetLeftRightEyeTransform(TrackerID,leftEyeTransform,rightEyeTransform);
            }
            UpdateFilterTranslationParams(TrackerID,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.Frequency,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.MinimumCutoff,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.Beta,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.DCutoff);
            UpdateFilterRotationParams(TrackerID, trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.Frequency,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.MinimumCutoff,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.Beta,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.DCutoff);            
            if(_filterEnabled != trackingSystemFilters.oldTrackingSystemFilters.FilterEnabled){_filterEnabled = trackingSystemFilters.oldTrackingSystemFilters.FilterEnabled; SetFilterEnabled(TrackerID,_filterEnabled); Debug.Log("Setting Filtered Enabled: " + _filterEnabled);} 
            if(trackingSystemFilters.trackingSystemUsed == TrackingSystemUsed.NEW){
                UseNewTrackingSystemForParams(TrackerID);
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
            else{
                hasInitializedTracker = true;
            }
            switch(trackingSystemFilters.trackingSystemUsed){
                case TrackingSystemUsed.NEW:
                #region setting new tracking params
                if(trackingSystemFilters.newTrackingSystemFilters.ShouldUpdateValues()){
                    UpdateNewFilterValues();

                }
                #endregion
                break;
                case TrackingSystemUsed.OLD:
                #region setting old tracking params
                    if(trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.CheckUpdate() || !setParametersFilterFirstTime){
                        UpdateFilterTranslationParams(TrackerID,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.Frequency,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.MinimumCutoff,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.Beta,trackingSystemFilters.oldTrackingSystemFilters.TranslationFilterParameters.DCutoff);
                    }
                    if(trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.CheckUpdate() || !setParametersFilterFirstTime){
                        UpdateFilterRotationParams(TrackerID,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.Frequency,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.MinimumCutoff,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.Beta,trackingSystemFilters.oldTrackingSystemFilters.RotationFilterParameters.DCutoff);
                    } 
                    if(trackingSystemFilters.oldTrackingSystemFilters.KTranslationFilterParams.CheckUpdate() || !setParametersFilterFirstTime){
                        UpdateKFilterTranslationParams(TrackerID,trackingSystemFilters.oldTrackingSystemFilters.KTranslationFilterParams.Q,trackingSystemFilters.oldTrackingSystemFilters.KTranslationFilterParams.R);
                    }
                    if(trackingSystemFilters.oldTrackingSystemFilters.KRrotationFilterParams.CheckUpdate() || !setParametersFilterFirstTime){
                        UpdateKFilterRotationParams(TrackerID,trackingSystemFilters.oldTrackingSystemFilters.KRrotationFilterParams.Q,trackingSystemFilters.oldTrackingSystemFilters.KRrotationFilterParams.R);
                    }            
                    if(trackingSystemFilters.oldTrackingSystemFilters.UseKalmanFilter != _useKalmanFilter){
                        _useKalmanFilter = trackingSystemFilters.oldTrackingSystemFilters.UseKalmanFilter;
                        SetKFilterEnabled(TrackerID,_useKalmanFilter);
                    }
                    if(trackingSystemFilters.oldTrackingSystemFilters.UseDollaryDooFilter != _useDollaryDooFilter){
                        _useDollaryDooFilter = trackingSystemFilters.oldTrackingSystemFilters.UseDollaryDooFilter;
                        SetFilterEnabled(TrackerID,_useDollaryDooFilter);
                    }
                    if(_filterEnabled != trackingSystemFilters.oldTrackingSystemFilters.FilterEnabled){_filterEnabled = trackingSystemFilters.oldTrackingSystemFilters.FilterEnabled; SetFilterEnabled(TrackerID,_filterEnabled); Debug.Log("Setting Filtered Enabled: " + _filterEnabled);} 
                    if(!setParametersFilterFirstTime)setParametersFilterFirstTime = true;
                    if(currentHeadPosePredict != HeadPosePredictionOffsets.HeadPosePredictionOffset){
                        currentHeadPosePredict = HeadPosePredictionOffsets.HeadPosePredictionOffset;
                        SetTimeOffset(TrackerID,currentHeadPosePredict);
                    }
                #endregion
                break;
            }
        }
        void UpdateNewFilterValues(){
                    NewTrackingSystemFilters newTrackingSystemFilters = trackingSystemFilters.newTrackingSystemFilters;
                    NewDollaryDooFilters dollaryDooFilters = trackingSystemFilters.newTrackingSystemFilters.dollaryDooFilterParams;
                    NewKalmanFilters kalmanFilters = trackingSystemFilters.newTrackingSystemFilters.kalmanFilterParameters;
                    
                    //update dollarydoo filter parameters
                    UpdateTransFilterDollaryDooParams(TrackerID,
                    dollaryDooFilters.TranslationFilterParams.Frequency,dollaryDooFilters.TranslationFilterParams.MinimumCutoff,dollaryDooFilters.TranslationFilterParams.Beta, dollaryDooFilters.TranslationFilterParams.DCutoff,
                    dollaryDooFilters.VelocityFilterParams.Frequency,dollaryDooFilters.VelocityFilterParams.MinimumCutoff,dollaryDooFilters.VelocityFilterParams.Beta, dollaryDooFilters.VelocityFilterParams.DCutoff,
                    dollaryDooFilters.AccelerationFilterParams.Frequency,dollaryDooFilters.AccelerationFilterParams.MinimumCutoff,dollaryDooFilters.AccelerationFilterParams.Beta, dollaryDooFilters.AccelerationFilterParams.DCutoff);

                    UpdateRotFilterDollaryDooParams(TrackerID,
                    dollaryDooFilters.RotationFilterParams.Frequency,dollaryDooFilters.RotationFilterParams.MinimumCutoff,dollaryDooFilters.RotationFilterParams.Beta, dollaryDooFilters.RotationFilterParams.DCutoff,
                    dollaryDooFilters.RotationVelocityFilterParams.Frequency,dollaryDooFilters.RotationVelocityFilterParams.MinimumCutoff,dollaryDooFilters.RotationVelocityFilterParams.Beta, dollaryDooFilters.RotationVelocityFilterParams.DCutoff,
                    dollaryDooFilters.RotationAccelerationFilterParams.Frequency,dollaryDooFilters.RotationAccelerationFilterParams.MinimumCutoff,dollaryDooFilters.RotationAccelerationFilterParams.Beta, dollaryDooFilters.RotationAccelerationFilterParams.DCutoff);
                    
                    //update kalman filter paramters
                    UpdateTransFilterKParams(TrackerID,
                        kalmanFilters.TranslationFilterParams.Q,kalmanFilters.TranslationFilterParams.R,
                        kalmanFilters.VelocityFilterParams.Q,kalmanFilters.VelocityFilterParams.R,
                        kalmanFilters.AccelerationFilterParams.Q,kalmanFilters.AccelerationFilterParams.R                    
                    );

                    UpdateRotFilterKParams(TrackerID,
                        kalmanFilters.RotationFilterParams.Q,kalmanFilters.RotationFilterParams.R,
                        kalmanFilters.RotationVelocityFilterParams.Q,kalmanFilters.RotationVelocityFilterParams.R,
                        kalmanFilters.RotationAccelerationFilterParams.Q,kalmanFilters.RotationAccelerationFilterParams.R
                    );

                    //enable/disable any dollarydoo filters
                    SetFilterEnabledExt(TrackerID,
                    newTrackingSystemFilters.slamDFilterEnabled,                    
                    dollaryDooFilters.TranslationFilterParams.Enabled,
                    dollaryDooFilters.AccelerationFilterParams.Enabled,
                    dollaryDooFilters.RotationVelocityFilterParams.Enabled,
                    dollaryDooFilters.RotationAccelerationFilterParams.Enabled);
                    
                    //enable/disable any kalman filters
                    SetKFilterEnabledExt(TrackerID,newTrackingSystemFilters.slamKFilterEnabled,
                    kalmanFilters.TranslationFilterParams.Enabled,
                    kalmanFilters.AccelerationFilterParams.Enabled,
                    kalmanFilters.RotationVelocityFilterParams.Enabled,
                    kalmanFilters.RotationAccelerationFilterParams.Enabled
                    );
        }
        public override void ObtainPose(){
            if(ApplyPoses){
                switch(trackingSystemFilters.trackingSystemUsed){
                    case TrackingSystemUsed.NEW:
                        IntPtr ptr = GetLatestTimestampPose(TrackerID);                 
                        Marshal.Copy(ptr, currentRealsensePoseExt, 0, 7);
                        transform.position =  new Vector3((float)currentRealsensePoseExt[0],(float)currentRealsensePoseExt[1],-(float)currentRealsensePoseExt[2]);
                        transform.rotation = new Quaternion(-(float)currentRealsensePoseExt[3],-(float)currentRealsensePoseExt[4],(float)currentRealsensePoseExt[5],(float)currentRealsensePoseExt[6]);                     
                        break;
                    case TrackingSystemUsed.OLD:
                        IntPtr ptr2 = GetLatestPose(TrackerID);                 
                        Marshal.Copy(ptr2, currentRealsensePose, 0, 7);
                        transform.position =  new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]);
                        transform.rotation = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]); 
                        break;
                }
            }
        } 
        public override void SaveEskyMapInformation(){
            ObtainMap(TrackerID);
        }
        static bool hasNotifiedDeviceNotConnected = false;
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
            if(debug_string.Contains("No device connected")){
                if(hasNotifiedDeviceNotConnected){
                    return;
                }else{
                    hasNotifiedDeviceNotConnected = true;
                }
            }
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
            float f = 70;            
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
            instance.SendImageData(info,lengthofarray,width,height,pixelCount);
//            Debug.Log("Getting Image");
        }        
        [DllImport("libProjectEskyLLAPIIntel")] 
        static extern void RegisterDeltaPoseUpdate(int TrackerID, DeltaPoseUpdateCallback callback);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void HookDeviceToIntel(int TrackerID);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetFilterEnabled(int iD, bool value); 
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateFilterTranslationParams(int iD, double _freq, double _mincutoff, double _beta, double _dcutoff);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateFilterRotationParams(int iD, double _freq, double _mincutoff, double _beta, double _dcutoff);        
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
        static extern void EnablePassthrough(int iD, bool enabled);

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
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetTimeOffset(int Id, float value);
        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UseAsyncHeadPosePredictor(int ID, bool val);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern IntPtr GetLatestTimestampPose(int ID);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateKFilterTranslationParams(int iD, double _q, double _r);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateKFilterRotationParams(int iD, double _q, double _r);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetKFilterEnabled(int ID, bool value);
        

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetFilterEnabledExt(int iD, bool slam, bool velocity, bool accel, bool angvelocity, bool angaccel);        

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void SetKFilterEnabledExt(int iD, bool slam, bool velocity, bool accel, bool angvelocity, bool angaccel);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateTransFilterDollaryDooParams(int iD, double _transfreq, double _transmincutoff, double _transbeta, double _transdcutoff,
        double _velfreq, double _velmincutoff, double _velbeta, double _veldcutoff,
        double _accelfreq, double _accelmincutoff, double _accelbeta, double _acceldcutoff);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateRotFilterDollaryDooParams(int iD, double _rotfreq, double _rotmincutoff, double _rotbeta, double _rotdcutoff,
        double _velfreq, double _velmincutoff, double _velbeta, double _veldcutoff,
        double _accelfreq, double _accelmincutoff, double _accelbeta, double _acceldcutoff);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateTransFilterKParams(int iD, 
        double _transq, double _transr,
        double _velq, double _velr,
        double _accelq, double _accelr);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UpdateRotFilterKParams(int iD, 
        double _rotq, double _rotr,
        double _angvelq, double _angvelr,
        double _angaccelq, double _angaccelr);

        [DllImport("libProjectEskyLLAPIIntel")]
        static extern void UseNewTrackingSystemForParams(int iD);
    }
}