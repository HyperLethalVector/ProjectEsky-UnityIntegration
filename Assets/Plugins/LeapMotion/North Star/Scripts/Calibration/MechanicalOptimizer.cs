using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.AR;

namespace Leap.Unity.AR.Testing {
  public class MechanicalOptimizer : MonoBehaviour, IRuntimeGizmoComponent {
    public ARRaytracer raytracer;
    public Transform reflector;
    public Transform screen;
    public EllipsoidTransform ellipse;

    public float rotationUnitRatio = 75f;
    NelderMead solver;

    void Start() {
      float[] initialCoord = new float[12];
      for (int i = 0; i < 3; i++) { initialCoord[i] = reflector.localPosition[i]; }
      for (int i = 0; i < 3; i++) { initialCoord[i + 3] = reflector.localRotation.eulerAngles[i] / rotationUnitRatio; }
      for (int i = 0; i < 3; i++) { initialCoord[i + 6] = screen.localPosition[i]; }
      for (int i = 0; i < 3; i++) { initialCoord[i + 9] = screen.localRotation.eulerAngles[i] / rotationUnitRatio; }

      solver = new NelderMead(initialCoord, distanceCost, 0.001f);
    }

    void Update() {
      solver.stepSolver();
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    }

    //The evaluates the cost associated with a coordinate in parameter space
    public float distanceCost(float[] coordinate) {
      //SET THE POSITIONS OF THE COMPONENTS
      reflector.localPosition = new Vector3(coordinate[0], coordinate[1], coordinate[2]);
      reflector.localRotation = Quaternion.Euler(new Vector3(coordinate[3], coordinate[4], coordinate[5]) * rotationUnitRatio);

      screen.localPosition = new Vector3(coordinate[6], coordinate[7], coordinate[8]);
      screen.localRotation = Quaternion.Euler(new Vector3(coordinate[9], coordinate[10], coordinate[11]) * rotationUnitRatio);

      //Gah, should probably do this on a transform dirty event or something...
      ellipse.UpdateEllipsoid();

      float cost = 0f;
      addAPoint(new Vector2(0.25f, 0.25f), ref cost);
      addAPoint(new Vector2(0.25f, 0.75f), ref cost);
      addAPoint(new Vector2(0.75f, 0.25f), ref cost);
      addAPoint(new Vector2(0.75f, 0.75f), ref cost);
      addAPoint(new Vector2(0.5f, 0.5f), ref cost);

      return cost;
    }

    void addAPoint(Vector2 point, ref float cost) {
      Vector3 refPoint1 = point;
      Vector3 testPoint1 = screenToWorld(raytracer.RenderUVToDisplayUV(new Vector2(1f - refPoint1.y, refPoint1.x)));
      refPoint1 = screenToWorld(refPoint1);

      Debug.DrawLine(refPoint1, refPoint1 + Vector3.up * 0.01f, Color.red);
      Debug.DrawLine(testPoint1, testPoint1 + Vector3.up * 0.01f, Color.green);
      Debug.DrawLine(refPoint1, testPoint1);
      float addedCost = Vector3.Distance(refPoint1, testPoint1);
      cost += (addedCost * addedCost * addedCost * addedCost * addedCost);
    }

    Vector3 screenToWorld(Vector2 screenUV) {
      return screen.TransformPoint((Vector3)screenUV - (new Vector3(0.5f, 0.5f, 0f)));
    }
  }
}