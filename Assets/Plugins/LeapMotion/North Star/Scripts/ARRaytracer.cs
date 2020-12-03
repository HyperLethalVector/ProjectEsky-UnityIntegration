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
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Leap.Unity.AR {
  public class ARRaytracer : MonoBehaviour, IRuntimeGizmoComponent {
    public OpticalCalibrationManager manager;
    public Camera eyePerspective;
    public EllipsoidTransform ellipse;
    public Transform Screen;
    public MeshFilter filter;
    public Vector2 meshResolution = new Vector2(10, 10);
    public CalibrationDeformer deformer;
    [Range(0.5f, 2f)]
    public float aspectRatio = 0.8f;

    public OpticalSystem optics;

    [Tooltip("Auto-recalculates the distortion mesh once every five frames when in Play "
           + "mode in the editor.")]
    public bool autoRefreshRuntimeEditor = true;

    public bool parallelizeRaytracing = true;

    private Mesh _backingDistortionMesh = null;
    private Mesh _distortionMesh {
      get {
        if (_backingDistortionMesh == null) {
          _backingDistortionMesh = new Mesh();
        }
        return _backingDistortionMesh;
      }
    }
    List<Vector2> meshUVs = new List<Vector2>(100);
    List<Vector3> meshVertices = new List<Vector3>(100);
    List<Vector3> previousMeshVertices = new List<Vector3>(100);
    List<int> meshTriangles = new List<int>(600);

    NativeArray<Vector2> rayUVs;
    NativeArray<Vector3> vertices;
    Vector3[] managedVertices;
    JobHandle raytraceJob;

    void Start() {
      if (eyePerspective == null) { eyePerspective = GetComponent<Camera>(); /*eyePerspective.aspect = aspectRatio;*/ }
      if (deformer == null) { deformer = eyePerspective.transform.parent.GetComponent<CalibrationDeformer>(); }

      //Set up job-elements
      meshVertices.Clear();
      meshUVs.Clear();
      //Full range = 0f - 1f
      for (float i = 0; i <= meshResolution.x; i++) {
        for (float j = 0; j <= meshResolution.y; j++) {
          Vector2 RenderUV = new Vector2(i / meshResolution.x, j / meshResolution.y);
          meshUVs.Add(RenderUV);
        }
      }
      rayUVs = new NativeArray<Vector2>(meshUVs.ToArray(), Allocator.Persistent);
      vertices = new NativeArray<Vector3>(meshUVs.Count, Allocator.Persistent);

      ScheduleCreateDistortionMesh();
      Application.targetFrameRate = -1000;
    }


    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (eyePerspective == null) { eyePerspective = GetComponent<Camera>(); }
      if (!Application.isPlaying) {
        ScheduleCreateDistortionMesh(false, drawer);
      }
    }

    private void LateUpdate() {
      // Nice to calculate the distortion mesh after the first Start() and Update().
      if (Time.time < 1.0f || (autoRefreshRuntimeEditor && (Application.isEditor || Time.frameCount % 2 == 0))) {
        ScheduleCreateDistortionMesh(true);
      }
    }

    [ContextMenu("Print Optical Calibration")]
    void printOpticalConfig() {
      Debug.Log(new OpticalSystem(eyePerspective, ellipse, Screen,
        (manager == null) ? null : manager.maybeHeadsetPose).ToString());
    }

    [ContextMenu("Create Distortion Mesh")]
    public void ScheduleCreateDistortionMesh(bool onlyRecomputeVertices = false,
                                             RuntimeGizmoDrawer drawer = null) {
      CompleteDistortionMesh(onlyRecomputeVertices, null);

      if (eyePerspective == null) { eyePerspective = GetComponent<Camera>(); }
      optics = (manager != null && Application.isPlaying) ?
        (transform.localPosition.x < 0f ?
          manager.currentCalibration.leftEye
        : manager.currentCalibration.rightEye)
      : new OpticalSystem(eyePerspective, ellipse, Screen,
        (manager == null) ? null : manager.maybeHeadsetPose);

      if (manager != null && Application.isPlaying) {
        eyePerspective.projectionMatrix = (transform.localPosition.x < 0f ? 
          manager.currentCalibration.leftEye :             
          manager.currentCalibration.rightEye).cameraProjection.ComposeProjection();
      } else {
        eyePerspective.ResetProjectionMatrix();
        eyePerspective.aspect = aspectRatio;
      }

      if (!parallelizeRaytracing || !vertices.IsCreated) { meshVertices.Clear(); }
      if (!onlyRecomputeVertices) { meshUVs.Clear(); }
      int index = 0;
      for (float i = 0; i <= meshResolution.x; i++) {
        for (float j = 0; j <= meshResolution.y; j++) {
          Vector2 RenderUV = new Vector2(i / meshResolution.x, j / meshResolution.y);
          if (!onlyRecomputeVertices) { meshUVs.Add(RenderUV); if (rayUVs.IsCreated) { rayUVs[index] = RenderUV; } }
          if (!parallelizeRaytracing || !vertices.IsCreated) {
            Vector3 eyeRay;
            eyeRay = ViewportPointToRayDirection(RenderUV, eyePerspective.transform.localPosition, optics.clipToWorld);
            //eyeRay = eyePerspective.ViewportPointToRay(new Vector3(RenderUV.x, RenderUV.y, 1f));
            meshVertices.Add(
              RenderUVToDisplayUV(eyeRay, optics, /*(i % 5 == 0 && j % 5 == 0)*/false,
              false/*, drawer*/) - (Vector2.one * 0.5f));
          }
          //if (drawer != null) { drawer.DrawSphere(filter.transform.TransformPoint(meshVertices[meshVertices.Count - 1]), 0.005f); }
          index++;
        }
      }

      if (parallelizeRaytracing && rayUVs.IsCreated && vertices.IsCreated) {
        raytraceJob = new RaytraceOpticsJob() {
          optics = optics,
          vertices = vertices,
          uvs = rayUVs
        }.Schedule(rayUVs.Length, 64);
        JobHandle.ScheduleBatchedJobs();
      } else {
        CompleteDistortionMesh(onlyRecomputeVertices, drawer);
      }
    }

    public void CompleteDistortionMesh(bool onlyRecomputeVertices = false,
                                       RuntimeGizmoDrawer drawer = null) {

      if(previousMeshVertices.Count != vertices.Length) previousMeshVertices = new List<Vector3>(vertices.Length);
      _distortionMesh.GetVertices(previousMeshVertices);

      if (parallelizeRaytracing && rayUVs.IsCreated && vertices.IsCreated) {
        raytraceJob.Complete();
        if (managedVertices == null || vertices.Length != managedVertices.Length) {
          managedVertices = vertices.ToArray();
        } else {
          vertices.CopyTo(managedVertices);
        }
        for(int i = 0; i < managedVertices.Length; i++) if(managedVertices[i].x == -0.5f) managedVertices[i] = previousMeshVertices[i];
        _distortionMesh.vertices = managedVertices;
      } else {
        for(int i = 0; i < meshVertices.Count; i++) if(meshVertices[i].x == -0.5f) meshVertices[i] = previousMeshVertices[i];
        _distortionMesh.SetVertices(meshVertices);
      }
      if (!onlyRecomputeVertices) { _distortionMesh.RecalculateNormals(); _distortionMesh.SetUVs(0, meshUVs); }

      if ((!onlyRecomputeVertices || meshTriangles.Count == 0) && _distortionMesh.vertexCount > 0) {
        meshTriangles.Clear();
        for (int x = 1; x <= meshResolution.x; x++) {
          for (int y = 1; y <= meshResolution.y; y++) {
            //Adds the index of the three vertices in order to make up each of the two tris
            meshTriangles.Add((int)meshResolution.x * x + y); //Top right
            meshTriangles.Add((int)meshResolution.x * x + y - 1); //Bottom right
            meshTriangles.Add((int)meshResolution.x * (x - 1) + y - 1); //Bottom left - First triangle
            meshTriangles.Add((int)meshResolution.x * (x - 1) + y - 1); //Bottom left 
            meshTriangles.Add((int)meshResolution.x * (x - 1) + y); //Top left
            meshTriangles.Add((int)meshResolution.x * x + y); //Top right - Second triangle
          }
        }
        _distortionMesh.SetTriangles(meshTriangles, 0);
      }

      if (filter != null) {
        filter.sharedMesh = _distortionMesh;
      }

      if (deformer != null && deformer.controlPoints.Count > 0) {
        deformer.InitializeMeshDeformations();
      }
    }

    public struct OpticalSystem {

      public float ellipseMinorAxis;
      public float ellipseMajorAxis;
      public Vector3 screenForward;
      public Vector3 screenPosition;
      public Vector3 eyePosition;
      public Matrix4x4 worldToSphereSpace;
      public Matrix4x4 sphereToWorldSpace;
      public Matrix4x4 worldToScreenSpace;
      public Matrix4x4 clipToWorld;

      public OpticalSystem(Camera eyePerspective,
                           EllipsoidTransform ellipse,
                           Transform Screen,
                           Pose? headsetOrigin = null) {
        eyePosition = eyePerspective.transform.position;
        
        bool didEllipsoidActuallyUpdate = ellipse.UpdateEllipsoid(); // Sigh.
        sphereToWorldSpace = ellipse.sphereToWorldSpace;
        worldToSphereSpace = ellipse.worldToSphereSpace;

        ellipseMajorAxis = ellipse.MajorAxis;
        ellipseMinorAxis = ellipse.MinorAxis;
        screenForward = Screen.forward;
        screenPosition = Screen.position;
        worldToScreenSpace = Screen.worldToLocalMatrix;
        clipToWorld = eyePerspective.cameraToWorldMatrix *
          eyePerspective.projectionMatrix.inverse;
          
        if (headsetOrigin.HasValue) {
          // If debugging this, helps to draw matrices with:
          // var drawer = HyperMegaStuff.HyperMegaLines.drawer;
          // (new Geometry.Sphere(radius)).DrawLines(
          //   drawer.DrawLine,
          //   overrideMatrix: aLocalToWorldMatrix);
          var headsetWorldToLocal = headsetOrigin.Value.inverse.matrix;
          eyePosition = headsetWorldToLocal.MultiplyPoint3x4(eyePosition);

          screenForward = headsetWorldToLocal.MultiplyVector(Screen.forward)
            .normalized;
          screenPosition = headsetWorldToLocal.MultiplyPoint3x4(Screen.position);
          worldToScreenSpace = (headsetWorldToLocal * Screen.localToWorldMatrix)
            .inverse;
          clipToWorld = headsetWorldToLocal * clipToWorld;

          if (didEllipsoidActuallyUpdate) {
            sphereToWorldSpace = headsetWorldToLocal * sphereToWorldSpace;
            worldToSphereSpace = sphereToWorldSpace.inverse;
          }
        }
      }
      public override string ToString() {
        return 
          "ellipseMinorAxis: " + ellipseMinorAxis +
          "\nellipseMajorAxis: " + ellipseMajorAxis +
          "\nscreenForward: " + screenForward.ToString("R") +
          "\nscreenPosition: " + screenPosition.ToString("R") +
          "\neyePosition: " + eyePosition.ToString("R") +
          "\nworldToSphereSpace: \n" + worldToSphereSpace +
          "sphereToWorldSpace: \n" + sphereToWorldSpace +
          "worldToScreenSpace: \n" + worldToScreenSpace +
          "clipToWorld: \n" + clipToWorld;
      }
    }

    public Vector2 RenderUVToDisplayUV(Vector2 UV, bool drawLine = false, RuntimeGizmoDrawer drawer = null) {
      optics = (manager != null && Application.isPlaying) ?
        (transform.localPosition.x < 0f ?
          manager.currentCalibration.leftEye
        :  manager.currentCalibration.rightEye)
      : new OpticalSystem(eyePerspective, ellipse, Screen,
        (manager == null) ? null : manager.maybeHeadsetPose);
      return RenderUVToDisplayUV(
        ViewportPointToRayDirection(new Vector3(UV.x, UV.y, 1f),
          optics.eyePosition, optics.clipToWorld),
        optics,
        drawLine);
    }

    public static Vector2 RenderUVToDisplayUV(Vector2 inputUV, OpticalSystem optics) {
      return RenderUVToDisplayUV(
        ViewportPointToRayDirection(new Vector3(inputUV.x, inputUV.y, 1f),
        optics.eyePosition,
        optics.clipToWorld),
        optics);
    }

    public static Vector2 RenderUVToDisplayUV(Vector3 inputUV,
                                              OpticalSystem optics,
                                              bool drawLine = false,
                                              bool printDebug = false) {
                                              //RuntimeGizmoDrawer drawer = null) {
      Vector3 sphereSpaceRayOrigin =
        optics.worldToSphereSpace.MultiplyPoint(optics.eyePosition);
      Vector3 sphereSpaceRayDirection =
        optics.worldToSphereSpace.MultiplyPoint(optics.eyePosition + inputUV) -
          sphereSpaceRayOrigin;
      sphereSpaceRayDirection =
        sphereSpaceRayDirection / sphereSpaceRayDirection.magnitude;
      float intersectionTime = intersectLineSphere(sphereSpaceRayOrigin,
        sphereSpaceRayDirection, Vector3.zero, 0.5f * 0.5f, false);
      /*if (printDebug) {
        Debug.Log(
        "sphereSpaceRayOrigin" + sphereSpaceRayOrigin.ToString("R") +
        "\nsphereSpaceRayDirection" + sphereSpaceRayDirection.ToString("R") +
        "\nintersectionTime" + intersectionTime);
      }*/
      if (intersectionTime < 0f) { return Vector2.zero; }
      Vector3 sphereSpaceIntersection = sphereSpaceRayOrigin + (intersectionTime * sphereSpaceRayDirection);

      //Ellipsoid  Normals
      Vector3 sphereSpaceNormal = -sphereSpaceIntersection / sphereSpaceIntersection.magnitude;
      sphereSpaceNormal = new Vector3(sphereSpaceNormal.x / Mathf.Pow(optics.ellipseMinorAxis / 2f, 2f), sphereSpaceNormal.y / Mathf.Pow(optics.ellipseMinorAxis / 2f, 2f), sphereSpaceNormal.z / Mathf.Pow(optics.ellipseMajorAxis / 2f, 2f));
      sphereSpaceNormal /= sphereSpaceNormal.magnitude;

      Vector3 worldSpaceIntersection = optics.sphereToWorldSpace.MultiplyPoint(sphereSpaceIntersection);
      Vector3 worldSpaceNormal = optics.sphereToWorldSpace.MultiplyVector(sphereSpaceNormal);
      worldSpaceNormal /= worldSpaceNormal.magnitude;

      Ray firstBounce = new Ray(worldSpaceIntersection, Vector3.Reflect(inputUV, worldSpaceNormal));
      intersectionTime = intersectPlane(optics.screenForward, optics.screenPosition, firstBounce.origin, firstBounce.direction);
      if (intersectionTime < 0f) { return Vector2.zero; }
      Vector3 planeIntersection = firstBounce.GetPoint(intersectionTime);

      Vector2 ScreenUV = optics.worldToScreenSpace.MultiplyPoint3x4(planeIntersection);

      // Uncomment for ray debugging.
       if (drawLine) {
         var eyeRayOrigin = optics.sphereToWorldSpace.MultiplyPoint(sphereSpaceRayOrigin);
         drawTrace(eyeRayOrigin, firstBounce.origin, planeIntersection);
       }

      //ScreenUV = new Vector2(Mathf.Clamp01(ScreenUV.x + 0.5f), Mathf.Clamp01(ScreenUV.y + 0.5f));
      ScreenUV = new Vector2(ScreenUV.x + 0.5f, ScreenUV.y + 0.5f);

      return ScreenUV;
    }

    public static Vector2 DisplayUVToRenderUV(Vector2 inputUV, OpticalSystem optics, int iterations = 40) {
      float epsilon = 0.025f;
      Vector2 curCameraUV = new Vector2(-inputUV.x, inputUV.y);

      for (int i = 0; i < iterations; i++) {
        Vector2 curDisplayUV = RenderUVToDisplayUV(curCameraUV, optics);
        Vector2 displayUVGradX = (RenderUVToDisplayUV(curCameraUV + (Vector2.right * epsilon), optics) - curDisplayUV) / epsilon;
        Vector2 displayUVGradY = (RenderUVToDisplayUV(curCameraUV + (Vector2.up * epsilon), optics) - curDisplayUV) / epsilon;
        Vector2 error = curDisplayUV - inputUV;
        Vector2 step = ((error.x * displayUVGradX) + (error.y * displayUVGradY)) * 0.1f;
        curCameraUV = curCameraUV + step;
        //Debug.Log("InputUV: " + inputUV.ToString("F3") + ", curUV: " + curDisplayUV.ToString("F3")+ ", Error:"+ error.magnitude);
      }

      return curCameraUV;
    }

    [BurstDiscard]
    static void drawTrace(Vector3 eyeOrigin, Vector3 firstBounce, Vector3 screenHit) {
      var drawer = HyperMegaStuff.HyperMegaLines.drawer;
      if (drawer != null) {
        drawer.color = LeapColor.coral;
        drawer.DrawLine(eyeOrigin, firstBounce);
        drawer.color = LeapColor.orange;
        drawer.DrawLine(firstBounce, screenHit);
        //drawer.DrawSphere(firstBounce.origin, 0.0005f);
        //drawer.DrawSphere(((firstBounce.origin - eyeRay.origin) * 1f) + eyeRay.origin, 0.0005f);
        //drawer.DrawSphere(planeIntersection, 0.0005f);
      }
    }

    public static float intersectLineSphere(Vector3 Origin, Vector3 Direction, Vector3 spherePos, float SphereRadiusSqrd, bool frontSide = true) {
      Vector3 L = spherePos - Origin;
      Vector3 offsetFromSphereCenterToRay = Project(L, Direction) - L;
      return (offsetFromSphereCenterToRay.sqrMagnitude <= SphereRadiusSqrd) ? Vector3.Dot(L, Direction) - (Mathf.Sqrt(SphereRadiusSqrd - offsetFromSphereCenterToRay.sqrMagnitude) * (frontSide ? 1f : -1f)) : -1f;
    }

    static Vector3 Project(Vector3 v1, Vector3 v2) {
      Vector3 v2Norm = (v2 / v2.magnitude);
      return Vector3.Dot(v1, v2Norm) * v2Norm;
    }

    public static float intersectPlane(Vector3 n, Vector3 p0, Vector3 l0, Vector3 l) {
      float denom = Vector3.Dot(-n, l);
      if (denom > 1.4e-45f) {
        Vector3 p0l0 = p0 - l0;
        float t = Vector3.Dot(p0l0, -n) / denom;
        return t;
      }
      return -1f;
    }

    public static Vector3 ViewportPointToRayDirection(Vector2 UV, Vector3 cameraPosition, Matrix4x4 clipToWorld) {
      Vector3 dir = clipToWorld.MultiplyPoint(new Vector3(UV.x - 0.5f, UV.y - 0.5f, 0.5f) * 2f) - cameraPosition;
      return dir / dir.magnitude;
    }

    //Enable in a new build of unity for a massive speedup!
    //[BurstCompile]
    //[ComputeJobOptimization]
    public struct RaytraceOpticsJob : IJobParallelFor {
      [ReadOnly]
      public NativeArray<Vector2> uvs;
      [ReadOnly]
      public OpticalSystem optics;
      [WriteOnly]
      public NativeArray<Vector3> vertices;
      public void Execute(int i) {
        Vector2 vert =  RenderUVToDisplayUV(
          ViewportPointToRayDirection(
            uvs[i], optics.eyePosition, optics.clipToWorld
          ),
          optics
        ) - new Vector2(0.5f, 0.5f);
        if(vert != new Vector2(0,0)) vertices[i] = vert;
      }
    }

    void OnDestroy() {
      CompleteDistortionMesh(false);
      rayUVs.Dispose();
      vertices.Dispose();
    }
  }
}
