using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.AR.Testing {
  public class RayOptimizer : MonoBehaviour, IRuntimeGizmoComponent {
    public ARRaytracer raytracer;

    public TransformEntry[] TransformsToOptimize = new TransformEntry[3];

    public Transform screenSpace;
    public float rotationUnitRatio = 75f;

    NelderMead solver;
    List<Vector4> PointCorrespondences = new List<Vector4>(9);

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

    string filename;

    void Start() {
      solver = new NelderMead(calculateCoordsFromTransformEntries(ref TransformsToOptimize), distanceCost, 0.001f);

      filename = Directory.GetParent(Application.dataPath).FullName + "/" + gameObject.name + " DisplayCalibration.txt";
      if (File.Exists(filename)) {
        string calibrationData = File.ReadAllText(filename);
        ListWrapper serializableList = JsonUtility.FromJson<ListWrapper>(calibrationData);
        PointCorrespondences = serializableList.data;
      }
    }

    void Update() {
      if (PointCorrespondences.Count > 0 && Input.GetKey(KeyCode.Space)) {
        //This keeps the simplex from becoming degenerate
        solver.constructRightAngleSimplex(solver.simplexVertices[0].coordinates, 0.001f);
        for (int i = 0; i < 10; i++) {
          solver.stepSolver();
        }
      }
    }

    float[] calculateCoordsFromTransformEntries(ref TransformEntry[] TransformsToOptimize) {
      int numDimensions = 0;
      for (int i = 0; i < TransformsToOptimize.Length; i++) {
        if (TransformsToOptimize[i].OptimizePositionX) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionY) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionZ) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizeRotationX) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionY) { numDimensions++; }
        if (TransformsToOptimize[i].OptimizePositionZ) { numDimensions++; }
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

    public void AddPosition(Vector3 canonicalPosition, Vector3 measuredPosition) {
      Vector3 canonicalViewport = raytracer.eyePerspective.WorldToViewportPoint(canonicalPosition);
      Vector2 canonicalUV = raytracer.RenderUVToDisplayUV(canonicalViewport);
      Vector3 measuredViewport = raytracer.eyePerspective.WorldToViewportPoint(measuredPosition);
      PointCorrespondences.Add(new Vector4(canonicalUV.x, canonicalUV.y, measuredViewport.x, measuredViewport.y));
    }

    public void Save() {
      if (PointCorrespondences.Count > 0) {
        ListWrapper serializableList = new ListWrapper();
        serializableList.data = PointCorrespondences;
        string jsonString = JsonUtility.ToJson(serializableList);
        File.WriteAllText(filename, jsonString);
        Debug.Log("SAVED CORRESPONDENCES: " + filename);
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      foreach (Vector4 correspondence in PointCorrespondences) {
        drawRayDisparity(new Vector2(correspondence[0], correspondence[1]), new Vector2(correspondence[2], correspondence[3]), drawer);
      }
    }

    //The evaluates the cost associated with a coordinate in parameter space
    public float distanceCost(float[] coordinate) {
      //SET THE POSITIONS OF THE COMPONENTS
      setTransformsFromCoord(coordinate, ref TransformsToOptimize);

      float cost = 0f;
      foreach (Vector4 correspondence in PointCorrespondences) {
        addRayCost(new Vector2(correspondence[0], correspondence[1]), new Vector2(correspondence[2], correspondence[3]), ref cost);
      }
      //Debug.Log(cost);
      return cost;
    }

    void setTransformsFromCoord(float[] coord, ref TransformEntry[] TransformsToOptimize) {
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

    void addRayCost(Vector2 canonicalUV, Vector2 measuredRay, ref float cost) {
      Vector3 testPoint1 = screenToWorld(raytracer.RenderUVToDisplayUV(measuredRay));
      Vector3 refPoint1 = screenToWorld(canonicalUV);

      float addedCost = Vector3.Distance(refPoint1, testPoint1) * 10000f;
      cost += (addedCost * addedCost);
    }

    public void drawCorrespondence(Vector3 canonicalPos, Vector3 measuredPos, RuntimeGizmoDrawer drawer) {
      Vector3 canonicalViewport = raytracer.eyePerspective.WorldToViewportPoint(canonicalPos);
      Vector2 canonicalUV = raytracer.RenderUVToDisplayUV(canonicalViewport);
      Vector3 measuredViewport = raytracer.eyePerspective.WorldToViewportPoint(measuredPos);
      drawRayDisparity(new Vector2(canonicalUV.x, canonicalUV.y), new Vector2(measuredViewport.x, measuredViewport.y), drawer);
    }

    void drawRayDisparity(Vector2 canonicalUV, Vector2 measuredRay, RuntimeGizmoDrawer drawer) {
      Vector3 refPoint1 = screenToWorld(canonicalUV);
      Vector3 testPoint1 = screenToWorld(raytracer.RenderUVToDisplayUV(measuredRay));

      drawer.color = Color.blue;
      drawer.DrawLine(refPoint1, testPoint1);
      drawer.color = Color.green;
      drawer.DrawSphere(refPoint1, 0.0009f);
      drawer.color = Color.red;
      drawer.DrawSphere(testPoint1, 0.0009f);
    }

    Vector3 screenToWorld(Vector2 screenUV) {
      return screenSpace.TransformPoint((Vector3)screenUV - (new Vector3(0.5f, 0.5f, 0f)));
    }
  }
}