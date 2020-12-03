/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.AR {

  //[ExecuteInEditMode]
  public class IPDAdjuster : MonoBehaviour {

    [Header("IPD")]
    public float ipd = 0.064f;
    public float heightOffset = 0f;
    public float depthOffset = 0f;
    public float pitchOffset = 0f;
    public float yawOffset = 0f;
    //public float screenForwardOffset;
    private float? _lastKnownIPD = null;
    private float? _lastKnownHeight = null;
    private float? _lastKnownDepth = null;
    private float? _lastKnownPitch = null;
    private float? _lastKnownYaw = null;

    //private float? _lastKnownScreenForwardOffset = null;
    //private Vector3 startingScreenLeft, startingScreenRight;
    //private int lastSentCommand = 0;

    [Header("Left Eye")]
    [Tooltip("When IPD is adjusted, this transform's local X coordinate is moved.")]
    public Transform   leftEyeIPDTransform;
    [Tooltip("When IPD is adjusted, CalculateDistortionMesh() is called.")]
    public ARRaytracer leftEyeARRaytracer;
    //[Tooltip("Adjusts the screen depth for the varifocal headset.")]
    //public Transform   leftScreenTransform;

    [Header("Right Eye")]
    [Tooltip("When IPD is adjusted, this transform's local X coordinate is moved.")]
    public Transform   rightEyeIPDTransform;
    [Tooltip("When IPD is adjusted, CalculateDistortionMesh() is called.")]
    public ARRaytracer rightEyeARRaytracer;
    //[Tooltip("Adjusts the screen depth for the varifocal headset.")]
    //public Transform   rightScreenTransform;

    [Header("Leap Hand Tracker")]
    [Tooltip("Adjusts the pitch and yaw of the hand tracker.")]
    public Transform leapTransform;

    [Header("Hotkeys")]
    [Tooltip("Nudges the IPD wider by 1 millimeter.")]
    public KeyCode adjustWiderKey = KeyCode.Plus;
    [Tooltip("Nudges the IPD narrower by 1 millimeter.")]
    public KeyCode adjustNarrowerKey = KeyCode.Minus;
    [Tooltip("Nudges the Eye Height higher by 1 millimeter.")]
    public KeyCode adjustHigherKey = KeyCode.UpArrow;
    [Tooltip("Nudges the Eye Height lower by 1 millimeter.")]
    public KeyCode adjustLowerKey = KeyCode.DownArrow;
    [Tooltip("Nudges the Eye Recession closer by 1 millimeter.")]
    public KeyCode adjustCloserKey = KeyCode.LeftArrow;
    [Tooltip("Nudges the Eye Recession farther by 1 millimeter.")]
    public KeyCode adjustFartherKey = KeyCode.RightArrow;

    [Tooltip("Nudges the Leap Tracker to point higher by 1 millimeter.")]
    public KeyCode adjustTrackerHigherKey = KeyCode.Keypad8;
    [Tooltip("Nudges the Leap Tracker to point lower by 1 millimeter.")]
    public KeyCode adjustTrackerLowerKey = KeyCode.Keypad2;
    [Tooltip("Nudges the Leap Tracker to point leftward by 1 millimeter.")]
    public KeyCode adjustTrackerLeftKey = KeyCode.Keypad4;
    [Tooltip("Nudges the Leap Tracker to point rightward by 1 millimeter.")]
    public KeyCode adjustTrackerRightKey = KeyCode.Keypad6;

    //[Tooltip("Nudges the Screen Recession forward by 1 millimeter.")]
    //public KeyCode screenForwardKey = KeyCode.Period;
    //[Tooltip("Nudges the Screen Recession backward by 1 millimeter.")]
    //public KeyCode screenBackwardKey = KeyCode.Comma;

    [Header("Debug")]
    public TextMesh debugText;

    private bool isConfigured {
      get {
        return !(leftEyeIPDTransform == null || leftEyeARRaytracer == null || 
                 rightEyeIPDTransform == null || rightEyeARRaytracer == null || 
                 leapTransform == null);
      }
    }

    private void Start() {
      if (isConfigured) {
        ipd = rightEyeIPDTransform.localPosition.x * 2f;
        heightOffset = rightEyeIPDTransform.localPosition.y;
        depthOffset = rightEyeIPDTransform.localPosition.z;
        pitchOffset = leapTransform.localEulerAngles.x;
        yawOffset = leapTransform.localEulerAngles.y;
        //startingScreenLeft = leftScreenTransform.localPosition;
        //startingScreenRight = rightScreenTransform.localPosition;
      }
    }

    public void resetEyes() {
      enabled = true;
      Start();
      ipd = 0.064f;
      heightOffset = -0.011f;
      depthOffset = -0.005f;
      RefreshIPD();
    }

    void Update() {
      if (!isConfigured) return;

      if (Application.isPlaying) {
        if (Input.GetKey(adjustWiderKey)) {
          ipd += 0.004f * Time.deltaTime;
        }
        if (Input.GetKey(adjustNarrowerKey)) {
          ipd -= 0.004f * Time.deltaTime;
        }

        if (Input.GetKey(adjustHigherKey)) {
          heightOffset += 0.05f * Time.deltaTime;
        }
        if (Input.GetKey(adjustLowerKey)) {
          heightOffset -= 0.05f * Time.deltaTime;
        }
        //heightOffset += 0.01f * Time.deltaTime * Input.GetAxis("6");


        if (Input.GetKey(adjustCloserKey)) {
          depthOffset += 0.05f * Time.deltaTime;
        }
        if (Input.GetKey(adjustFartherKey)) {
          depthOffset -= 0.05f * Time.deltaTime;
        }

        if (Input.GetKey(adjustTrackerHigherKey)){// || Input.GetAxis("joystick button 0")) {
          pitchOffset -= 20f * Time.deltaTime;
        }
        if (Input.GetKey(adjustTrackerLowerKey)) {
          pitchOffset += 20f * Time.deltaTime;
        }

        if (Input.GetKey(adjustTrackerRightKey)) {
          yawOffset += 20f * Time.deltaTime;
        }
        if (Input.GetKey(adjustTrackerLeftKey)) {
          yawOffset -= 20f * Time.deltaTime;
        }

        /*if (Input.GetKey(screenForwardKey)) {
          screenForwardOffset += 0.01f * Time.deltaTime;
        }
        if (Input.GetKey(screenBackwardKey)) {
          screenForwardOffset -= 0.01f * Time.deltaTime;
        }
        screenForwardOffset = Mathf.Clamp(screenForwardOffset, -0.013f, 0f);
        
        int currentServoOffset = (int)screenForwardOffset.Remap(0, -0.013f, 0, 25);
        if (currentServoOffset != lastSentCommand) {
          string command = currentServoOffset.ToString();
          ThreadedVarifocalSerialProcessor processor = FindObjectOfType<ThreadedVarifocalSerialProcessor>();
          if(processor != null) processor.SerialCommands.TryEnqueue(command);
          lastSentCommand = currentServoOffset;
        }*/
      }

      if (!_lastKnownIPD.HasValue    || _lastKnownIPD.Value    != ipd ||
          !_lastKnownHeight.HasValue || _lastKnownHeight.Value != heightOffset ||
          !_lastKnownDepth.HasValue  || _lastKnownDepth.Value  != depthOffset ||
          !_lastKnownPitch.HasValue  || _lastKnownPitch.Value  != pitchOffset ||
          !_lastKnownYaw.HasValue    || _lastKnownYaw.Value    != yawOffset) {
          //|| !_lastKnownScreenForwardOffset.HasValue || _lastKnownScreenForwardOffset.Value != screenForwardOffset) {
        RefreshIPD();

        _lastKnownIPD = ipd;
        _lastKnownHeight = heightOffset;
        _lastKnownDepth = depthOffset;
        _lastKnownPitch = pitchOffset;
        _lastKnownYaw = yawOffset;

        if (debugText != null) debugText.text =
            "IPD: " + (ipd * 1000f).ToString("n2") + "mm\n" +
            "Height: " + (heightOffset * 1000f).ToString("n2") + "mm\n" +
            "Depth: " + (depthOffset * 1000f).ToString("n2") + "mm";
      }
    }

    public void RefreshIPD() {
      if (!isConfigured) return;
      Vector3 lPos = leftEyeIPDTransform.localPosition;
      Vector3 rPos = rightEyeIPDTransform.localPosition;
      lPos.x = -ipd / 2f; rPos.x = ipd / 2f;
      lPos.y = heightOffset; rPos.y = heightOffset;
      lPos.z = depthOffset; rPos.z = depthOffset;
      leftEyeIPDTransform.localPosition = lPos;
      rightEyeIPDTransform.localPosition = rPos;

      leapTransform.localEulerAngles = new Vector3(pitchOffset, yawOffset, 0f);

      //if (Application.isPlaying) {
      //  leftScreenTransform.localPosition = startingScreenLeft + (leftScreenTransform.localRotation * Vector3.forward) * screenForwardOffset;
      //  rightScreenTransform.localPosition = startingScreenRight + (rightScreenTransform.localRotation * Vector3.forward) * screenForwardOffset;
      //}

      OpticalCalibrationManager manager = GetComponent<OpticalCalibrationManager>();
      if (manager != null) {
        manager.currentCalibration.leftEye.eyePosition = lPos;
        manager.currentCalibration.rightEye.eyePosition = rPos;
        manager.UpdateCalibrationFromObjects();
      }

      leftEyeARRaytracer.ScheduleCreateDistortionMesh();
      rightEyeARRaytracer.ScheduleCreateDistortionMesh();
    }

  }

}
