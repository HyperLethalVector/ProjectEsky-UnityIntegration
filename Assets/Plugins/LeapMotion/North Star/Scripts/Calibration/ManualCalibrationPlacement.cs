using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using LeapInternal;

public class ManualCalibrationPlacement : MonoBehaviour {
  public LeapServiceProvider provider;
  public CameraPostProcessing processing;
  public Transform selectionCursor;
  public Transform leftRigel, rightRigel;

  public KeyCode AdvanceCalibration = KeyCode.Semicolon;
  public TextMesh debugTextState;

  Ray[] calibrationPointRays = new Ray[6];
  bool suspended = false;

  void Start () {
    StartCoroutine("CalibrationRoutine");
	}

  void Update() {
    suspended = false;
  }

  IEnumerator CalibrationRoutine() {
    //So it will need to select 6 points(3 in each image)
    for (int i = 0; i < calibrationPointRays.Length; i+=2) {

      yield return waitForKeypress(AdvanceCalibration, "Align the Cursor in the Left Camera and Press: " + AdvanceCalibration);
      Vector2 cursorPixelPosition = localSpaceToPixel(selectionCursor.localPosition, processing.combinedImage);
      Vector leapRayDir = Connection.GetConnection(0).PixelToRectilinear(Image.CameraType.RIGHT, new Vector(cursorPixelPosition.x,
                                                                                                            cursorPixelPosition.y, 0f));
      Vector3 rayDir = new Vector3(leapRayDir.x, leapRayDir.y, 1f);
      calibrationPointRays[i] = new Ray(leftRigel.position, leftRigel.TransformDirection(rayDir));

      yield return waitForKeypress(AdvanceCalibration, "Align the Cursor in the Right Camera and Press: " + AdvanceCalibration);
      cursorPixelPosition = localSpaceToPixel(selectionCursor.localPosition, processing.combinedImage);
      leapRayDir = Connection.GetConnection(0).PixelToRectilinear(Image.CameraType.LEFT, new Vector(cursorPixelPosition.x,
                                                                                                    cursorPixelPosition.y, 0f));
      rayDir = new Vector3(leapRayDir.x, leapRayDir.y, 1f);
      calibrationPointRays[i+1] = new Ray(rightRigel.position, rightRigel.TransformDirection(rayDir));
    }

    //Triangulate each of those points in world space from the "known" position of the rigel
    yield return waitForKeypress(AdvanceCalibration, "Verify that the rays and points look appropriate and press: " + AdvanceCalibration);
    //Kabsch from those points to points on the model
    //Move the model to align with the kabsched points
  }

  WaitUntil waitForKeypress(KeyCode code, string DebugText = "") {
    debugTextState.text = DebugText;
    Debug.Log(DebugText);
    suspended = Input.GetKeyDown(code);
    return new WaitUntil(() => !suspended && Input.GetKeyDown(code));
  }

  void OnDrawGizmos() {
    for (int i = 0; i < calibrationPointRays.Length; i += 2) {
      Gizmos.DrawRay(calibrationPointRays[i]); Gizmos.DrawRay(calibrationPointRays[i + 1]);
      Gizmos.DrawSphere(RayRayIntersection(calibrationPointRays[i], calibrationPointRays[i + 1]), 0.01f);
    }
  }

  public static Vector2 localSpaceToPixel(Vector3 combinedLocalSpace, Image image) {
    Vector2 UV = new Vector2(combinedLocalSpace.x + 0.5f, ((combinedLocalSpace.y * 2f) + 1f) % 1f);
    return UV * new Vector2(image.Width, image.Height);
  }

  public static float ClosestAlphaOnSegmentToLine(Vector3 segA, Vector3 segB, Vector3 lineA, Vector3 lineB) {
    Vector3 lineBA = lineB - lineA; float lineDirSqrMag = Vector3.Dot(lineBA, lineBA);
    Vector3 inPlaneA = segA - ((Vector3.Dot(segA - lineA, lineBA) / lineDirSqrMag) * lineBA),
            inPlaneB = segB - ((Vector3.Dot(segB - lineA, lineBA) / lineDirSqrMag) * lineBA);
    Vector3 inPlaneBA = inPlaneB - inPlaneA;
    return (lineDirSqrMag != 0f && inPlaneA != inPlaneB) ? Vector3.Dot(lineA - inPlaneA, inPlaneBA) / Vector3.Dot(inPlaneBA, inPlaneBA) : 0f;
  }

  public static Vector3 RayRayIntersection(Ray rayA, Ray rayB) {
     return Vector3.LerpUnclamped(rayA.origin, rayA.origin + rayA.direction, 
      ClosestAlphaOnSegmentToLine(rayA.origin, rayA.origin + rayA.direction, 
                                  rayB.origin, rayB.origin + rayB.direction));
  }
}
