// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Esky.LeapMotion.Input
{
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
        FPS_120 = 120,
        FPS_90 = 90,
        FPS_60 = 60,
        FPS_30 = 30
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
    /// <summary>
    /// The profile for the Leap Motion Device Manager. The settings for this profile can be viewed if the Leap Motion Device Manager input data provider is 
    /// added to the MRTK input configuration profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Profiles/Esky Device Profile", fileName = "EskyDeviceManagerProfile", order = 4)]
    [MixedRealityServiceProfile(typeof(EskyDeviceManager))]
    public class EskyDeviceManagerProfile : BaseMixedRealityProfile
    {
        [Space(10)]
        [SerializeField]
        [Tooltip("The location of the leap motion controller. LeapControllerOrientation.Headset indicates the controller is mounted on a headset. " +
            "LeapControllerOrientation.Desk indicates the controller is placed flat on desk. The default value is set to LeapControllerOrientation.Headset")]
        private EskyLeapControllerOrientation leapControllerOrientation = EskyLeapControllerOrientation.Esky;

        /// <summary>
        /// The location of the leap motion controller. LeapControllerOrientation.Headset indicates the controller is mounted on a headset. 
        /// LeapControllerOrientation.Desk indicates the controller is placed flat on desk. The default value is set to LeapControllerOrientation.Headset.
        /// </summary>
        public EskyLeapControllerOrientation LeapControllerOrientation => leapControllerOrientation;

        [SerializeField]
        [Tooltip("Adds an offset to the game object with LeapServiceProvider attached.  This offset is only applied if the leapControllerOrientation " +
        "is LeapControllerOrientation.Desk and is necessary for the hand to appear in front of the main camera. If the leap controller is on the " +
        "desk, the LeapServiceProvider is added to the scene instead of the LeapXRServiceProvider. The anchor point for the hands is the position of the " + 
        "game object with the LeapServiceProvider attached.")]
        private Vector3 leapControllerOffset = new Vector3(0, -0.2f, 0.2f);

        /// <summary>
        /// Adds an offset to the game object with LeapServiceProvider attached.  This offset is only applied if the leapControllerOrientation
        /// is LeapControllerOrientation.Desk and is necessary for the hand to appear in front of the main camera. If the leap controller is on the 
        /// desk, the LeapServiceProvider is added to the scene instead of the LeapXRServiceProvider. The anchor point for the hands is the position of the 
        /// game object with the LeapServiceProvider attached.
        /// </summary>
        public Vector3 LeapControllerOffset
        {
            get => leapControllerOffset;
            set => leapControllerOffset = value;
        }

        [SerializeField]
        [Tooltip("The distance between the index finger tip and the thumb tip required to enter the pinch/air tap selection gesture. " +
            "The pinch gesture enter will be registered for all values less than the EnterPinchDistance. The default EnterPinchDistance value is 0.02 and must be between 0.015 and 0.1. ")]
        private float enterPinchDistance = 0.02f;

        /// <summary>
        /// The distance between the index finger tip and the thumb tip required to enter the pinch/air tap selection gesture.
        /// The pinch gesture enter will be registered for all values less than the EnterPinchDistance. The default EnterPinchDistance value is 0.02 and must be between 0.015 and 0.1. 
        /// </summary>
        public float EnterPinchDistance
        {
            get => enterPinchDistance;
            set => enterPinchDistance = value;
        }

        [SerializeField]
        [Tooltip("The minimum distance between the index finger tip and the thumb tip required to exit the pinch/air tap gesture to deselect.  The distance between the thumb and  " +
            "the index tip must be greater than the ExitPinchDistance to raise the OnInputUp event")]
        private float exitPinchDistance = 0.05f;

        /// <summary>
        /// The minimum distance between the index finger tip and the thumb tip required to exit the pinch/air tap gesture to deselect.  The distance between the thumb and 
        /// the index tip must be greater than the ExitPinchDistance to raise the OnInputUp event
        /// </summary>
        public float ExitPinchDistance
        {        
            get => exitPinchDistance;
            set => exitPinchDistance = value;
        }

        // Esky specific configurations
        [SerializeField]
        [Tooltip("The rig spawned by Project Esky for use with your headset")]        
        private RigToUse rigToUse = RigToUse.NorthStarV2;
        
        [SerializeField]
        [Tooltip("If you are using a custom rig, spawn it here")]                
        private GameObject customRig = null;

        [SerializeField]
        [Tooltip("The between sensor offsets")]
        private EskySensorOffsets sensorOffsets;

        [SerializeField]
        [Tooltip("Do we use the internal camera preview!")]        
        private bool usesCameraPreview;

        [SerializeField]
        [Tooltip("The unity frame rate used")]                
        private TargetApplicationFrameRate targetFrameRate;
        
        
        [SerializeField]
        [Tooltip("Do we use temporal reprojection? (Where applicable)")]                        
        private TemporalReprojectionSettings reprojectionSettings;
        [SerializeField]
        [Tooltip("The undistortion representation")]                                
        private NativeShaderToUse nativeShaderToUse;

        [SerializeField]
        [Tooltip("Device sensor offsets")] 
        private EskySensorOffsets myOffsets;
        [SerializeField]
        [Tooltip("V2 Display calibrations")]         
        private DisplayCalibrationV2 v2CalibrationValues;
        [SerializeField]
        [Tooltip("V1 Display calibrations")]         
        private EskyV1DisplayCalibrations v1CalibrationValues;
        [SerializeField]
        [Tooltip("Display window settings")]        
        private DisplayWindowSettings displayWindowSettings;
        [SerializeField]
        [Tooltip("RGB Sensor module calibrations")]        
        private RGBSensorModuleCalibrations sensorModuleCalibrations;        
        [SerializeField]
        [Tooltip("Do we use the RGB sensor module?")]                
        private bool usesExternalRGBCamera = false;
        [SerializeField]
        [Tooltip("What pose filter system do we want?")]
        private FilterSystemToUse filterSystemToUse;


        [SerializeField]
        [Tooltip("Should we load the Rig Center to Tracker offsets?")]
        private bool useTrackerOffsets;
        public bool UseTrackerOffsets{
            get => useTrackerOffsets;
            set => useTrackerOffsets = value;
        }
        [SerializeField]
        [Tooltip("Should we dump the current settins after stopping the 'play in editor?'")]
        private bool saveAfterStoppingEditor;


        [SerializeField]
        public bool SaveAfterStoppingEditor{
            get => saveAfterStoppingEditor;
            set => saveAfterStoppingEditor = value;
        }
        [SerializeField]
        public FilterSystemToUse FilterSystemToUse{
            get => filterSystemToUse;
            set => filterSystemToUse = value;
        }
        [SerializeField]
        public RigToUse RigToUse{
            get => rigToUse;
            set => rigToUse = value;            
        }
        [SerializeField]
        public GameObject CustomRig{
            get => customRig;
            set => customRig = value;
        }
        [SerializeField]
        public EskySensorOffsets SensorOffsets{
            get => sensorOffsets;
            set => sensorOffsets = value;
        }
        [SerializeField]
        public bool UsesCameraPreview{
            get => usesCameraPreview;
            set => usesCameraPreview = value;
        }

        [SerializeField]
        public TargetApplicationFrameRate TargetFrameRate{
            get => targetFrameRate;
            set => targetFrameRate = value;
        }
        
        
        [SerializeField]
        public TemporalReprojectionSettings ReprojectionSettings{
            get => reprojectionSettings;
            set => reprojectionSettings = value;
        }
        
        [SerializeField]
        public NativeShaderToUse NativeShaderToUse{
            get => nativeShaderToUse;
            set => nativeShaderToUse = value;
        }

        [SerializeField]
        public DisplayCalibrationV2 V2CalibrationValues{
            get => v2CalibrationValues;
            set => v2CalibrationValues = value;
        }


        [SerializeField]
        public EskyV1DisplayCalibrations V1CalibrationValues{
            get => v1CalibrationValues;
            set => v1CalibrationValues = value;
        }
        [SerializeField]
        public DisplayWindowSettings DisplayWindowSettings{
            get => displayWindowSettings;
            set => displayWindowSettings = value;
        }
        [SerializeField]
        public RGBSensorModuleCalibrations SensorModuleCalibrations{
            get => sensorModuleCalibrations;
            set => sensorModuleCalibrations = value;
        }
        [SerializeField]
        public bool UsesExternalRGBCamera{
            get => usesExternalRGBCamera;
            set => usesExternalRGBCamera = value;
        }

    }

}

