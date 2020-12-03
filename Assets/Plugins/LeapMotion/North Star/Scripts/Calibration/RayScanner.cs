using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.AR {
  public class RayScanner : MonoBehaviour, IRuntimeGizmoComponent {
    public CameraPostProcessing processing;
    public float time = -1f;
    public float sensitivity = 0.4f;

    public Camera RightCamera;
    public Camera LeftCamera;

    public Vector2 meshResolution = new Vector2(10, 10);
    public MeshFilter leftEyeMeshFilter;
    public MeshFilter rightEyeMeshFilter;

    float lastTime = 0f;
    Vector3 pos = Vector3.zero;
    //Vector3 lastRecordedPos = Vector3.zero;

    Mesh leftEyeDistortionMesh;
    Mesh rightEyeDistortionMesh;
    List<Vector2> leftEyeMeshUVs = new List<Vector2>(100);
    List<Vector2> rightEyeMeshUVs = new List<Vector2>(100);
    List<Vector3> meshVertices = new List<Vector3>(100);
    List<int> meshTriangles = new List<int>(600);
    int UVIndex = 0;

    private void Start() {
      leftEyeDistortionMesh = new Mesh();
      rightEyeDistortionMesh = new Mesh();
    }

    private void Update() {
      if (Input.GetKeyDown(KeyCode.Space)) {
        CreateDistortionMeshes();
      }

      if ((time > 0f && Time.time - lastTime > 0.175) || Input.GetKey(KeyCode.Space)) {
        time += 1f / meshResolution.x;
        lastTime = Time.time;

        pos.x = ((time % 1f) - 0.5f) * Camera.main.aspect;
        pos.y = (-Mathf.Floor(time) / meshResolution.y) + 0.5f;
        /*
        if (processing.LeftBlobs.Count > 0) {
          //DO MESH UV ASSIGNMENT HERE
          Vector ray = processing.combinedImage.PixelToRectilinear(Image.PerspectiveType.STEREO_LEFT, new Vector(processing.LeftBlobs[processing.biggestLeftBlobIndex].x, 
                                                                                                                 processing.LeftBlobs[processing.biggestLeftBlobIndex].y, 0f));
          Debug.DrawLine(LeftCamera.transform.position, LeftCamera.transform.TransformPoint(new Vector3(ray.x, ray.y, 1f)), Color.cyan, 0.15f);
          leftEyeMeshUVs[UVIndex] = calculateUVFromRay(new Vector3(ray.x, ray.y, 1f), LeftCamera);
          //lastRecordedPos = processing.LeftBlobs[processing.biggestLeftBlobIndex];
        }else if (UVIndex > 0) {
          leftEyeMeshUVs[UVIndex] = leftEyeMeshUVs[UVIndex - 1];
        }

        if (processing.RightBlobs.Count > 0) {
          //DO MESH UV ASSIGNMENT HERE
          Vector ray = processing.combinedImage.PixelToRectilinear(Image.PerspectiveType.STEREO_RIGHT, new Vector(processing.RightBlobs[processing.biggestRightBlobIndex].x,
                                                                                                                  processing.RightBlobs[processing.biggestRightBlobIndex].y - processing.combinedImage.Height, 0f));
          Debug.DrawLine(RightCamera.transform.position, RightCamera.transform.TransformPoint(new Vector3(ray.x, ray.y, 1f)), Color.red, 0.15f);
          rightEyeMeshUVs[UVIndex] = calculateUVFromRay(new Vector3(ray.x, ray.y, 1f), RightCamera);
          //lastRecordedPos = processing.RightBlobs[processing.biggestRightBlobIndex];
        } else if (UVIndex > 0) {
          rightEyeMeshUVs[UVIndex] = rightEyeMeshUVs[UVIndex - 1];
        }
        */
        reapplyMeshUVs();

        UVIndex++;
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (time > 0f) {
        drawer.DrawSphere(transform.TransformPoint(pos), 0.05f);
      }
    }

    public Vector2 calculateUVFromRay(Vector3 ray, Camera camera) {
      return camera.WorldToViewportPoint(camera.transform.TransformPoint(ray));
    }


    public void CreateDistortionMeshes() {
      meshVertices.Clear();
      leftEyeMeshUVs.Clear();
      rightEyeMeshUVs.Clear();
      meshTriangles.Clear();

      //Full range = 0f - 1f
      for (float i = 0; i <= 1.001f; i += 1f / (meshResolution.x - 1)) {
        for (float j = 0; j <= 1.001f; j += 1f / (meshResolution.y - 1)) {
          Vector2 RenderUV = new Vector2(i, j);
          leftEyeMeshUVs.Add(RenderUV);
          rightEyeMeshUVs.Add(RenderUV);
          meshVertices.Add(RenderUV - (Vector2.one * 0.5f));
        }
      }

      for (int x = 1; x < meshResolution.x; x++) {
        for (int y = 1; y < meshResolution.y; y++) {
          //Adds the index of the three vertices in order to make up each of the two tris
          meshTriangles.Add((int)meshResolution.x * x + y); //Top right
          meshTriangles.Add((int)meshResolution.x * x + y - 1); //Bottom right
          meshTriangles.Add((int)meshResolution.x * (x - 1) + y - 1); //Bottom left - First triangle
          meshTriangles.Add((int)meshResolution.x * (x - 1) + y - 1); //Bottom left 
          meshTriangles.Add((int)meshResolution.x * (x - 1) + y); //Top left
          meshTriangles.Add((int)meshResolution.x * x + y); //Top right - Second triangle
        }
      }

      leftEyeDistortionMesh.SetVertices(meshVertices);
      leftEyeDistortionMesh.SetUVs(0, leftEyeMeshUVs);
      leftEyeDistortionMesh.SetTriangles(meshTriangles, 0);
      leftEyeDistortionMesh.RecalculateNormals();

      leftEyeMeshFilter.sharedMesh = leftEyeDistortionMesh;

      rightEyeDistortionMesh.SetVertices(meshVertices);
      rightEyeDistortionMesh.SetUVs(0, rightEyeMeshUVs);
      rightEyeDistortionMesh.SetTriangles(meshTriangles, 0);
      rightEyeDistortionMesh.RecalculateNormals();

      rightEyeMeshFilter.sharedMesh = rightEyeDistortionMesh;
#if UNITY_EDITOR
      AssetDatabase.CreateAsset(leftEyeDistortionMesh, "Assets/ARTesting/Models/GeneratedMeshes/LeftEyeMesh.asset");
      AssetDatabase.CreateAsset(rightEyeDistortionMesh, "Assets/ARTesting/Models/GeneratedMeshes/RightEyeMesh.asset");
#endif
    }

    public void reapplyMeshUVs() {
      leftEyeDistortionMesh.SetUVs(0, leftEyeMeshUVs);
      rightEyeDistortionMesh.SetUVs(0, rightEyeMeshUVs);
    }
  }
}