﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System.Collections.Generic;
using Unity.Profiling;
using System;

#if LEAPMOTIONCORE_PRESENT
using Leap;
using Leap.Unity;
using Leap.Unity.Attachments;
#endif

namespace Microsoft.MixedReality.Toolkit.Esky.LeapMotion.Input
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.WindowsStandalone | SupportedPlatforms.WindowsEditor,
        "Esky Device Manager",
        "Esky/Profiles/EskyDeviceManagerProfile.asset",
        "MixedRealityToolkit.Providers",
        true)]
    /// <summary>
    /// Class that detects the tracking state of leap motion hands.  This class will only run if the Leap Motion Core Assets are in the project and the Leap Motion Device
    /// Manager data provider has been added in the input system configuration profile.
    /// </summary>
    public class EskyDeviceManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public EskyDeviceManager(
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(inputSystem, name, priority, profile) { }


#region IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            // Leap Motion only supports Articulated Hands
            return (capability == MixedRealityCapability.ArticulatedHand);
        }


#endregion IMixedRealityCapabilityCheck Implementation
#if LEAPMOTIONCORE_PRESENT
        /// <summary>
        /// The profile that contains settings for the Leap Motion Device Manager input data provider.  This profile is nested under 
        /// Input > Input Data Providers > Leap Motion Device Manager in the MixedRealityToolkit object in the hierarchy.
        /// </summary>
        public EskyDeviceManagerProfile SettingsProfile => ConfigurationProfile as EskyDeviceManagerProfile;

        /// <summary>
        /// The LeapServiceProvider is added to the scene at runtime in OnEnable. 
        /// </summary>
        public LeapServiceProvider LeapMotionServiceProvider { get; protected set; }

        /// <summary>
        /// The distance between the index finger tip and the thumb tip required to enter the pinch/air tap selection gesture.
        /// The pinch gesture enter will be registered for all values less than the EnterPinchDistance. The default EnterPinchDistance value is 0.02 and must be between 0.015 and 0.1. 
        /// </summary>
        private float enterPinchDistance => SettingsProfile.EnterPinchDistance;

        public RigToUse rigToUse => SettingsProfile.RigToUse;
        public GameObject customRig => SettingsProfile.CustomRig;
        /// <summary>
        /// The distance between the index finger tip and the thumb tip required to exit the pinch/air tap gesture.
        /// The pinch gesture exit will be registered for all values greater than the ExitPinchDistance. The default ExitPinchDistance value is 0.05 and must be between 0.015 and 0.1. 
        /// </summary>
        private float exitPinchDistance => SettingsProfile.ExitPinchDistance;

        /// <summary>
        /// If true, the leap motion controller is connected and detected.
        /// </summary>
        private bool IsLeapConnected => LeapMotionServiceProvider.IsConnected();

        /// <summary>
        /// The Leap attachment hands, used to determine which hand is currently tracked by leap.
        /// </summary>
        private AttachmentHands leapAttachmentHands = null;

        /// <summary>
        /// List of hands that are currently in frame and detected by the leap motion controller. If there are no hands in the current frame, this list will be empty.
        /// </summary>
        private List<Hand> currentHandsDetectedByLeap => LeapMotionServiceProvider.CurrentFrame.Hands;

        // This value can only be set in the profile, the default is LeapControllerOrientation.Headset.
        private EskyLeapControllerOrientation leapControllerOrientation => SettingsProfile.LeapControllerOrientation;

        /// <summary>
        /// Adds an offset to the game object with LeapServiceProvider attached.  This offset is only applied if the leapControllerOrientation
        /// is LeapControllerOrientation.Desk and is necessary for the hand to appear in front of the main camera. If the leap controller is on the 
        /// desk, the LeapServiceProvider is added to the scene instead of the LeapXRServiceProvider. The anchor point for the position of the leap hands is 
        /// the position of the game object with the LeapServiceProvider attached.
        /// </summary>
        private Vector3 leapHandsOffset => SettingsProfile.LeapControllerOffset;

        /// <summary>
        /// Dictionary to capture all active leap motion hands detected.
        /// </summary>
        private readonly Dictionary<Handedness, EskyLeapMotionArticulatedHand> trackedHands = new Dictionary<Handedness, EskyLeapMotionArticulatedHand>();

        private AttachmentHand leftAttachmentHand = null;
        private AttachmentHand rightAttachmentHand = null;

        private static readonly ProfilerMarker UpdatePerfMarker = new ProfilerMarker("[MRTK] LeapMotionDeviceManager.Update");
        GameObject g;
        bool spawnedEskyRig = false;
        /// <inheritdoc />
        /// p
        /// 
        /// 
        EskySettings es = new EskySettings();
        public override void Enable()
        {
            
            try{
                es = JsonUtility.FromJson<EskySettings>(System.IO.File.ReadAllText("EskySettings.json"));
            }catch(System.Exception e){
                Debug.LogError("Couldn't load the esky config:" + e.Message);
                #if !UNITY_EDITOR
                Application.Quit();
                #endif
            }
            #if UNITY_EDITOR
            es.reprojectionSettings = SettingsProfile.ReprojectionSettings;
            es.rigToUse = SettingsProfile.RigToUse;
            es.targetFrameRate = SettingsProfile.TargetFrameRate;
            es.UsesCameraPreview = SettingsProfile.UsesCameraPreview;
            es.usesExternalRGBCamera = SettingsProfile.UsesExternalRGBCamera;
            es.UseTrackerOffsets = SettingsProfile.UseTrackerOffsets;
            es.UseNetworkingDebugVis = SettingsProfile.UseNetworkingDebugRig;
            #endif
            g = GameObject.Find("CalibrationRig");
            if(g == null){
                switch(es.rigToUse){
                    case RigToUse.NorthStarXSensor:
                    g = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/V2Rigs/NorthStarRigXSensor") as GameObject);
                    spawnedEskyRig = true;
                    break;
                    case RigToUse.NorthStarV2:
                    g = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/V2Rigs/NorthStarRigV2") as GameObject);
                    spawnedEskyRig = true;                
                    break;
                    case RigToUse.Ariel:                
                    g = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/V2Rigs/ArielRig") as GameObject);                
                    spawnedEskyRig = true;                
                    break;
                    case RigToUse.ArielMKII:
                    g = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/V2Rigs/ArielRigMKII") as GameObject);                
                    spawnedEskyRig = true;                
                    break;                    
                    case RigToUse.NorthStarV1:
                    g = GameObject.Instantiate<GameObject>(Resources.Load("EskyRigs/V1Rigs/NorthStarRigV1") as GameObject);
                    spawnedEskyRig = true;                
                    break;
                    case RigToUse.Custom:
                    g = GameObject.Instantiate<GameObject>(customRig);                
                    spawnedEskyRig = true;                
                    break;
                }
            }else{
                spawnedEskyRig = true;
            }
            
            string message = JsonUtility.ToJson(es,true);
            g.SendMessage("DumpSettingsConfig",SettingsProfile.SaveAfterStoppingEditor);
            g.SendMessage("ReceiveConfig",message);            
            g.transform.position = CameraCache.Main.transform.position;
            g.transform.rotation = CameraCache.Main.transform.rotation;
            g.transform.parent = CameraCache.Main.transform.parent;
            GameObject.DestroyImmediate(CameraCache.Main.gameObject);
            g.SetActive(true);
            base.Enable();
            switch(leapControllerOrientation){
                case EskyLeapControllerOrientation.Headset:                
                CameraCache.Main.gameObject.AddComponent<LeapXRServiceProvider>();                
                break;
                case EskyLeapControllerOrientation.Desk:
                GameObject leapProviderDesk = new GameObject("LeapProvider");
                // The LeapServiceProvider does not need to be attached to a camera, but the location of this gameobject is the anchor for the desk hands
                LeapMotionServiceProvider = leapProviderDesk.AddComponent<LeapServiceProvider>();
                // Follow the transform of the main camera by adding the service provider as a child of the main camera
                leapProviderDesk.transform.parent = CameraCache.Main.transform;
                // Apply hand position offset, an offset is required to render the hands in view and in front of the camera
                LeapMotionServiceProvider.transform.position += leapHandsOffset;                
                break;
                case EskyLeapControllerOrientation.Esky:
                GameObject LeapProviderEsky =  GameObject.Find("LeapMotion");
                if(LeapProviderEsky != null){
                    LeapMotionServiceProvider = LeapProviderEsky.GetComponent<LeapXRServiceProvider>();
                    Debug.Log("Setting the hand models");
                    GameObject g = GameObject.Find("HandModels");
                    g.GetComponent<HandModelManager>().leapProvider = LeapMotionServiceProvider;
                }else{
                    Debug.LogError("Couldn't find a 'LeapMotion' game object in scene, the Esky Leapmotion provider needs this, did you modify the transform structure???");
                }
                break;
            }


            // Add the attachment hands to the scene for the purpose of getting the tracking state of each hand and joint positions
            GameObject leapAttachmentHandsGameObject = new GameObject("LeapAttachmentHands");
            leapAttachmentHands = leapAttachmentHandsGameObject.AddComponent<AttachmentHands>();

            // The first hand in attachmentHands.attachmentHands is always left
            leftAttachmentHand = leapAttachmentHands.attachmentHands[0];

            // The second hand in attachmentHands.attachmentHands is always right
            rightAttachmentHand = leapAttachmentHands.attachmentHands[1];

            // Enable all attachment point flags in the leap hand. By default, only the wrist and the palm are enabled.
            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                leapAttachmentHands.attachmentPoints |= EskyLeapMotionArticulatedHand.ConvertMRTKJointToLeapJoint(joint);
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            base.Disable();

            // Only destroy the objects if the application is playing because the objects are added to the scene at runtime
            if (Application.isPlaying)
            {
                // Destroy AttachmentHands GameObject
                if (leapAttachmentHands != null)
                {
                    GameObject.Destroy(leapAttachmentHands.gameObject);
                }
                if(spawnedEskyRig){
                    GameObject.Destroy(g);
                }
                if (LeapMotionServiceProvider != null)
                {
                    // Destroy the LeapProvider GameObject if the controller orientation is the desk
                    if (leapControllerOrientation == EskyLeapControllerOrientation.Desk)
                    {
                        GameObject.Destroy(LeapMotionServiceProvider.gameObject);
                    }
                    // Destroy the LeapXRServiceProvider attached to the main camera if the controller orientation is headset
                    else if (leapControllerOrientation == EskyLeapControllerOrientation.Headset)
                    {
                        GameObject.Destroy(LeapMotionServiceProvider);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new LeapMotionArticulatedHand to the scene.
        /// </summary>
        /// <param name="handedness">The handedness (Handedness.Left or Handedness.Right) of the hand to be added</param>
        private void OnHandDetected(Handedness handedness)
        {
            // Only create a new hand if the hand does not exist
            if (!trackedHands.ContainsKey(handedness))
            {
                var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
                var inputSource = CoreServices.InputSystem?.RequestNewGenericInputSource($"Leap {handedness} Controller", pointers, InputSourceType.Hand);
                var leapHand = new EskyLeapMotionArticulatedHand(TrackingState.Tracked, handedness, inputSource);

                // Set pinch thresholds
                leapHand.HandDefinition.EnterPinchDistance = enterPinchDistance;
                leapHand.HandDefinition.ExitPinchDistance = exitPinchDistance;

                // Set the leap attachment hand to the corresponding handedness
                if (handedness == Handedness.Left)
                {
                    leapHand.SetAttachmentHands(leftAttachmentHand, LeapMotionServiceProvider);
                }
                else // handedness == Handedness.Right
                {
                    leapHand.SetAttachmentHands(rightAttachmentHand, LeapMotionServiceProvider);
                }
                
                // Set the pointers for an articulated hand to the leap hand
                foreach (var pointer in pointers)
                {
                    pointer.Controller = leapHand;
                }

                trackedHands.Add(handedness, leapHand);

                CoreServices.InputSystem.RaiseSourceDetected(inputSource, leapHand);
            }
        }

        /// <summary>
        /// Removes the LeapMotionArticulated hand from the scene when the tracking is lost.
        /// </summary>
        /// <param name="handedness">The handedness (Handedness.Left or Handedness.Right) of the hand to be removed</param>
        private void OnHandDetectionLost(Handedness handedness)
        {
            if (CoreServices.InputSystem != null)
            {
                CoreServices.InputSystem.RaiseSourceLost(trackedHands[handedness].InputSource);
            }

            // Disable the pointers if the hand is not tracking
            RecyclePointers(trackedHands[handedness].InputSource);

            // Remove hand from tracked hands
            trackedHands.Remove(trackedHands[handedness].ControllerHandedness);
        }

        /// <summary>
        /// Update the number of tracked leap hands.
        /// </summary>
        /// <param name="isLeftTracked">The tracking state of the left leap hand</param>
        /// <param name="isRightTracked">The tracking state of the right leap hand</param>
        private void UpdateLeapTrackedHands(bool isLeftTracked, bool isRightTracked)
        {
            // Left Hand Update
            if (isLeftTracked && !trackedHands.ContainsKey(Handedness.Left))
            {
                OnHandDetected(Handedness.Left);
            }            
            else if (!isLeftTracked && trackedHands.ContainsKey(Handedness.Left))
            {
                OnHandDetectionLost(Handedness.Left);
            }

            // Right Hand Update
            if (isRightTracked && !trackedHands.ContainsKey(Handedness.Right))
            {
                OnHandDetected(Handedness.Right);
            }
            else if (!isRightTracked && trackedHands.ContainsKey(Handedness.Right))
            {
                OnHandDetectionLost(Handedness.Right);
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            using (UpdatePerfMarker.Auto())
            {
                if (IsLeapConnected)
                {
                    // if the number of tracked hands in frame has changed
                    if (currentHandsDetectedByLeap.Count != trackedHands.Count)
                    {
                        UpdateLeapTrackedHands(leftAttachmentHand.isTracked, rightAttachmentHand.isTracked);
                    }

                    // Update the hand/hands that are in trackedhands
                    foreach (KeyValuePair<Handedness, EskyLeapMotionArticulatedHand> hand in trackedHands)
                    {
                        hand.Value.UpdateState();
                    }
                }
            }
        }
#endif
    }

}

