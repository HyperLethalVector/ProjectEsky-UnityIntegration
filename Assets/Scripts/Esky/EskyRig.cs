using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;
using static Leap.Unity.AR.OpticalCalibrationManager;

namespace BEERLabs.ProjectEsky.Configurations{

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
    [System.Serializable]
    public struct ReflectorOptics {
      [SerializeField]      
      public float ellipseMinorAxis;
      [SerializeField]
      public float ellipseMajorAxis;
      [SerializeField]
      public Vector3 screenForward;
      [SerializeField]
      public Vector3 screenPosition;
      [SerializeField]
      public Vector3 eyePosition;
      [SerializeField]
      public Quaternion eyeRotation;
      public Vector4 cameraProjection;
      [HideInInspector]      
      public Matrix4x4 sphereToWorldSpace;
      [HideInInspector]      
      public Matrix4x4 worldToScreenSpace;
    }
    [System.Serializable]
    public struct EskyV1DisplayCalibrations {
      [SerializeField]
      public ReflectorOptics leftEye;
      [SerializeField]
      public ReflectorOptics rightEye;
    }
    [System.Serializable]
    public class DisplayWindowSettings{
        public int DisplayXLoc;
        public int DisplayYLoc;
        public int DisplayWidth;
        public int DisplayHeight;
        public int EyeTextureWidth;
        public int EyeTextureHeight;        
    }

    [System.Serializable]
    public class DisplayCalibrationV2{
        [SerializeField]         
        public float[] left_uv_to_rect_x ;
        [SerializeField]
        public float[] left_uv_to_rect_y ;
        [SerializeField]
        public float[] right_uv_to_rect_x;
        [SerializeField]
        public float[] right_uv_to_rect_y;
        [SerializeField]
        public float[] left_eye_offset;
        [SerializeField]
        public float[] right_eye_offset;
    }
    [System.Serializable]
    public class EskySensorOffsets{
        [SerializeField]
        public Vector3 TranslationEyeToLeapMotion;
        [SerializeField]
        public Quaternion RotationEyeToLeapMotion;
        [SerializeField]
        public Vector3 TranslationFromTracker;

        [SerializeField]
        public Quaternion RotationFromTracker;
        [SerializeField]
        public Vector3 RGBSensorTranslationFromTracker;
        [SerializeField]
        public Quaternion RGBSensorRotationFromTracker;
    }
    [System.Serializable]
    public enum NativeShaderToUse{
        NoUndistortion,
        V2Undistortion,
        LookUpTextureUndistortion
    }
    [System.Serializable]
    public enum FilterSystemToUse{
        NEW,
        OLD        
    }
    [System.Serializable]
    public enum RigToUse{
        NorthStarV2,
        NorthStarV1,
        Ariel,
        Custom        
    }
    [System.Serializable]
    public enum TrackingFilterToUse{
        New,
        Old
    }
    [System.Serializable]
    public enum TemporalReprojectionSettings{
        Enabled,
        Disabled
    }
    [System.Serializable]
    public enum TargetApplicationFrameRate{
        FPS_120,
        FPS_90,
        FPS_60,
        FPS_30
    }
    [System.Serializable] 
    public class EskySettings{

        [SerializeField]
        public RigToUse rigToUse;
        [SerializeField]
        public TargetApplicationFrameRate targetFrameRate;
        [SerializeField]
        public TemporalReprojectionSettings reprojectionSettings;
        [SerializeField]
        public NativeShaderToUse nativeShaderToUse;

        [SerializeField]
        public EskySensorOffsets myOffsets;
        [SerializeField]
        public DisplayCalibrationV2 v2CalibrationValues;
        [SerializeField]
        public EskyV1DisplayCalibrations v1CalibrationValues;
        [SerializeField]
        public DisplayWindowSettings displayWindowSettings;
        [SerializeField]
        public RGBSensorModuleCalibrations sensorModuleCalibrations;        
        [SerializeField]
        public bool usesExternalRGBCamera = false;
        [SerializeField]
        public bool UsesCameraPreview;
        [SerializeField]
        public bool UseTrackerOffsets;
    }
    public class EskyRig : MonoBehaviour
    {
        public Leap.Unity.AR.WindowOffsetManager v1WindowManager;
        public Leap.Unity.AR.OpticalCalibrationManager v1Renderer;
        public BEERLabs.ProjectEsky.Rendering.EskyNativeDxRenderer nativeDirectXrenderer;
        public BEERLabs.ProjectEsky.Extras.Modules.EskyRGBSensorModule RGBSensorModule;

        public static EskyRig instance;
        public Transform LeapMotionController;
        public Transform RigCenter;
        public Transform RGBSensor;
        public Transform SensorPreview;
        public Transform NetworkingObject;
        public GameObject Rig;
        public EskySettings LoadedSettings;
        bool dumpSettings = true;
        bool didLoadSettings = false;
        // Start is called before the first frame update
        void Start(){
            instance = this;
        }
        public void DumpSettingsConfig(bool shouldDumpSettings){
            dumpSettings = shouldDumpSettings;
        }
        public void SaveSettings(){
            LoadedSettings.myOffsets.TranslationEyeToLeapMotion = LeapMotionController.localPosition;
            LoadedSettings.myOffsets.RotationEyeToLeapMotion = LeapMotionController.localRotation;            
            Debug.Log("Saved Esky rig settings!");
        }
        public void ReceiveConfig(string config){
            didLoadSettings = true;
            LoadedSettings = JsonUtility.FromJson<EskySettings>(config);
            LeapMotionController.localPosition = LoadedSettings.myOffsets.TranslationEyeToLeapMotion;
            LeapMotionController.localRotation = LoadedSettings.myOffsets.RotationEyeToLeapMotion;
  //          GameObject gg = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/HandModels") as GameObject);
//            gg.GetComponent<HandModelManager>().leapProvider = LeapMotionController.GetComponent<LeapXRServiceProvider>();
           // if(LoadedSettings.UseTrackerOffsets){
           //     RigCenter.localPosition = LoadedSettings.myOffsets.TranslationFromTracker;
          //      RigCenter.localRotation = LoadedSettings.myOffsets.RotationFromTracker;
           // }

            if(nativeDirectXrenderer != null){
                nativeDirectXrenderer.displaySettings.DisplayWidth = LoadedSettings.displayWindowSettings.DisplayWidth;
                nativeDirectXrenderer.displaySettings.DisplayHeight = LoadedSettings.displayWindowSettings.DisplayHeight;
                nativeDirectXrenderer.displaySettings.DisplayXLoc = LoadedSettings.displayWindowSettings.DisplayXLoc;
                nativeDirectXrenderer.displaySettings.DisplayYLoc = LoadedSettings.displayWindowSettings.DisplayYLoc;
                nativeDirectXrenderer.displaySettings.EyeTextureHeight = LoadedSettings.displayWindowSettings.EyeTextureHeight;
                nativeDirectXrenderer.displaySettings.EyeTextureWidth = LoadedSettings.displayWindowSettings.EyeTextureWidth;
                nativeDirectXrenderer.displaySettings.RendererWindowID = 0;              
                nativeDirectXrenderer.calibration.left_uv_to_rect_x = LoadedSettings.v2CalibrationValues.left_uv_to_rect_x;  
                nativeDirectXrenderer.calibration.left_uv_to_rect_y = LoadedSettings.v2CalibrationValues.left_uv_to_rect_y;  
                nativeDirectXrenderer.calibration.right_uv_to_rect_x = LoadedSettings.v2CalibrationValues.right_uv_to_rect_x;
                nativeDirectXrenderer.calibration.right_uv_to_rect_y = LoadedSettings.v2CalibrationValues.right_uv_to_rect_y;                                
                nativeDirectXrenderer.calibration.left_eye_offset = LoadedSettings.v2CalibrationValues.left_eye_offset;
                nativeDirectXrenderer.calibration.right_eye_offset = LoadedSettings.v2CalibrationValues.right_eye_offset;
            }
            
            if(LoadedSettings.UsesCameraPreview){
                if(SensorPreview != null){
                    SensorPreview.gameObject.SetActive(true);
                }
            }

            if(LoadedSettings.usesExternalRGBCamera){
                if(RGBSensor != null){
                    RGBSensor.gameObject.SetActive(true);
                    RGBSensor.localPosition = LoadedSettings.myOffsets.RGBSensorTranslationFromTracker;
                    RGBSensor.localRotation = LoadedSettings.myOffsets.RGBSensorRotationFromTracker;
                    ProjectEsky.RGBSensorModuleCalibrations rsmc = new ProjectEsky.RGBSensorModuleCalibrations();
                    rsmc.camID = LoadedSettings.sensorModuleCalibrations.camID;                
                    rsmc.cx = LoadedSettings.sensorModuleCalibrations.camID;
                    rsmc.cy = LoadedSettings.sensorModuleCalibrations.camID;
                    rsmc.fx = LoadedSettings.sensorModuleCalibrations.camID;
                    rsmc.fy = LoadedSettings.sensorModuleCalibrations.camID;                
                    rsmc.d1 = LoadedSettings.sensorModuleCalibrations.d1;
                    rsmc.d2 = LoadedSettings.sensorModuleCalibrations.d2;
                    rsmc.d3 = LoadedSettings.sensorModuleCalibrations.d3;
                    rsmc.d4 = LoadedSettings.sensorModuleCalibrations.d4;                
                    rsmc.SensorChannels = LoadedSettings.sensorModuleCalibrations.SensorChannels;
                    rsmc.SensorFoV = LoadedSettings.sensorModuleCalibrations.SensorFoV;
                    rsmc.SensorWidth = LoadedSettings.sensorModuleCalibrations.SensorWidth;
                    rsmc.SensorHeight = LoadedSettings.sensorModuleCalibrations.SensorHeight;
                    RGBSensor.GetComponent<BEERLabs.ProjectEsky.Extras.Modules.EskyRGBSensorModule>().myCalibrations = rsmc;
                }
            }
            Rig.SetActive(true);
            if(v1Renderer != null){
                HeadsetCalibration hc = new HeadsetCalibration();
                hc.leftEye.cameraProjection = LoadedSettings.v1CalibrationValues.leftEye.cameraProjection;// = LoadedSettings.v1CalibrationValues.leftEye;
                hc.leftEye.ellipseMajorAxis = LoadedSettings.v1CalibrationValues.leftEye.ellipseMajorAxis;
                hc.leftEye.ellipseMinorAxis = LoadedSettings.v1CalibrationValues.leftEye.ellipseMinorAxis;     
                hc.leftEye.eyePosition = LoadedSettings.v1CalibrationValues.leftEye.eyePosition;     
                hc.leftEye.eyeRotation = LoadedSettings.v1CalibrationValues.leftEye.eyeRotation;     
                hc.leftEye.screenForward = LoadedSettings.v1CalibrationValues.leftEye.screenForward; 
                hc.leftEye.screenPosition = LoadedSettings.v1CalibrationValues.leftEye.screenPosition;
                hc.leftEye.sphereToWorldSpace = LoadedSettings.v1CalibrationValues.leftEye.sphereToWorldSpace;
                hc.leftEye.worldToScreenSpace = LoadedSettings.v1CalibrationValues.leftEye.worldToScreenSpace;
                //right eye
                hc.rightEye.cameraProjection = LoadedSettings.v1CalibrationValues.rightEye.cameraProjection;// = LoadedSettings.v1CalibrationValues.rightEye;
                hc.rightEye.ellipseMajorAxis = LoadedSettings.v1CalibrationValues.rightEye.ellipseMajorAxis;
                hc.rightEye.ellipseMinorAxis = LoadedSettings.v1CalibrationValues.rightEye.ellipseMinorAxis;     
                hc.rightEye.eyePosition = LoadedSettings.v1CalibrationValues.rightEye.eyePosition;     
                hc.rightEye.eyeRotation = LoadedSettings.v1CalibrationValues.rightEye.eyeRotation;     
                hc.rightEye.screenForward = LoadedSettings.v1CalibrationValues.rightEye.screenForward; 
                hc.rightEye.screenPosition = LoadedSettings.v1CalibrationValues.rightEye.screenPosition;
                hc.rightEye.sphereToWorldSpace = LoadedSettings.v1CalibrationValues.rightEye.sphereToWorldSpace;
                hc.rightEye.worldToScreenSpace = LoadedSettings.v1CalibrationValues.rightEye.worldToScreenSpace;
                v1Renderer.TryLoadCalibrationFromEsky(hc,false);                
            }
            if(v1WindowManager != null){
                Leap.Unity.AR.WindowOffsetManager.SetPosition(LoadedSettings.displayWindowSettings.DisplayXLoc,LoadedSettings.displayWindowSettings.DisplayYLoc,LoadedSettings.displayWindowSettings.DisplayWidth,LoadedSettings.displayWindowSettings.DisplayHeight);
            }
        }
        public void OnDestroy(){
            if(dumpSettings){
                Debug.Log("Saving Settings");
                string message = JsonUtility.ToJson(LoadedSettings,true);
                System.IO.File.WriteAllText("EskySettings.json",message);
            }
        }
    }
    
}