/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;
namespace Leap.Unity.AR {

  using Pose = Leap.Unity.Pose;

  public class OpticalCalibrationManager : MonoBehaviour {

    const string CALIBRATION_ARG_REGEX = "--use_calibration=\"(.*)\"";

    readonly Vector4 DEFAULT_PROJECTION_PARAMS = new Vector4(-0.7f, 0.7f, 0.3f, -1.40f);
    Vector4 rightEyeProjectionParams = new Vector4(-0.7f, 0.7f, 0.3f, -1.40f);
    Vector4 leftEyeProjectionParams = new Vector4(-0.7f, 0.7f, 0.3f, -1.40f);

    readonly Vector3 DEFAULT_LEFT_EYE = new Vector3(-0.032f, 0f, 0f);


    [Tooltip("Press this key to save a new calibration file specified by the file name "
           + "into the calibrations folder.")]
    public KeyCode saveCalibrationKey = KeyCode.S;

    [Tooltip("If this option is checked, the Ctrl key must be held down to save a calibration.")]
    public bool requireCtrlHeld = true;

    [QuickButton("Save Now", "SaveCurrentCalibration")]
    public string outputCalibrationFile = "";

    [QuickButton("Load Now", "LoadCalibration")]
    public string inputCalibrationFile = "AR00.json";

    public bool loadInputFileOnStart = false;
    public bool allowSavingInBuild = false;
    public bool saveSteamVRStyleCalibration = false;

    public KeyCode recalculateFromTransformsKey = KeyCode.T;
    [Tooltip("Recalculate calibration from relevant Transforms every frame.")]
    public bool everyFrameRecalculateFromTransforms = false;

    public ARRaytracer leftEye, rightEye;
    public LeapXRServiceProvider provider;
    public Transform headTransform;
    /// <summary>
    /// Gets the pose of headTransform, or null if the headTransform is not set.
    /// </summary>
    public Pose? maybeHeadsetPose {
      get {
        if (headTransform != null) {
          return headTransform.ToPose();
        }
        return null;
      }
    }

    [NonSerialized]
    public HeadsetCalibration currentCalibration;

    /// <summary>
    /// The destination file path of the calibration file if SaveCurrentCalibration() is
    /// called. (Read only.)
    /// </summary>
    public string outputFilePath {
      get {
        return Path.Combine("./OpticalCalibrations/V1", outputCalibrationFile);
      }
    }

    /// <summary>
    /// The path of the calibration file to load if LoadCalibration() is called.
    /// (Read only.)
    /// </summary>
    public string inputFilePath {
      get {
        return Path.Combine("./OpticalCalibrations/V1", inputCalibrationFile);
      }
    }

    void Start() {
        // Load a default setup.
        currentCalibration.leapTracker = new PhysicalComponent(provider.deviceOrigin.ToWorldPose(), provider.gameObject.name);
        if (leftEye != null && leftEye.ellipse != null && leftEye.Screen != null) {
          leftEyeProjectionParams = DEFAULT_PROJECTION_PARAMS;
          currentCalibration.leftEye = constructLeftEyeOptics();
          
        }
        if (rightEye != null && rightEye.ellipse != null && rightEye.Screen != null) {
          rightEyeProjectionParams = DEFAULT_PROJECTION_PARAMS;
          currentCalibration.rightEye = constructRightEyeOptics();
        }      
    }
    void LateUpdate(){      
      if (loadInputFileOnStart) {
        loadInputFileOnStart = false;
        LoadCalibration();
      }
    }
    void Update() {
      if ((Application.isEditor || allowSavingInBuild) && Input.GetKeyDown(saveCalibrationKey)
          && (!requireCtrlHeld // Optionally require "Ctrl" key held down
              || (Input.GetKey(KeyCode.LeftControl)
              || Input.GetKey(KeyCode.RightControl)))) {
        SaveCurrentCalibration();
      }

      if (Input.GetKeyDown(recalculateFromTransformsKey)
          || everyFrameRecalculateFromTransforms) {
        UpdateCalibrationFromObjects();
      }
    }

    /// <summary>
    /// Outputs the current state of the headset as a calibration file based on the
    /// configured output file name into the configured calibrations folder.
    /// </summary>
    public void SaveCurrentCalibration() {
      if (!Application.isPlaying) {
        Debug.LogError("For safety, calibrations cannot be saved at edit-time. "
                     + "Enter play mode to save a calibration.", this);
        return;
      }

      int randomNumber = UnityEngine.Random.Range(0, 10000);
      string temp = outputCalibrationFile;
      if (string.IsNullOrEmpty(outputCalibrationFile) || (!Application.isEditor)) {
        outputCalibrationFile = randomNumber + " - Temp Calibration.json";
        Debug.LogWarning("outputCalibrationFile was null or empty; defaulting to "
                         + "'XXXX - Temp Calibration.json'.", this);
      }

      //If this is a calibrator scene, find the screen calibrator and optimizers and 
      //stop the optimization process to allow us to reload the calibration we're saving
      Testing.WebcamScreenCalibration calibratorObject = FindObjectOfType<Testing.WebcamScreenCalibration>();
      if(calibratorObject != null) {
        calibratorObject.StopAllCoroutines();
        var optimizers = FindObjectsOfType<Testing.DenseOptimizer>();
        foreach (var optimizer in optimizers) { optimizer.StopSolve(); }
      }


      Pose devicePose;
      if (headTransform != null) {
        devicePose = headTransform.ToPose().inverse *
          provider.deviceOrigin.ToPose();
      }
      else {
        devicePose = provider.deviceOrigin.ToLocalPose();
      }
      currentCalibration.leapTracker = new PhysicalComponent(
        devicePose,
        provider.gameObject.name
      );

      currentCalibration.leftEye = constructLeftEyeOptics(false);
      currentCalibration.rightEye = constructRightEyeOptics(false);

      var outputPath = outputFilePath;
      File.WriteAllText(outputPath,
        JsonUtility.ToJson(currentCalibration, prettyPrint: true)
                            .Replace("\n", "\r\n")
                            .Replace("    ", "  "));

      Debug.Log("Saved current calibration to: " + outputPath);

      inputCalibrationFile = outputCalibrationFile;
      LoadCalibration(disableEllipsoids: false, ignoreConfig: true);

      if (saveSteamVRStyleCalibration) {
        outputCalibrationFile = randomNumber + " - Temp SteamVR Calibration.vrsettings";
        var steamVROutputDirectory = Path.Combine("./OpticalCalibrations/",
          "SteamVR/");
        if (!Directory.Exists(steamVROutputDirectory)) {
          Directory.CreateDirectory(steamVROutputDirectory);
        }
        outputPath = Path.Combine(steamVROutputDirectory, randomNumber + " - Temp SteamVR Calibration.vrsettings");

        SteamVRHeadsetCalibration steamVRCalib = new SteamVRHeadsetCalibration();
        steamVRCalib.leftEye = currentCalibration.leftEye;
        steamVRCalib.rightEye = currentCalibration.rightEye;

        string steamVRJSON =
          (SteamVROptics.getSteamVRPrefixString() +
          JsonUtility.ToJson(steamVRCalib, prettyPrint: true)
                             .Substring(1) //(remove first bracket)
                             .Replace("\n", "\r\n"))
                             .Replace("    ", "  ");

        File.WriteAllText(outputPath, steamVRJSON);
        Debug.Log("Saved SteamVR calibration to: " + outputPath);
      }
      outputCalibrationFile = temp;
    }

    /// <summary>
    /// Loads the calibration file specified by the inputCalibrationFile field, which
    /// </summary>
    public void LoadCalibration(bool disableEllipsoids = true, bool ignoreConfig = false) {
      if (!Application.isPlaying) {
        Debug.LogError("For safety, calibrations cannot be loaded at edit-time. "
                     + "Enter play mode to load a calibration.", this);
        return;
      }

      if (string.IsNullOrEmpty(inputCalibrationFile)) {
        Debug.LogError("inputCalibrationFile field is null or empty; cannot load "
                     + "a calibration file.", this);
      }

      var pathToLoad = inputFilePath;

      // Check command line args for an override.
      // Scan command line environments for a headset calibration file
      // and set that as the input file if found.
      var commandLineArgs = System.Environment.GetCommandLineArgs();
      var useCalibrationRegEx =
        new System.Text.RegularExpressions.Regex(CALIBRATION_ARG_REGEX);
      var calibrationOverrideFile = "";
      bool foundMatch = false;
      bool wasValidMatch = false;
      foreach (var arg in commandLineArgs) {
        var match = useCalibrationRegEx.Match(arg);
        if (match.Groups.Count > 1 && match.Groups[1].Captures.Count > 0) {
          calibrationOverrideFile = match.Groups[1].Captures[0].Value;
          Debug.Log("Loading use_calibration arg: " + calibrationOverrideFile);
          foundMatch = true;

          wasValidMatch = TryLoadCalibrationFromPath(calibrationOverrideFile);
        }
      }
      if (foundMatch) {
        if (!wasValidMatch) {
          Debug.LogError("Failed to load " + calibrationOverrideFile);
        }
        else {
          pathToLoad = calibrationOverrideFile;
        }
      }

      // Check the config.json file for ANOTHER override. We can probably remove
      // the command flag override now.
      if (!ignoreConfig) {
        if (Config.TryRead<string>("northStarCalibration",
                                   ref inputCalibrationFile)) {
          Path.ChangeExtension(inputCalibrationFile, ".json");
          pathToLoad = inputFilePath; // Reload the file path.
          Debug.Log("Set input calibration to " + inputCalibrationFile +
            " from config.json.");
        }
      }

      TryLoadCalibrationFromPath(pathToLoad, disableEllipsoids);
    }
    public bool TryLoadCalibrationFromEsky(HeadsetCalibration calibration, bool disableEllipsoids){
        currentCalibration = calibration;        
        provider.deviceOrigin.localPosition = currentCalibration.leapTracker.localPose.position;
        provider.deviceOrigin.localRotation = currentCalibration.leapTracker.localPose.rotation;

        provider.deviceOrigin.SetLocalPose(currentCalibration.leapTracker.localPose);
        if (leftEye != null) {
          leftEye.eyePerspective.transform.localPosition = currentCalibration.leftEye.eyePosition;
          leftEyeProjectionParams = currentCalibration.leftEye.cameraProjection;
          if (leftEye.ellipse != null) {
            if (disableEllipsoids) leftEye.ellipse.enabled = false;
            leftEye.ellipse.MinorAxis = currentCalibration.leftEye.ellipseMinorAxis;
            leftEye.ellipse.MajorAxis = currentCalibration.leftEye.ellipseMajorAxis;
            leftEye.ellipse.sphereToWorldSpace = currentCalibration.leftEye.sphereToWorldSpace;
            leftEye.ellipse.worldToSphereSpace = currentCalibration.leftEye.sphereToWorldSpace.inverse;
          }
          if (leftEye.Screen != null) {
            Matrix4x4 screenTransform = currentCalibration.leftEye.worldToScreenSpace.inverse;
            leftEye.Screen.position = provider.transform.parent.TransformPoint(screenTransform.GetVector3());
            leftEye.Screen.rotation = provider.transform.parent.rotation * screenTransform.GetQuaternion();
            leftEye.Screen.localScale = screenTransform.lossyScale;
          }
        }
        if (rightEye != null) {
          rightEye.eyePerspective.transform.localPosition = currentCalibration.rightEye.eyePosition;
          rightEyeProjectionParams = currentCalibration.rightEye.cameraProjection;
          if (rightEye.ellipse != null) {
            if (disableEllipsoids) rightEye.ellipse.enabled = false;
            rightEye.ellipse.MinorAxis = currentCalibration.rightEye.ellipseMinorAxis;
            rightEye.ellipse.MajorAxis = currentCalibration.rightEye.ellipseMajorAxis;
            rightEye.ellipse.sphereToWorldSpace = currentCalibration.rightEye.sphereToWorldSpace;
            rightEye.ellipse.worldToSphereSpace = currentCalibration.rightEye.sphereToWorldSpace.inverse;
          }
          if (rightEye.Screen != null) {
            Matrix4x4 screenTransform = currentCalibration.rightEye.worldToScreenSpace.inverse;
            rightEye.Screen.position = provider.transform.parent.TransformPoint(screenTransform.GetVector3());
            rightEye.Screen.rotation = provider.transform.parent.rotation * screenTransform.GetQuaternion();
            rightEye.Screen.localScale = screenTransform.lossyScale;
          }
        }

        ARRaytracer[] raytracers = GetComponentsInChildren<ARRaytracer>();
        foreach (ARRaytracer raytracer in raytracers) {
          raytracer.ScheduleCreateDistortionMesh();
        }
        Debug.Log("V1 Headset calibration successfully loaded from Esky");
        return true;
    }
    public bool TryLoadCalibrationFromPath(string inputFilePath,
                                           bool disableEllipsoids = true) {
      var inputFile = inputFilePath;

      if (File.Exists(inputFile)) {
        string calibrationData = File.ReadAllText(inputFile);
        currentCalibration = JsonUtility.FromJson<HeadsetCalibration>(calibrationData);
        provider.deviceOrigin.localPosition = currentCalibration.leapTracker.localPose.position;
        provider.deviceOrigin.localRotation = currentCalibration.leapTracker.localPose.rotation;

        provider.deviceOrigin.SetLocalPose(currentCalibration.leapTracker.localPose);
        if (leftEye != null) {
          leftEye.eyePerspective.transform.localPosition = currentCalibration.leftEye.eyePosition;
          leftEyeProjectionParams = currentCalibration.leftEye.cameraProjection;
          if (leftEye.ellipse != null) {
            if (disableEllipsoids) leftEye.ellipse.enabled = false;
            leftEye.ellipse.MinorAxis = currentCalibration.leftEye.ellipseMinorAxis;
            leftEye.ellipse.MajorAxis = currentCalibration.leftEye.ellipseMajorAxis;
            leftEye.ellipse.sphereToWorldSpace = currentCalibration.leftEye.sphereToWorldSpace;
            leftEye.ellipse.worldToSphereSpace = currentCalibration.leftEye.sphereToWorldSpace.inverse;
          }
          if (leftEye.Screen != null) {
            Matrix4x4 screenTransform = currentCalibration.leftEye.worldToScreenSpace.inverse;
            leftEye.Screen.position = provider.transform.parent.TransformPoint(screenTransform.GetVector3());
            leftEye.Screen.rotation = provider.transform.parent.rotation * screenTransform.GetQuaternion();
            leftEye.Screen.localScale = screenTransform.lossyScale;
          }
        }
        if (rightEye != null) {
          rightEye.eyePerspective.transform.localPosition = currentCalibration.rightEye.eyePosition;
          rightEyeProjectionParams = currentCalibration.rightEye.cameraProjection;
          if (rightEye.ellipse != null) {
            if (disableEllipsoids) rightEye.ellipse.enabled = false;
            rightEye.ellipse.MinorAxis = currentCalibration.rightEye.ellipseMinorAxis;
            rightEye.ellipse.MajorAxis = currentCalibration.rightEye.ellipseMajorAxis;
            rightEye.ellipse.sphereToWorldSpace = currentCalibration.rightEye.sphereToWorldSpace;
            rightEye.ellipse.worldToSphereSpace = currentCalibration.rightEye.sphereToWorldSpace.inverse;
          }
          if (rightEye.Screen != null) {
            Matrix4x4 screenTransform = currentCalibration.rightEye.worldToScreenSpace.inverse;
            rightEye.Screen.position = provider.transform.parent.TransformPoint(screenTransform.GetVector3());
            rightEye.Screen.rotation = provider.transform.parent.rotation * screenTransform.GetQuaternion();
            rightEye.Screen.localScale = screenTransform.lossyScale;
          }
        }

        ARRaytracer[] raytracers = GetComponentsInChildren<ARRaytracer>();
        foreach (ARRaytracer raytracer in raytracers) {
          raytracer.ScheduleCreateDistortionMesh();
        }

        Debug.Log("Headset calibration successfully loaded from " + inputCalibrationFile);

        return true;
      } else {
        currentCalibration.leapTracker = new PhysicalComponent(provider.deviceOrigin.ToWorldPose(), provider.gameObject.name);
        if (leftEye != null && leftEye.ellipse != null &&
            leftEye.Screen != null) {
          currentCalibration.leftEye = constructLeftEyeOptics();
        }
        if (rightEye != null && rightEye.ellipse != null &&
            rightEye.Screen != null) {
          currentCalibration.rightEye = constructRightEyeOptics();
        }
        Debug.LogWarning("No calibration exists for: " + inputFile +
          "; no calibration was loaded.");
          
        return false;
      }
    }

    /// <summary>
    /// Loads the calibration from the positions of the objects in the scene and
    /// updates the calibration struct.
    /// </summary>
    public void UpdateCalibrationFromObjects(bool onlyOneEye = false, bool updateLeftEye = true) {
      currentCalibration.leapTracker = new PhysicalComponent(
        provider.deviceOrigin.ToLocalPose(), provider.gameObject.name);
      if (!onlyOneEye || (onlyOneEye && updateLeftEye)) {
        currentCalibration.leftEye = constructLeftEyeOptics();
        leftEye.ScheduleCreateDistortionMesh(true);
      }
      if (!onlyOneEye || (onlyOneEye && !updateLeftEye)) {
        currentCalibration.rightEye = constructRightEyeOptics();
        rightEye.ScheduleCreateDistortionMesh(true);
      }
    }

    private ReflectorOptics constructLeftEyeOptics(bool useDefaultEyes = false) {
      if (useDefaultEyes) leftEye.eyePerspective.transform.position = DEFAULT_LEFT_EYE;
      return new ReflectorOptics(
        leftEye.eyePerspective.transform.position,
        leftEye.ellipse,
        leftEye.Screen,
        leftEyeProjectionParams,
        headTransform != null ? headTransform.ToPose() : (Pose?)null,
        leftEye.eyePerspective.transform.rotation
      );
    }

    private ReflectorOptics constructRightEyeOptics(bool useDefaultEyes = false) {
      if (useDefaultEyes) rightEye.eyePerspective.transform.position = 
          Vector3.Scale(new Vector3(-1f, 1f, 1f), DEFAULT_LEFT_EYE);
      return new ReflectorOptics(
        rightEye.eyePerspective.transform.position,
        rightEye.ellipse,
        rightEye.Screen,
        rightEyeProjectionParams,
        headTransform != null ? headTransform.ToPose() : (Pose?)null,
        rightEye.eyePerspective.transform.rotation
      );
    }

    [System.Serializable]
    public struct HeadsetCalibration {
      public ReflectorOptics leftEye;
      public ReflectorOptics rightEye;
      public PhysicalComponent leapTracker;
    }

    [System.Serializable]
    public struct ReflectorOptics {
      
      public float ellipseMinorAxis;
      public float ellipseMajorAxis;
      public Vector3 screenForward;
      public Vector3 screenPosition;
      public Vector3 eyePosition;
      public Quaternion eyeRotation;
      public Vector4 cameraProjection;
      public Matrix4x4 sphereToWorldSpace;
      public Matrix4x4 worldToScreenSpace;

      public ReflectorOptics(Vector3 pupilPosition,
                             EllipsoidTransform ellipse,
                             Transform Screen,
                             Vector4 projectionParameters,
                             Pose? headsetOrigin = null,
                             Quaternion? optionalPupilRotation = null,
                             bool updateEllipsoid = true) {
        eyePosition = pupilPosition;

        bool didEllipsoidActuallyUpdate = false;
        if (updateEllipsoid) {
          didEllipsoidActuallyUpdate = ellipse.UpdateEllipsoid();
        }
        sphereToWorldSpace = ellipse.sphereToWorldSpace;

        ellipseMajorAxis = ellipse.MajorAxis;
        ellipseMinorAxis = ellipse.MinorAxis;

        screenForward = Screen.forward;
        screenPosition = Screen.position;
        worldToScreenSpace = Screen.worldToLocalMatrix;
        cameraProjection = projectionParameters;
        
        eyeRotation = Quaternion.identity;
        if (optionalPupilRotation.HasValue) {
          var pupilRotation = optionalPupilRotation.Value;
          if (optionalPupilRotation == default(Quaternion)) { 
            optionalPupilRotation = Quaternion.identity;
          }
          eyeRotation = pupilRotation;
        }
        
        if (headsetOrigin.HasValue) {
          // If debugging this, helps to draw matrices with:
          // var drawer = HyperMegaStuff.HyperMegaLines.drawer;
          // (new Geometry.Sphere(radius)).DrawLines(
          //   drawer.DrawLine,
          //   overrideMatrix: aLocalToWorldMatrix);
          var headsetWorldToLocal = headsetOrigin.Value.inverse.matrix;
          eyePosition = headsetWorldToLocal.MultiplyPoint3x4(eyePosition);
          eyeRotation = OpticalCalibrationManager.LossyMatrixMultQuaternion(
            headsetWorldToLocal, eyeRotation 
          );
          screenForward = headsetWorldToLocal.MultiplyVector(Screen.forward)
            .normalized;
          screenPosition = headsetWorldToLocal.MultiplyPoint3x4(Screen.position);
          worldToScreenSpace = (headsetWorldToLocal * Screen.localToWorldMatrix)
            .inverse;
          if (didEllipsoidActuallyUpdate) {
            sphereToWorldSpace = headsetWorldToLocal * sphereToWorldSpace;
          }
        }
      }

      public static implicit operator ARRaytracer.OpticalSystem(ReflectorOptics curOptics) {
        ARRaytracer.OpticalSystem optics = new ARRaytracer.OpticalSystem();
        optics.ellipseMinorAxis = curOptics.ellipseMinorAxis;
        optics.ellipseMajorAxis = curOptics.ellipseMajorAxis;
        optics.screenForward = curOptics.screenForward;
        optics.screenPosition = curOptics.screenPosition;
        optics.eyePosition = curOptics.eyePosition;
        optics.sphereToWorldSpace = curOptics.sphereToWorldSpace;
        optics.worldToSphereSpace = curOptics.sphereToWorldSpace.inverse;
        optics.worldToScreenSpace = curOptics.worldToScreenSpace;

        if (curOptics.eyeRotation.ApproxEquals(default(Quaternion))) {
          curOptics.eyeRotation = Quaternion.identity;
        }
        Matrix4x4 eyeToWorld = Matrix4x4.TRS(
          curOptics.eyePosition,
          curOptics.eyeRotation,
          Vector3.one
        );
        // Unity convention for "camera forward" matches OpenGL: _negative_ Z.
        eyeToWorld.m02 *= -1;
        eyeToWorld.m12 *= -1;
        eyeToWorld.m22 *= -1;
        optics.clipToWorld = eyeToWorld *
          curOptics.cameraProjection.ComposeProjection().inverse;
        return optics;
      }

      public override string ToString() {
        return
          "ellipseMinorAxis: " + ellipseMinorAxis +
          "\nellipseMajorAxis: " + ellipseMajorAxis +
          "\nscreenForward: " + screenForward.ToString("R") +
          "\nscreenPosition: " + screenPosition.ToString("R") +
          "\neyePosition: " + eyePosition.ToString("R") +
          "\ncameraProjection: " + cameraProjection.ToString("R") +
          "\nworldToSphereSpace: \n" + sphereToWorldSpace.inverse +
          "sphereToWorldSpace: \n" + sphereToWorldSpace +
          "worldToScreenSpace: \n" + worldToScreenSpace;
      }
    }

    [System.Serializable]
    public struct PhysicalComponent {
      public string name;
      public string serial;
      public Pose localPose;
      public PhysicalComponent(Pose localPose, string name = "", string serial = "") {
        this.name = name;
        this.serial = serial;
        this.localPose = localPose;
      }
    }

    //Using this special struct for the serialization of SteamVR compatible .json
    [System.Serializable]
    public struct SteamVROptics {
      public float ellipseMinorAxis;
      public float ellipseMajorAxis;
      public float screenForward_x, screenForward_y, screenForward_z;
      public float screenPosition_x, screenPosition_y, screenPosition_z;
      public float eyePosition_x, eyePosition_y, eyePosition_z;
      public float cameraProjection_x, cameraProjection_y, cameraProjection_z, cameraProjection_w;
      public float sphereToWorldSpace_e00, sphereToWorldSpace_e01, sphereToWorldSpace_e02, sphereToWorldSpace_e03,
                   sphereToWorldSpace_e10, sphereToWorldSpace_e11, sphereToWorldSpace_e12, sphereToWorldSpace_e13,
                   sphereToWorldSpace_e20, sphereToWorldSpace_e21, sphereToWorldSpace_e22, sphereToWorldSpace_e23;
      public float worldToScreenSpace_e00, worldToScreenSpace_e01, worldToScreenSpace_e02, worldToScreenSpace_e03,
                   worldToScreenSpace_e10, worldToScreenSpace_e11, worldToScreenSpace_e12, worldToScreenSpace_e13,
                   worldToScreenSpace_e20, worldToScreenSpace_e21, worldToScreenSpace_e22, worldToScreenSpace_e23;
      public static implicit operator ReflectorOptics(SteamVROptics curOptics) {
        ReflectorOptics optics = new ReflectorOptics();
        optics.ellipseMinorAxis = curOptics.ellipseMinorAxis;
        optics.ellipseMajorAxis = curOptics.ellipseMajorAxis;
        optics.screenForward = new Vector3(curOptics.screenForward_x, curOptics.screenForward_y, curOptics.screenForward_z);
        optics.screenPosition = new Vector3(curOptics.screenPosition_x, curOptics.screenPosition_y, curOptics.screenPosition_z);
        optics.eyePosition = new Vector3(curOptics.eyePosition_x, curOptics.eyePosition_y, curOptics.eyePosition_z);

        optics.sphereToWorldSpace = new Matrix4x4();
        optics.sphereToWorldSpace.m00 = curOptics.sphereToWorldSpace_e00; optics.sphereToWorldSpace.m01 = curOptics.sphereToWorldSpace_e01; optics.sphereToWorldSpace.m02 = curOptics.sphereToWorldSpace_e02; optics.sphereToWorldSpace.m03 = curOptics.sphereToWorldSpace_e03;
        optics.sphereToWorldSpace.m10 = curOptics.sphereToWorldSpace_e10; optics.sphereToWorldSpace.m11 = curOptics.sphereToWorldSpace_e11; optics.sphereToWorldSpace.m12 = curOptics.sphereToWorldSpace_e12; optics.sphereToWorldSpace.m13 = curOptics.sphereToWorldSpace_e13;
        optics.sphereToWorldSpace.m20 = curOptics.sphereToWorldSpace_e20; optics.sphereToWorldSpace.m21 = curOptics.sphereToWorldSpace_e21; optics.sphereToWorldSpace.m22 = curOptics.sphereToWorldSpace_e22; optics.sphereToWorldSpace.m23 = curOptics.sphereToWorldSpace_e23;
        optics.sphereToWorldSpace.m30 = 0f; optics.sphereToWorldSpace.m31 = 0f; optics.sphereToWorldSpace.m32 = 0f; optics.sphereToWorldSpace.m33 = 1f;

        optics.worldToScreenSpace = new Matrix4x4();
        optics.worldToScreenSpace.m00 = curOptics.worldToScreenSpace_e00; optics.worldToScreenSpace.m01 = curOptics.worldToScreenSpace_e01; optics.worldToScreenSpace.m02 = curOptics.worldToScreenSpace_e02; optics.worldToScreenSpace.m03 = curOptics.worldToScreenSpace_e03;
        optics.worldToScreenSpace.m10 = curOptics.worldToScreenSpace_e10; optics.worldToScreenSpace.m11 = curOptics.worldToScreenSpace_e11; optics.worldToScreenSpace.m12 = curOptics.worldToScreenSpace_e12; optics.worldToScreenSpace.m13 = curOptics.worldToScreenSpace_e13;
        optics.worldToScreenSpace.m20 = curOptics.worldToScreenSpace_e20; optics.worldToScreenSpace.m21 = curOptics.worldToScreenSpace_e21; optics.worldToScreenSpace.m22 = curOptics.worldToScreenSpace_e22; optics.worldToScreenSpace.m23 = curOptics.worldToScreenSpace_e23;
        optics.worldToScreenSpace.m30 = 0f; optics.worldToScreenSpace.m31 = 0f; optics.worldToScreenSpace.m32 = 0f; optics.worldToScreenSpace.m33 = 1f;

        optics.cameraProjection = new Vector4(curOptics.cameraProjection_x, curOptics.cameraProjection_y, curOptics.cameraProjection_z, curOptics.cameraProjection_w);

        return optics;
      }

      public static implicit operator SteamVROptics(ReflectorOptics curOptics) {
        SteamVROptics optics = new SteamVROptics();
        optics.ellipseMinorAxis = curOptics.ellipseMinorAxis;
        optics.ellipseMajorAxis = curOptics.ellipseMajorAxis;
        optics.screenForward_x = curOptics.screenForward.x; optics.screenForward_y = curOptics.screenForward.y; optics.screenForward_z = curOptics.screenForward.z;
        optics.screenPosition_x = curOptics.screenPosition.x; optics.screenPosition_y = curOptics.screenPosition.y; optics.screenPosition_z = curOptics.screenPosition.z;
        optics.eyePosition_x = curOptics.eyePosition.x; optics.eyePosition_y = curOptics.eyePosition.y; optics.eyePosition_z = curOptics.eyePosition.z;

        optics.cameraProjection_x = curOptics.cameraProjection.x;
        optics.cameraProjection_y = curOptics.cameraProjection.y;
        optics.cameraProjection_z = curOptics.cameraProjection.w; // Swap Z and W
        optics.cameraProjection_w = curOptics.cameraProjection.z; // (Unity<->Steam)

        optics.sphereToWorldSpace_e00 = curOptics.sphereToWorldSpace.m00; optics.sphereToWorldSpace_e01 = curOptics.sphereToWorldSpace.m01; optics.sphereToWorldSpace_e02 = curOptics.sphereToWorldSpace.m02; optics.sphereToWorldSpace_e03 = curOptics.sphereToWorldSpace.m03;
        optics.sphereToWorldSpace_e10 = curOptics.sphereToWorldSpace.m10; optics.sphereToWorldSpace_e11 = curOptics.sphereToWorldSpace.m11; optics.sphereToWorldSpace_e12 = curOptics.sphereToWorldSpace.m12; optics.sphereToWorldSpace_e13 = curOptics.sphereToWorldSpace.m13;
        optics.sphereToWorldSpace_e20 = curOptics.sphereToWorldSpace.m20; optics.sphereToWorldSpace_e21 = curOptics.sphereToWorldSpace.m21; optics.sphereToWorldSpace_e22 = curOptics.sphereToWorldSpace.m22; optics.sphereToWorldSpace_e23 = curOptics.sphereToWorldSpace.m23;

        optics.worldToScreenSpace_e00 = curOptics.worldToScreenSpace.m00; optics.worldToScreenSpace_e01 = curOptics.worldToScreenSpace.m01; optics.worldToScreenSpace_e02 = curOptics.worldToScreenSpace.m02; optics.worldToScreenSpace_e03 = curOptics.worldToScreenSpace.m03;
        optics.worldToScreenSpace_e10 = curOptics.worldToScreenSpace.m10; optics.worldToScreenSpace_e11 = curOptics.worldToScreenSpace.m11; optics.worldToScreenSpace_e12 = curOptics.worldToScreenSpace.m12; optics.worldToScreenSpace_e13 = curOptics.worldToScreenSpace.m13;
        optics.worldToScreenSpace_e20 = curOptics.worldToScreenSpace.m20; optics.worldToScreenSpace_e21 = curOptics.worldToScreenSpace.m21; optics.worldToScreenSpace_e22 = curOptics.worldToScreenSpace.m22; optics.worldToScreenSpace_e23 = curOptics.worldToScreenSpace.m23;

        return optics;
      }

      public static string getSteamVRPrefixString() {
        return "{\r\n" +
          "  \"driver_northstar\": {\r\n" +
          "    \"headsetwindowX\": -1,\r\n" +
          "    \"headsetwindowY\": 0,\r\n" +
          "    \"headsetwindowidth\": 2880,\r\n" +
          "    \"headsetwindowheight\": 1600,\r\n" +
          "    \"headsetrenderwidth\": 1440,\r\n" +
          "    \"headsetrenderheight\": 1600,\r\n" +
          "    \"headsetfrequency\": 120,\r\n" +
          "    \"enable_headtracking\": true,\r\n" +
          "    \"enable_eyetracking\": false,\r\n" +
          "    \"enable_handtracking\": false,\r\n" +
          "    \"headposetimeoffset\": 17,\r\n" +
          "    \"used_devicerotation_x\": 0.7,\r\n" +
          "    \"used_devicerotation_y\": 0.0,\r\n" +
          "    \"used_devicerotation_z\": 0.0,\r\n" +
          "    \"deviceheadoffset_x\": 0,\r\n" +
          "    \"deviceheadoffset_y\": -0.0770678,\r\n" +
          "    \"deviceheadoffset_z\": 0.02598797,\r\n" +
          "    \"eyetrackeroffset_x\": 0,\r\n" +
          "    \"eyetrackeroffset_y\": 0.02725284,\r\n" +
          "    \"eyetrackeroffset_z\": 0.0465651,\r\n" +
          "    \"eyetrackerrotation_x\": 0.87,\r\n" +
          "    \"eyetrackerrotation_y\": 3.14159,\r\n" +
          "    \"eyetrackerrotation_z\": 0.0,\r\n" +
          "    \"recalc_ipd\": false,\r\n" +
          "    \"ipd\": 0.0635,\r\n" +
          "    \"verbose\": true,\r\n" +
          "    \"eyesmoothing\": 20,\r\n" +
          "    \"eyemaxerror\": 3.0,\r\n" +
          "    \"photonlatency\": 0.035,\r\n" +
          "    \"initsolveriters\": 50,\r\n" +
          "    \"optsolveriters\": 50\r\n" +
          "  },\r\n" +
          "  \"alwaysActivate\": true,\r\n" +
          "  \"name\": \"northstar\",\r\n" +
          "  \"date\" : \"" + DateTime.Today.Year + "-" + DateTime.Today.Month + "-" + DateTime.Today.Day + "\",\r\n" +
          "  \"directory\": \"\",\r\n" +
          "  \"resourceOnly\": false,\r\n" +
          "  \"hmd_presence\": [ \"*.*\" ],";
      }

    }

    [System.Serializable]
    public struct SteamVRHeadsetCalibration {
      public SteamVROptics leftEye;
      public SteamVROptics rightEye;
    }

    public static Matrix4x4 MatrixFromQuaternion(Quaternion q) {
      return Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
      return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    /// <summary>
    /// Computes "matrix * quaternion" and returns the resulting quaternion,
    /// by (lossily) converting the quaternion to a rotation matrix and back.
    /// </summary>
    public static Quaternion LossyMatrixMultQuaternion(Matrix4x4 m,
                                                       Quaternion q) {
      var rotM = MatrixFromQuaternion(q);
      var result = rotM * m;
      return QuaternionFromMatrix(result);
    }

  }

  public static class ProjectionExtension {
    //Creates a projection matrix from the tangent of the half angles from the center of the left, right, top, and bottom of the frustum.
    /*public static Matrix4x4 PerspectiveOffCenter(this Vector4 tangentHalfAngles, float near = 0.07f, float far = 1000f) {
      float x = (2.0F * near) / (tangentHalfAngles.y - tangentHalfAngles.x);
      float y = (2.0F * near) / (tangentHalfAngles.z - tangentHalfAngles.w);
      float a = (tangentHalfAngles.y + tangentHalfAngles.x) / (tangentHalfAngles.y - tangentHalfAngles.x);
      float b = (tangentHalfAngles.z + tangentHalfAngles.w) / (tangentHalfAngles.z - tangentHalfAngles.w);
      float c = -(far + near) / (far - near);
      float d = -(2.0F * far * near) / (far - near);
      float e = -1.0F;
      Matrix4x4 m = new Matrix4x4();
      m[0, 0] = x; m[0, 1] = 0; m[0, 2] = a; m[0, 3] = 0;
      m[1, 0] = 0; m[1, 1] = y; m[1, 2] = b; m[1, 3] = 0;
      m[2, 0] = 0; m[2, 1] = 0; m[2, 2] = c; m[2, 3] = d;
      m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = e; m[3, 3] = 0;
      return m;
    }*/

    public static Matrix4x4 ComposeProjection(this Vector4 tangentHalfAngles,
                                              float zNear = 0.07f, 
                                              float zFar = 1000f) {
      float fLeft = tangentHalfAngles.x;
      float fRight = tangentHalfAngles.y;
      float fTop = tangentHalfAngles.w;
      float fBottom = tangentHalfAngles.z;
      
      float idx = 1.0f / (fRight - fLeft);
      float idy = 1.0f / (fBottom - fTop);
      //float idz = 1.0f / (zFar - zNear);
      float sx = fRight + fLeft;
      float sy = fBottom + fTop;

      float c = -(zFar + zNear) / (zFar - zNear);
      float d = -(2.0F * zFar * zNear) / (zFar - zNear);

      Matrix4x4 m = new Matrix4x4();
      m[0,0] = 2 * idx; m[0,1] = 0;       m[0,2] = sx * idx; m[0,3] = 0;
      m[1,0] = 0;       m[1,1] = 2 * idy; m[1,2] = sy * idy; m[1,3] = 0;
      m[2,0] = 0;       m[2,1] = 0;       m[2,2] = c;        m[2,3] = d;
      m[3,0] = 0;       m[3,1] = 0;       m[3,2] = -1.0f;    m[3,3] = 0;
      //m[2,2] = -zFar * idz; m[2,3] = -zFar * zNear * idz;
      return m;
    }
  }
}
