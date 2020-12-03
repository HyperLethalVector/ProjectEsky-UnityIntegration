/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.AR {
  public class CalibrationDeformer : MonoBehaviour, IRuntimeGizmoComponent {
    public MeshFilter filter;
    public bool isLeftEye = false;
    public Camera mainCamera;
    public List<int> vertexIndices = new List<int>(0);
    public List<Vector3> controlPoints = new List<Vector3>(0);
    public bool drawRuntimeGizmos = true;

    MeshDeformer calibrationMesh;
    Vector3 curMousePosition, lastMousePosition;
    int closestVertex = -1;
    Vector2 screen = new Vector3(2880, 1600);

    void Start() {
      if (!enabled) { return; }
      InitializeMeshDeformations();
    }

    void Update() {
      if (!enabled) { return; }
      curMousePosition = new Vector3(Input.mousePosition.x, screen.y - Input.mousePosition.y, 0f);
      bool mouseOnLeftSide = Input.mousePosition.x < screen.x / 2;
      if ((mouseOnLeftSide && !isLeftEye) || (!mouseOnLeftSide && isLeftEye)) { closestVertex = -1; return; }
      Cursor.visible = !(new Rect(0f, 0f, screen.x, screen.y).Contains(Input.mousePosition));

      if (!Application.isEditor) { return; }
      if (closestVertex != -1 && Input.GetMouseButton(0) || Input.GetMouseButtonDown(1)) {
        //Move a control point if it already exists
        for (int i = 0; i < vertexIndices.Count; i++) {
          if (closestVertex == vertexIndices[i]) {
            Ray cameraRay = mainCamera.ScreenPointToRay(curMousePosition);
            if (!Input.GetMouseButtonDown(1)) {
              Vector3 start = filter.transform.InverseTransformPoint(cameraRay.origin);
              controlPoints[i] = new Vector3(start.x, start.y, 0f);
              calibrationMesh.updateMeshDeformation(ref filter, controlPoints.ToArray(), true, 1.5f, 10);
            } else {
              vertexIndices.RemoveAt(i);
              controlPoints.RemoveAt(i);
              InitializeMeshDeformations();
            }
            return;
          }
        }

        //If control point doesn't already exist, create a new one
        if (!Input.GetMouseButtonDown(1)) {
          vertexIndices.Add(closestVertex);
          controlPoints.Add(calibrationMesh.distortedMeshVertices[closestVertex]);
          InitializeMeshDeformations();
        }
      } else if (Input.mousePosition != lastMousePosition) {
        //Find closest vertex to mouse position
        closestVertex = findClosestVertexToMouse(calibrationMesh, mainCamera);
      }
      lastMousePosition = Input.mousePosition;
    }

    public void InitializeMeshDeformations() {
      if (!enabled) { return; }
      calibrationMesh = new MeshDeformer(filter, vertexIndices.ToArray());
      calibrationMesh.updateMeshDeformation(ref filter, controlPoints.ToArray(), true, 2f, 50);
    }

    int findClosestVertexToMouse(MeshDeformer mesh, Camera cam) {
      Ray cameraRay = mainCamera.ScreenPointToRay(curMousePosition);
      Vector3 start = filter.transform.InverseTransformPoint(cameraRay.origin);
      Vector3 end = filter.transform.InverseTransformPoint(cameraRay.origin + (cameraRay.direction * mainCamera.farClipPlane));

      int closestVertex = -1; float closestSqrDistance = 100000.0f;
      for (int i = 0; i < calibrationMesh.distortedMeshVertices.Length; i++) {
        float currentSqrDistance = SqrDistanceToSegment(calibrationMesh.distortedMeshVertices[i], start, end);
        if (currentSqrDistance < closestSqrDistance) {
          closestVertex = i;
          closestSqrDistance = currentSqrDistance;
        }
      }
      return closestVertex;
    }

    float SqrDistanceToSegment(Vector3 position, Vector3 a, Vector3 b) {
      Vector3 ba = b - a;
      return Vector3.SqrMagnitude(position - Vector3.Lerp(a, b, Vector3.Dot(position - a, ba) / ba.sqrMagnitude));
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!Application.isEditor || !drawRuntimeGizmos || !enabled) { return; }
      if (!Input.GetMouseButton(0)) {
        Ray cameraRay = mainCamera.ScreenPointToRay(curMousePosition);
        Vector3 start = filter.transform.InverseTransformPoint(cameraRay.origin);
        drawer.DrawSphere(filter.transform.TransformPoint(new Vector3(start.x, start.y, 0f)), 0.0075f);
      }

      drawer.color = Color.green;
      if (closestVertex != -1) {
        drawer.DrawSphere(filter.transform.TransformPoint(calibrationMesh.distortedMeshVertices[closestVertex]), 0.01f);
      }
      for (int i = 0; i < vertexIndices.Count; i++) {
        drawer.DrawSphere(filter.transform.TransformPoint(calibrationMesh.distortedMeshVertices[vertexIndices[i]]), 0.005f);
      }
    }
  }
}
