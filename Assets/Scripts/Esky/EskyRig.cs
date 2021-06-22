using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public Vector3 TranslationFromTracker;

        [SerializeField]
        public Quaternion RotationFromTracker;
        [SerializeField]
        public Vector3 RGBSensorTranslationFromTracker;
        [SerializeField]
        public Vector3 RGBSensorRotationFromTracker;
    }
    [System.Serializable]
    public enum V2ShaderToUse{
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
        public V2ShaderToUse v2ShaderToUse;

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
    }
    public class EskyRig : MonoBehaviour
    {
        public static EskyRig instance;
        public Transform LeapMotionController;
        public Transform RGBSensor;
        public Transform NetworkingObject;
        public GameObject Rig;
        public EskySettings LoadedSettings;
        // Start is called before the first frame update
        void Start(){
            instance = this;
        }
        public void ReceiveConfig(string config){
            LoadedSettings = JsonUtility.FromJson<EskySettings>(config);
            Debug.Log(config);
            Rig.SetActive(true);
        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}