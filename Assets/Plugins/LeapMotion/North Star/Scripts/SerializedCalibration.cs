/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.AR {

  using Pose = Leap.Unity.Pose;

  public class SerializedCalibration : MonoBehaviour {
    
    [Tooltip("This is a folder in StreamingAssets where calibrations are kept.")]
    public StreamingFolder calibrationsFolder;
    
    [Tooltip("Press this key to save a new calibration file specified by the file name "
           + "into the calibrations folder.")]
    public KeyCode saveCalibrationKey = KeyCode.S;

    [Tooltip("If this option is checked, the Ctrl key must be held down to save a calibration.")]
    public bool requireCtrlHeld = true;

    [QuickButton("Save Now", "SaveCurrentCalibration")]
    public string outputCalibrationFile = "outputCalibration.json";

    [QuickButton("Load Now", "LoadCalibration")]
    public string inputCalibrationFile = "defaultCalibration.json";

    public bool loadInputFileOnStart = true;
    public bool allowSavingInBuild = true;

    [Header("Headset Calibration Map")]
    /// <summary>
    /// The Scene-serialized pairing between calibration names and Transforms.
    /// </summary>
    public StringTransformDictionary calibratedComponents = new StringTransformDictionary();
    [System.Serializable]
    public class StringTransformDictionary : SerializableDictionary<string, Transform> { }

    /// <summary>
    /// The destination file path of the calibration file if SaveCurrentCalibration() is
    /// called. (Read only.)
    /// </summary>
    public string outputFilePath {
      get {
        return Path.Combine(calibrationsFolder.Path, outputCalibrationFile);
      }
    }

    /// <summary>
    /// The path of the calibration file to load if LoadCalibration() is called.
    /// (Read only.)
    /// </summary>
    public string inputFilePath {
      get {
        return Path.Combine(calibrationsFolder.Path, inputCalibrationFile);
      }
    }

    void Start() {
      // Scan command line environments for a headset calibration file specification
      // and set that as the input file if found.
      string[] args = System.Environment.GetCommandLineArgs();
      foreach (string arg in args) {
        if (arg.Contains("--headsetCalibration=")) {
          inputCalibrationFile = arg.Substring(21);
        }
      }
      
      if (loadInputFileOnStart) {
        LoadCalibration();
      }
    }

    void Update() {
      if ((Application.isEditor|| allowSavingInBuild) && Input.GetKeyDown(saveCalibrationKey)
          && (!requireCtrlHeld // Optionally require "Ctrl" key held down
              || (Input.GetKey(KeyCode.LeftControl)
              || Input.GetKey(KeyCode.RightControl)))) {
        SaveCurrentCalibration();
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

      if (string.IsNullOrEmpty(outputCalibrationFile)||(!Application.isEditor)) {
        outputCalibrationFile = "ARCalibration - " + Random.Range(0, 10000)+".json";
        Debug.LogWarning("outputCalibrationFile was null or empty; defaulting to "
                         + "'ARCalibration - XXXX.json'.", this);
      }

      List<CalibrationComponent> calibrationList = new List<CalibrationComponent>();
      foreach (var nameTransformPair in calibratedComponents) {
        var name = nameTransformPair.Key;
        var transform = nameTransformPair.Value;

        CalibrationDeformer deformer = nameTransformPair.Value.GetComponent<CalibrationDeformer>();
        if (deformer == null) {
          calibrationList.Add(new CalibrationComponent(name,
                                                       transform.ToLocalPose(),
                                                       transform.localScale));
        } else {
          calibrationList.Add(new CalibrationComponent(name,
                                                       transform.ToLocalPose(),
                                                       transform.localScale,
                                                       deformer.vertexIndices,
                                                       deformer.controlPoints));
        }
      }

      var outputPath = outputFilePath;
      File.WriteAllText(outputPath,
        JsonUtility.ToJson(new ListWrapper<CalibrationComponent>(calibrationList),
                           prettyPrint: true));
      Debug.Log("Saved current calibration to: " + outputPath);
    }

    /// <summary>
    /// Loads the calibration file specified by the inputCalibrationFile field, which
    /// </summary>
    public void LoadCalibration() {
      if (!Application.isPlaying) {
        Debug.LogError("For safety, calibrations cannot be loaded at edit-time. "
                     + "Enter play mode to load a calibration.", this);
        return;
      }

      if (string.IsNullOrEmpty(inputCalibrationFile)) {
        Debug.LogError("inputCalibrationFile field is null or empty; cannot load "
                     + "a calibration file.", this);
      }

      var inputFile = inputFilePath;

      if (File.Exists(inputFile)) {
        string calibrationData = File.ReadAllText(inputFile);
        ListWrapper<CalibrationComponent> serializableList
          = JsonUtility.FromJson<ListWrapper<CalibrationComponent>>(calibrationData);

        foreach (CalibrationComponent calib in serializableList.list) {
          Transform componentTransform = calibratedComponents[calib.name];
          componentTransform.localPosition = calib.localPose.position;
          componentTransform.localRotation = calib.localPose.rotation;
          componentTransform.localScale = calib.localScale;

          CalibrationDeformer deformer = componentTransform.GetComponent<CalibrationDeformer>();
          if (deformer != null) {
            deformer.vertexIndices = calib.vertexIndices;
            deformer.controlPoints = calib.controlPoints;
          }
        }
        ARRaytracer[] raytracers = GetComponentsInChildren<ARRaytracer>();
        foreach (ARRaytracer raytracer in raytracers) {
          raytracer.ScheduleCreateDistortionMesh();
        }

        Debug.Log("Headset calibration successfully loaded from " + inputCalibrationFile);
      }
      else {
        Debug.LogWarning("No calibration exists for: " + inputFile + "; no calibration "
                       + "was loaded.");
      }
    }

    [System.Serializable]
    public struct CalibrationComponent {
      public string name;
      public Pose localPose;
      public Vector3 localScale;
      public List<int> vertexIndices;
      public List<Vector3> controlPoints;
      public CalibrationComponent(string name, Pose localPose, Vector3 localScale) {
        this.name = name;
        this.localPose = localPose;
        this.localScale = localScale;
        vertexIndices = new List<int>(0);
        controlPoints = new List<Vector3>(0);
      }
      public CalibrationComponent(string name, Pose localPose, Vector3 localScale, List<int> vertexIndices, List<Vector3> controlPoints) {
        this.name = name;
        this.localPose = localPose;
        this.localScale = localScale;
        this.vertexIndices = vertexIndices;
        this.controlPoints = controlPoints;
      }
    }

    [System.Serializable]
    public struct ListWrapper<T> {
      public List<T> list;
      public ListWrapper(List<T> list) {
        this.list = list;
      }
    }
  }

}
