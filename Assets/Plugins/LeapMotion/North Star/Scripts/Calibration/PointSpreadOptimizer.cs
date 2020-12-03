using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using Leap.Unity.Attributes;
using Leap;
using LeapInternal;
using Leap.Unity.Interaction;

namespace Leap.Unity.AR {

  public class PointSpreadOptimizer : MonoBehaviour, IRuntimeGizmoComponent {

    [QuickButton("Solve", "Solve")]
    public ARRaytracer raytracer;
    public OpticalCalibrationManager manager;
    public Transform newScreen;

    [Range(0.1f, 2f)]
    public float focalDistance = 0.5f;
    [Range(0.001f, 0.01f)]
    public float pupilDiameter = 0.001f;
    [Range(0, 400)]
    public int whichToDraw = 2;

    Vector3 planePos, planeNormal;
    [NonSerialized]
    public static List<Vector3> intersectionPoints;

    public Testing.DenseOptimizer.TransformEntry[] TransformsToOptimize = new Testing.DenseOptimizer.TransformEntry[3];
    public bool isLeft;
    public float rotationUnitRatio = 750f;
    [Range(0.00025f, 0.025f)]
    public float simplexSize = 0.025f;
    public KeyCode key;

    NelderMead solver;

    float startingDist;

    public void Solve() {
      startingDist = Vector3.Distance(raytracer.eyePerspective.transform.position, planePos);

      #if UNITY_EDITOR
      UnityEditor.Undo.RecordObject(newScreen, "Move Screen Position");
      foreach(Testing.DenseOptimizer.TransformEntry trans in TransformsToOptimize) UnityEditor.Undo.RecordObject(trans.TransformToOptimize, "Optimize Transform");
      #endif

      solver = new NelderMead(Testing.DenseOptimizer.calculateCoordsFromTransformEntries(ref TransformsToOptimize, rotationUnitRatio), costFunction, simplexSize);
      for(int i = 0; i < 100; i++) solver.stepSolver();

      Testing.DenseOptimizer.setTransformsFromCoord(solver.simplexVertices[0].coordinates, ref TransformsToOptimize, rotationUnitRatio);

      newScreen.position = Vector3.ProjectOnPlane(newScreen.position - planePos, planeNormal) + planePos;
      newScreen.rotation = Quaternion.FromToRotation(newScreen.forward, planeNormal) * newScreen.rotation;

      //Debug.Log("First: " + solver.simplexVertices[0].cost + ", Last: " + solver.simplexVertices[solver.simplexVertices.Count - 1].cost);
    }

    public float costFunction(float[] coordinate) {
      Testing.DenseOptimizer.setTransformsFromCoord(coordinate, ref TransformsToOptimize, rotationUnitRatio);
      manager.UpdateCalibrationFromObjects(true, true);

      float focalCost = evaluateFocalPlane();
      float currentDist = Vector3.Distance(raytracer.eyePerspective.transform.position, planePos);

      return focalCost + ((currentDist < startingDist) ? 100f : 0f);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) { evaluateFocalPlane(drawer); }

    public float evaluateFocalPlane(RuntimeGizmoDrawer drawer = null) {
      intersectionPoints = new List<Vector3>();

      raytracer.manager.UpdateCalibrationFromObjects(true, true);
      ARRaytracer.OpticalSystem optics = raytracer.optics;

      int curDrawing = 0;
      for (float v = 0f; v < 1.1f; v += 0.1f) {
        for (float u = 0f; u < 1.1f; u += 0.1f) {
          List<Ray> intersectRayList = new List<Ray>();

          Vector3 convergencePoint = raytracer.eyePerspective.ViewportToWorldPoint(new Vector3(u, v, focalDistance));
          if (drawer != null) { drawer.color = LeapColor.electricBlue; drawer.DrawSphere(convergencePoint, 0.005f); }

          optics.eyePosition = transform.position + (transform.right * (pupilDiameter * 0.5f));
          Ray firstBounceRay = PointSpreadTracer.traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
          intersectRayList.Add(firstBounceRay);

          optics.eyePosition = transform.position + (transform.right * -(pupilDiameter * 0.5f));
          firstBounceRay = PointSpreadTracer.traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
          intersectRayList.Add(firstBounceRay);

          optics.eyePosition = transform.position + (transform.up * (pupilDiameter * 0.5f));
          firstBounceRay = PointSpreadTracer.traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
          intersectRayList.Add(firstBounceRay);

          optics.eyePosition = transform.position + (transform.up * -(pupilDiameter * 0.5f));
          firstBounceRay = PointSpreadTracer.traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
          intersectRayList.Add(firstBounceRay);

          Vector3 leftRight = PointSpreadTracer.Fit.ClosestPointOnRayToRay(intersectRayList[0], intersectRayList[1]);
          Vector3 topBottom = PointSpreadTracer.Fit.ClosestPointOnRayToRay(intersectRayList[2], intersectRayList[3]);
          if (drawer != null) {
            drawer.color = LeapColor.coral; drawer.DrawSphere(leftRight, 0.0005f);
            drawer.color = LeapColor.forest; drawer.DrawSphere(topBottom, 0.0005f);
          }

          intersectionPoints.Add(leftRight); intersectionPoints.Add(topBottom);
          curDrawing++;
        }
      }

      optics.eyePosition = raytracer.eyePerspective.transform.position;

      Vector3 position = Vector3.zero; Vector3 normal = Vector3.zero;
      return PointSpreadTracer.Fit.Plane(intersectionPoints, out planePos, out planeNormal, 200, drawer);
    }
  }
}
