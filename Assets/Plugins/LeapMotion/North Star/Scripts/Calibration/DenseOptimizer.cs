using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;
using Leap;
using LeapInternal;
using Leap.Unity.Interaction;

namespace Leap.Unity.AR.Testing {
  public class DenseOptimizer : MonoBehaviour {
    [QuickButton("Start Solve", "StartSolve")]
    public ARRaytracer raytracer;
    [QuickButton("Stop Solve", "StopSolve")]
    public WebcamScreenCalibration calibration;
    public OpticalCalibrationManager manager;
    public TransformEntry[] TransformsToOptimize = new TransformEntry[3];
    public bool isLeft;
    public float rotationUnitRatio = 750f;
    [Range(0.00025f, 0.025f)]
    public float simplexSize = 0.025f;
    public KeyCode key;

    [NonSerialized]
    public NelderMeadRoutine solver;

    [Serializable]
    public struct ListWrapper {
      public List<Vector4> data;
    }

    [Serializable]
    public struct TransformEntry {
      public Transform TransformToOptimize;
      public bool OptimizePositionX;
      public bool OptimizePositionY;
      public bool OptimizePositionZ;
      public bool OptimizeRotationX;
      public bool OptimizeRotationY;
      public bool OptimizeRotationZ;
      [HideInInspector]
      public Vector3 originalLocalPosition;
      [HideInInspector]
      public Quaternion originalLocalRotation;
    }

    public void ToggleSolve() {
      if (solver != null) {
        StopSolve();
      } else {
        StartSolve();
      }
    }
    public void StartSolve() {
      if (solver == null) { StartCoroutine(StartNelderMead()); }
    }
    public void StopSolve() {
      if (solver != null) { calibration.StopAllCoroutines(); StopAllCoroutines(); solver = null; }
    }

    IEnumerator StartNelderMead() {
      solver = new NelderMeadRoutine();
      solver.isLeft = isLeft;
      solver.steppingSolver = true;
      yield return StartCoroutine(
        solver.initializeNelderMeadRoutine(
          calculateCoordsFromTransformEntries(ref TransformsToOptimize, rotationUnitRatio),
            setTransforms, measureCost, this, simplexSize));
    }

    void Update() {
      /* //This keeps the simplex from becoming degenerate
      solver.constructRightAngleSimplex(solver.simplexVertices[0].coordinates, 0.001f);
      for (int i = 0; i < 10; i++) solver.stepSolver(); */
      //if (Input.GetKeyDown(key)) ToggleSolve();
      if (solver != null && !solver.steppingSolver) {
        StartCoroutine(solver.stepSolver());
      }
    }

    public static float[] calculateCoordsFromTransformEntries(ref TransformEntry[] TransformsToOptimize, float rotationUnitRatio) {
      int numDimensions = 0;
      for (int i = 0; i < TransformsToOptimize.Length; i++) {
        if (TransformsToOptimize[i].OptimizePositionX) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionY) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionZ) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizeRotationX) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizeRotationY) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizeRotationZ) { numDimensions++; }
        TransformsToOptimize[i].originalLocalPosition = TransformsToOptimize[i].TransformToOptimize.localPosition;
        TransformsToOptimize[i].originalLocalRotation = TransformsToOptimize[i].TransformToOptimize.localRotation;
      }
      float[] initialCoord = new float[numDimensions];
      int curIndex = 0;
      for (int i = 0; i < TransformsToOptimize.Length; i++) {
        if (TransformsToOptimize[i].OptimizePositionX) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalPosition.x; }
        if (TransformsToOptimize[i].OptimizePositionY) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalPosition.y; }
        if (TransformsToOptimize[i].OptimizePositionZ) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalPosition.z; }
        if (TransformsToOptimize[i].OptimizeRotationX) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalRotation.eulerAngles[0] / rotationUnitRatio; }
        if (TransformsToOptimize[i].OptimizeRotationY) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalRotation.eulerAngles[1] / rotationUnitRatio; }
        if (TransformsToOptimize[i].OptimizeRotationZ) { initialCoord[curIndex++] = TransformsToOptimize[i].originalLocalRotation.eulerAngles[2] / rotationUnitRatio; }
      }
      return initialCoord;
    }

    //These two together evaluate the cost associated with a coordinate in parameter space
    public void setTransforms(float[] coordinate) {
      //SET THE POSITIONS OF THE COMPONENTS
      setTransformsFromCoord(coordinate, ref TransformsToOptimize, rotationUnitRatio);

      int deviceIndex = solver.isBottomRigel ? 1 : 0;

      // Since one camera is upside down, its left/right position is flipped!
      Transform leftCamera = solver.isBottomRigel ? calibration.calibrationDevices[deviceIndex].RightCamera : 
                                                    calibration.calibrationDevices[deviceIndex].LeftCamera;
      Transform rightCamera = solver.isBottomRigel ? calibration.calibrationDevices[deviceIndex].LeftCamera :
                                                     calibration.calibrationDevices[deviceIndex].RightCamera;

      if (isLeft) {
        manager.leftEye.transform.position = leftCamera.position;
      } else {
        manager.rightEye.transform.position = rightCamera.position;
      }

      manager.UpdateCalibrationFromObjects(true, isLeft);
    }
    //YIELD RETURN WAIT 0.1 SECONDS
    //GIVE TIME FOR THE CAMERA TO UPDATE
    public float measureCost() {
      int deviceIndex = solver.isBottomRigel ? 1 : 0;

      // Since one camera is upside down, its left/right cost is flipped!
      var LeftImageMetrics = solver.isBottomRigel ? calibration.calibrationDevices[deviceIndex].rightImageMetrics :
                                                    calibration.calibrationDevices[deviceIndex].leftImageMetrics;
      var RightImageMetrics = solver.isBottomRigel ? calibration.calibrationDevices[deviceIndex].leftImageMetrics :
                                                     calibration.calibrationDevices[deviceIndex].rightImageMetrics;

      float leftCost, rightCost;
      if (calibration.calculateSumOfDeviation) {
        leftCost = LeftImageMetrics.totalMaskDeviation;
        rightCost = RightImageMetrics.totalMaskDeviation;
      } else {
        leftCost = (float)LeftImageMetrics.sum;
        rightCost = (float)RightImageMetrics.sum;
      }

      float cost = isLeft ? leftCost : rightCost;
      if (!calibration.calculateSumOfDeviation) cost = -cost;
      //Debug.Log("Measuring cost: " + cost);
      return cost;
    }

    public static void setTransformsFromCoord(float[] coord, ref TransformEntry[] TransformsToOptimize, float rotationUnitRatio) {
      int curIndex = 0;
      for (int i = 0; i < TransformsToOptimize.Length; i++) {
        Vector3 origPos = TransformsToOptimize[i].TransformToOptimize.localPosition;
        TransformsToOptimize[i].TransformToOptimize.localPosition = new Vector3(
          TransformsToOptimize[i].OptimizePositionX ? coord[curIndex++] : origPos.x,
          TransformsToOptimize[i].OptimizePositionY ? coord[curIndex++] : origPos.y,
          TransformsToOptimize[i].OptimizePositionZ ? coord[curIndex++] : origPos.z);

        Vector3 origRot = TransformsToOptimize[i].TransformToOptimize.localRotation.eulerAngles;
        TransformsToOptimize[i].TransformToOptimize.localRotation = Quaternion.Euler(new Vector3(
          TransformsToOptimize[i].OptimizeRotationX ? coord[curIndex++] * rotationUnitRatio : origRot.x,
          TransformsToOptimize[i].OptimizeRotationY ? coord[curIndex++] * rotationUnitRatio : origRot.y,
          TransformsToOptimize[i].OptimizeRotationZ ? coord[curIndex++] * rotationUnitRatio : origRot.z));

        EllipsoidTransform ellipsoid;
        if ((ellipsoid = TransformsToOptimize[i].TransformToOptimize.GetComponent<EllipsoidTransform>()) != null) {
          ellipsoid.UpdateEllipsoid();
        }
      }
    }
  }
}