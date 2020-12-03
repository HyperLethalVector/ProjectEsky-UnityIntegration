/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using Leap.Unity.Attributes;
//using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity.AR {

  public class LeapDeviceTracking : MonoBehaviour {

    [FormerlySerializedAs("LeapProvider")]
    [Tooltip("The Leap Service Provider for the Device that is Attached to the Headset")]
    public LeapServiceProvider leapProvider;
    public Transform head;

    [FormerlySerializedAs("ExtrapolationAmount")]
    [Tooltip("Extrapolation amount in microseconds.")]
    public long extrapolationAmount = 18000;

    [Tooltip("Multiplies your tracked position vector.")]
    public float positionScaleFactor = 1f;

    public bool useDifferentRotationExtrapolation = false;
    [DisableIf("useDifferentRotationExtrapolation", isEqualTo: false)]
    public long rotationExtrapolationAmount = 35000;

    [NonSerialized]
    public Vector3 _devicePosition;

    [NonSerialized]
    public Quaternion _deviceRotation;

    public Camera preRenderCamera;
    public bool useLateUpdate = true;

    private Vector3 _positionalDrift = Vector3.zero;
    //private bool onNewRig = false;

    [Header("Debug")]
    [Tooltip("Receive head data, transform it, and call debugCallPerPose, "
      + "but don't actually set the center eye transform.")]
    public bool receiveButDontSet = false;
    // Debug: Called whenever we 
    public Action<Pose> debugCallPerHeadPose = null;
    public Action<Pose> debugCallPerLeapPose = null;

    void Start() {
      Camera.onPreRender += onPreRender;

      if (preRenderCamera == null) preRenderCamera = Camera.main;
      // if (transform.root.GetComponent<OpticalCalibrationManager>() != null) {
      //   onNewRig = true;
      // }
    }

    void Update() {
      // if (Input.GetKeyDown(KeyCode.R)
      //     && GetComponent<PuckDriftCorrector>() == false) {
      //   _positionalDrift = _devicePosition + _positionalDrift -
      //     transform.parent.position;
      // }
    }

    private void onPreRender(Camera cameraAboutToRender) {
      if (cameraAboutToRender != preRenderCamera) return;
      if (useLateUpdate) return;

      updatePositionTracking();

      leapProvider.RetransformFrames();
    }

    private void LateUpdate() {
      if (!useLateUpdate) return;

      updatePositionTracking();

      leapProvider.RetransformFrames();
    }

    private void updatePositionTracking() {
      if (leapProvider == null) return;
      if (head == null) head = transform.parent;

      // Update head transform in leap device space. This probably won't change
      // very often or at all, but we want to support it changing.
      // Read as "head in leap space".
      var head_leap = leapProvider.transform.ToPose().inverse *
        head.ToPose();

      // // This transform should match the Leap Provider's transform.
      // var providerPose = leapProvider.transform.ToPose();
      // this.transform.SetPose(providerPose);
      // leapProvider.transform.SetPose(providerPose);

      // Get the pose from the head event based with a given extrapolation time.
      // Rotation may be extrapolated a different amount later.
      var headEvent = new LeapInternal.LEAP_HEAD_POSE_EVENT();
      leapProvider.GetLeapController().GetInterpolatedHeadPose(
        ref headEvent, 
        leapProvider.GetLeapController().Now() + extrapolationAmount
      );

      // Get head event position at the extrapolation time and convert to
      // Unity's coordinate frame.
      _devicePosition = headEvent.head_position.ToVector3() / 1000f;
      _devicePosition =
        new Vector3(-_devicePosition.x, -_devicePosition.z, _devicePosition.y);
      _devicePosition *= positionScaleFactor;

      // Get head event rotation in device coordinate frame, potentially at a
      // different extrapolation time depending on settings.
      _deviceRotation = headEvent.head_orientation.ToQuaternion();
      if (useDifferentRotationExtrapolation) {
        leapProvider.GetLeapController().GetInterpolatedHeadPose(
          ref headEvent, 
          leapProvider.GetLeapController().Now() + //.CurrentFrame.Timestamp +
          rotationExtrapolationAmount
        );
        _deviceRotation = headEvent.head_orientation.ToQuaternion();
      }
      else {
        // Make sure serialized rotation extrapolation matched position
        // extrapolation if "use different rotation amount" is disabled.
        rotationExtrapolationAmount = extrapolationAmount; 
      }

      // Convert rotation to Unity's coordinate frame.
      _deviceRotation = Quaternion.LookRotation(Vector3.up, -Vector3.forward) *
        _deviceRotation *
        Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, -Vector3.forward));

      // Apply the head event pose only if the position and rotation are valid.
      if ((_devicePosition != Vector3.zero && !_devicePosition.ContainsNaN()
          && _deviceRotation != default(Quaternion)
          && !_deviceRotation.ContainsNaN())) {
        _devicePosition -= _positionalDrift;

        var newDevicePose = new Pose(_devicePosition, _deviceRotation);
        var newHeadPose = newDevicePose * head_leap;

        if (!receiveButDontSet) {
          head.SetLocalPose(newHeadPose);
        }

        if (debugCallPerHeadPose != null) {
          debugCallPerHeadPose(newHeadPose);
        }
        if (debugCallPerLeapPose != null) {
          debugCallPerLeapPose(newDevicePose);
        }

        // Move the head directly to the Leap's position.
        // head.localPosition = _devicePosition;
        // head.localRotation = _deviceRotation;


        // //This trick doesn't work if you update in PreCull
        // //(because by then it's already too late for transforms to be updated)
        // //called then, it will enqueue your update to the next frame
        // //(adding a frame of latency)
        // if (onNewRig && useLateUpdate) {
        //   transform.parent = head.parent;
        //   head.parent = transform;
        //   transform.localScale = Vector3.one;
        // }

        // if (!receiveButDontSet) {
        //   transform.localPosition = _devicePosition;
        //   transform.localRotation = _deviceRotation;
        // }

        // if (onNewRig && useLateUpdate) {
        //   head.parent = transform.parent;
        //   transform.parent = head;
        //   head.localScale = Vector3.one;
        // }
      }
    }

  }

}
