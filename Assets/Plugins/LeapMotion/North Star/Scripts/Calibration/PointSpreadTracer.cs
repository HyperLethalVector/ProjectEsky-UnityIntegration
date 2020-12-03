using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.AR {

  public class PointSpreadTracer : MonoBehaviour, IRuntimeGizmoComponent {

    public static List<Vector3> averagedInterSectionPoints;

    public ARRaytracer raytracer;
    public OpticalCalibrationManager manager;
    public Transform newScreen;

    [Range(0.1f, 2f)]
    public float focalDistance = 0.5f;

    Vector3 curFocalPoint = Vector3.zero;

    [Range(0, 400)]
    public int whichToDraw = 2;
    public float curDrawing = 0;

    [Range(0.001f, 0.2f)]
    public float constantDistance = 0.5f;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      Quaternion beginningRot = transform.rotation;
      if (curFocalPoint.ContainsNaN()) curFocalPoint = Vector3.zero;

      averagedInterSectionPoints = new List<Vector3>();

      ARRaytracer.OpticalSystem optics = raytracer.optics;

      curDrawing = 0;
      for (float yRot = -30f; yRot < 30f; yRot += 5f) {
        for (float xRot = -20f; xRot < 70f; xRot += 5f) {
          //for (float yRot = -0f; yRot < 25f; yRot += 5f) {
          //  for (float xRot = 5f; xRot < 35f; xRot += 5f) {
          List<Ray> intersectRayList = new List<Ray>();

          for (float i = 0f; i < 91; i += 90) {
            transform.rotation = Quaternion.Euler(0, 0, i);// Quaternion.Euler(xRot, yRot, i);
            Vector3 convergencePoint = transform.position + ((Vector3.forward) +
                                      (Vector3.up * -xRot *0.01f) + (Vector3.right * yRot * 0.01f))*focalDistance;

            optics.eyePosition = transform.position + (transform.right * 0.001f);
            Ray firstBounceRay = traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
            intersectRayList.Add(firstBounceRay);

            transform.rotation = Quaternion.Euler(0, 0, i + 180f); //Quaternion.Euler(xRot, yRot, i+180f);
            optics.eyePosition = transform.position + (transform.right * 0.001f);
            firstBounceRay = traceRay((convergencePoint - optics.eyePosition).normalized, optics, whichToDraw == curDrawing ? drawer : null);
            intersectRayList.Add(firstBounceRay);
          }

          Vector3 intersectSum = Vector3.zero;
          int numPoints = 0;

          for (int i = 0; i < (intersectRayList.Count - 1); i+=2) {
            Vector3 tmp = Fit.ClosestPointOnRayToRay(intersectRayList[i], intersectRayList[i + 1]);
            drawer.color = i==0?LeapColor.coral : LeapColor.forest;
            drawer.DrawSphere(tmp, 0.0005f);
            intersectSum += tmp;

            numPoints++;
          }

          Vector3 ptAverage = intersectSum / numPoints;
          averagedInterSectionPoints.Add(ptAverage);

          /*for (int i = 0; i < (intersectRayList.Count - 1); i += 2) {
            Vector3 tmp = ClosestPointOnRayToRay(intersectRayList[i], intersectRayList[i + 1]);
            drawer.color = LeapColor.red;
            drawer.DrawLine(tmp, ptAverage);
          }*/

          //Debug.Log("Average: " + ptAverage);
          curDrawing++;
        }
      }

      optics.eyePosition = raytracer.eyePerspective.transform.position;

      //drawer.color = LeapColor.periwinkle;
      //for (int i = 0; i < averagedInterSectionPoints.Count; i++) drawer.DrawSphere(averagedInterSectionPoints[i], .0005f);

      Vector3 position = Vector3.zero; Vector3 normal = Vector3.zero;
      Fit.Plane(averagedInterSectionPoints, out position, out normal, 200, drawer);
      newScreen.position = Vector3.ProjectOnPlane(newScreen.position - position, normal) + position;
      newScreen.rotation = Quaternion.FromToRotation(newScreen.forward, normal) * newScreen.rotation;

      //log the continous fit to the console
      ///Debug.Log("Position is " + position + "  normal is " + normal);
      transform.rotation = beginningRot;

      Vector2 CornerOneUV = ARRaytracer.DisplayUVToRenderUV(new Vector2(0f, 0f), optics, 80);
      Vector2 CornerTwoUV = ARRaytracer.DisplayUVToRenderUV(new Vector2(1f, 0f), optics, 80);
      Vector2 CornerThreeUV = ARRaytracer.DisplayUVToRenderUV(new Vector2(0f, 1f), optics, 80);
      Vector2 CornerFourUV = ARRaytracer.DisplayUVToRenderUV(new Vector2(1f, 1f), optics, 80);

      drawer.matrix = Matrix4x4.identity;
      traceRay(ARRaytracer.ViewportPointToRayDirection(new Vector3(CornerOneUV.x, CornerOneUV.y, 1f),
        raytracer.eyePerspective.transform.position, optics.clipToWorld), optics, drawer);
      traceRay(ARRaytracer.ViewportPointToRayDirection(new Vector3(CornerTwoUV.x, CornerTwoUV.y, 1f),
        raytracer.eyePerspective.transform.position, optics.clipToWorld), optics, drawer);
      traceRay(ARRaytracer.ViewportPointToRayDirection(new Vector3(CornerThreeUV.x, CornerThreeUV.y, 1f),
        raytracer.eyePerspective.transform.position, optics.clipToWorld), optics, drawer);
      traceRay(ARRaytracer.ViewportPointToRayDirection(new Vector3(CornerFourUV.x, CornerFourUV.y, 1f),
        raytracer.eyePerspective.transform.position, optics.clipToWorld), optics, drawer);
    }

    public static Ray traceRay(Vector3 rayDirection, ARRaytracer.OpticalSystem optics, RuntimeGizmoDrawer drawer = null) {
      //Debug.Log(optics.eyePosition.ToString("G3"));
      Vector3 sphereSpaceRayOrigin = optics.worldToSphereSpace.MultiplyPoint(optics.eyePosition);
      Vector3 sphereSpaceRayDirection = (optics.worldToSphereSpace.MultiplyPoint(optics.eyePosition + rayDirection) - sphereSpaceRayOrigin);
      sphereSpaceRayDirection = sphereSpaceRayDirection / sphereSpaceRayDirection.magnitude;
      float intersectionTime = ARRaytracer.intersectLineSphere(sphereSpaceRayOrigin, sphereSpaceRayDirection, Vector3.zero, 0.5f * 0.5f, false);

      if (intersectionTime < 0f) {
        Debug.Log("bad ray....");
        return new Ray();
      }
      Vector3 sphereSpaceIntersection = sphereSpaceRayOrigin + (intersectionTime * sphereSpaceRayDirection);

      //Ellipsoid  Normals
      Vector3 sphereSpaceNormal = -sphereSpaceIntersection / sphereSpaceIntersection.magnitude;
      sphereSpaceNormal = new Vector3(
        sphereSpaceNormal.x / Mathf.Pow(optics.ellipseMinorAxis / 2f, 2f), 
        sphereSpaceNormal.y / Mathf.Pow(optics.ellipseMinorAxis / 2f, 2f), 
        sphereSpaceNormal.z / Mathf.Pow(optics.ellipseMajorAxis / 2f, 2f));
      sphereSpaceNormal /= sphereSpaceNormal.magnitude;

      Vector3 worldSpaceIntersection = optics.sphereToWorldSpace.MultiplyPoint(sphereSpaceIntersection);
      Vector3 worldSpaceNormal = optics.sphereToWorldSpace.MultiplyVector(sphereSpaceNormal);
      worldSpaceNormal /= worldSpaceNormal.magnitude;

      Ray firstBounce = new Ray(worldSpaceIntersection, Vector3.Reflect(rayDirection, worldSpaceNormal));

      if (drawer != null) {
        //float halfDistance = constantDistance - Vector3.Distance(optics.eyePosition, firstBounce.origin);
        drawer.DrawLine(optics.eyePosition, firstBounce.origin);
        drawer.DrawLine(firstBounce.origin, firstBounce.origin + (firstBounce.direction * 0.5f));
        //drawer.DrawSphere(firstBounce.origin + (firstBounce.direction * halfDistance), 0.001f);
      }

      // we just need the first bounce
      return firstBounce;
    }

    ///MATH UTILITIES

    public static class Fit {
      //These techniques should be extensible to n-dimensions

      public static void Line(List<Vector3> points, out Vector3 position,
      ref Vector3 direction, int iters = 100, RuntimeGizmoDrawer drawer = null) {
        if (
        direction == Vector3.zero ||
        float.IsNaN(direction.x) ||
        float.IsInfinity(direction.x)) direction = Vector3.right;

        //Calculate Average
        position = Vector3.zero;
        for (int i = 0; i < points.Count; i++) position += points[i];
        position /= points.Count;

        //Step the optimal fitting line approximation
        for (int iter = 0; iter < iters; iter++) {
          Vector3 accumulatedOffset = Vector3.zero; float sum = 0f;
          for (int i = 0; i < points.Count; i++) {
            float alpha = TimeAlongSegment(points[i], position, position + direction);
            Vector3 lineToPointOffset = points[i] - Vector3.LerpUnclamped(position, position + direction, alpha);
            accumulatedOffset += lineToPointOffset * alpha;
            sum += alpha * alpha;

            if (drawer != null) {
              Gizmos.color = Color.red;
              Gizmos.DrawRay(points[i], -lineToPointOffset);
            }
          }
          direction += accumulatedOffset / sum;
          direction = direction.normalized;
        }
        if (drawer != null) {
          Gizmos.color = Color.white;
          Gizmos.DrawRay(position, direction * 2f);
          Gizmos.DrawRay(position, -direction * 2f);
        }
      }

      public static float Plane(List<Vector3> points, out Vector3 position,
        out Vector3 normal, int iters = 200, RuntimeGizmoDrawer drawer = null) {

        //Find the primary principal axis
        Vector3 primaryDirection = Vector3.right;
        Line(points, out position, ref primaryDirection, iters / 2);

        //Flatten the points along that axis
        List<Vector3> flattenedPoints = new List<Vector3>(points);
        for (int i = 0; i < flattenedPoints.Count; i++)
          flattenedPoints[i] = Vector3.ProjectOnPlane(points[i] - position, primaryDirection) + position;

        //Find the secondary principal axis
        Vector3 secondaryDirection = Vector3.right;
        Line(flattenedPoints, out position, ref secondaryDirection, iters / 2);

        normal = Vector3.Cross(primaryDirection, secondaryDirection).normalized;

        float residualSum = 0f;
        foreach (Vector3 point in points) residualSum += Vector3.Distance(point, Vector3.ProjectOnPlane(point - position, normal) + position);

        if (drawer != null) {
          drawer.color = Color.red;
          //foreach (Vector3 point in points) drawer.DrawLine(point, Vector3.ProjectOnPlane(point - position, normal) + position);
          drawer.color = Color.blue;
          drawer.DrawLine(position, position + (normal * 0.02f));
          drawer.DrawLine(position, position - (normal * 0.02f));
          drawer.matrix = Matrix4x4.TRS(position, Quaternion.LookRotation(normal, primaryDirection), new Vector3(0.025f, 0.025f, 0.001f));
          drawer.DrawWireSphere(Vector3.zero, 1f);
          drawer.matrix = Matrix4x4.identity;
        }

        return residualSum;
      }

      public static float TimeAlongSegment(Vector3 position, Vector3 a, Vector3 b) {
        Vector3 ba = b - a;
        return Vector3.Dot(position - a, ba) / ba.sqrMagnitude;
      }

      public static float ClosestTimeOnSegmentToLine(Vector3 segA, Vector3 segB, Vector3 lineA, Vector3 lineB) {
        Vector3 lineBA = lineB - lineA; float lineDirSqrMag = Vector3.Dot(lineBA, lineBA);
        Vector3 inPlaneA = segA - ((Vector3.Dot(segA - lineA, lineBA) / lineDirSqrMag) * lineBA),
               inPlaneB = segB - ((Vector3.Dot(segB - lineA, lineBA) / lineDirSqrMag) * lineBA);
        Vector3 inPlaneBA = inPlaneB - inPlaneA;
        return (inPlaneA != inPlaneB) ? Vector3.Dot(lineA - inPlaneA, inPlaneBA) / Vector3.Dot(inPlaneBA, inPlaneBA) : 0f;
      }

      public static Vector3 ClosestPointOnRayToRay(Ray a, Ray b) {
        return Vector3.LerpUnclamped(b.origin, b.origin + b.direction, ClosestTimeOnSegmentToLine(b.origin, b.origin + b.direction, a.origin, a.origin + a.direction));
      }

      public static Vector3 ConstrainToSegment(Vector3 position, Vector3 a, Vector3 b) {
        Vector3 ba = b - a;
        return Vector3.Lerp(a, b, Vector3.Dot(position - a, ba) / ba.sqrMagnitude);
      }
    }
  }
}
