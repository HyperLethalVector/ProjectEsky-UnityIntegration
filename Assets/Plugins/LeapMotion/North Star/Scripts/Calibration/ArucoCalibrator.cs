using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.AR.Testing {
  public class ArucoCalibrator : MonoBehaviour {
    public ArUcoProvider provider;
    public RayOptimizer leftOptimizer;
    public RayOptimizer rightOptimizer;
    //public Transform leftCanonicalPointTransforms;
    //public Transform rightCanonicalPointTransforms;
    public Transform[] measuredPointTransforms;
    public KeyCode AddLeftMeasurementKey;
    public KeyCode AddRightMeasurementKey;
    public KeyCode SaveMeasurementKey;

    // Use this for initialization
    void Start() {
      //Ray cameraRay = leftOptimizer.raytracer.eyePerspective.ViewportPointToRay(Vector3.one * 0.5f);
      //leftCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.3f);
      //leftCanonicalPointTransforms.LookAt(cameraRay.origin);
      //cameraRay = rightOptimizer.raytracer.eyePerspective.ViewportPointToRay(Vector3.one * 0.5f);
      //rightCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.3f);
      //rightCanonicalPointTransforms.LookAt(cameraRay.origin);
    }

    // Update is called once per frame
    void Update() {
      RuntimeGizmoDrawer drawer;
      RuntimeGizmoManager.TryGetGizmoDrawer(out drawer);

      for (int i = 0; i < provider.points.Length; i++) {
        drawer.DrawSphere(provider.points[i], 0.005f);
      }

      if (provider.points.Length == 4) {
        transform.position = (provider.points[0] + provider.points[1] + provider.points[2] + provider.points[3]) / 4.0f;
        //transform.rotation = Quaternion.LookRotation(provider.points[0] - provider.points[1], provider.points[0] - provider.points[2]);
        /*
        bool leftUpdate = false, rightUpdate = false;
        if (Input.GetKey(AddLeftMeasurementKey)) {
          drawer.DrawLine(leftCanonicalPointTransforms.position, transform.position);
          leftOptimizer.drawCorrespondence(leftCanonicalPointTransforms.position, transform.position, drawer);
        } else if (Input.GetKey(AddRightMeasurementKey)) {
          drawer.DrawLine(rightCanonicalPointTransforms.position, transform.position);
          rightOptimizer.drawCorrespondence(rightCanonicalPointTransforms.position, transform.position, drawer);
        }

        if (Input.GetKeyUp(AddLeftMeasurementKey)) {
          leftOptimizer.AddPosition(leftCanonicalPointTransforms.position, transform.position);
          leftUpdate = true;
        } else if (Input.GetKeyUp(AddRightMeasurementKey)) {
          rightOptimizer.AddPosition(rightCanonicalPointTransforms.position, transform.position);
          rightUpdate = true;
        }

        //Advance the canonical point transforms
        if (leftUpdate) {
          Ray cameraRay = leftOptimizer.raytracer.eyePerspective.ViewportPointToRay(((Vector3)Random.insideUnitCircle * 0.4f) + (Vector3.one * 0.5f));
          leftCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.25f);
          leftCanonicalPointTransforms.LookAt(cameraRay.origin);
        }
        if (rightUpdate) {
          Ray cameraRay = rightOptimizer.raytracer.eyePerspective.ViewportPointToRay(((Vector3)Random.insideUnitCircle * 0.4f) + (Vector3.one * 0.5f));
          rightCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.25f);
          rightCanonicalPointTransforms.LookAt(cameraRay.origin);
        }*/

        if (Input.GetKeyUp(SaveMeasurementKey)) {
          rightOptimizer.Save();
          leftOptimizer.Save();
        }
        /*
        float avgSideLength = 
          (Vector3.Distance(provider.points[0], provider.points[1]) +
           Vector3.Distance(provider.points[1], provider.points[2]) +
           Vector3.Distance(provider.points[2], provider.points[3]) +
           Vector3.Distance(provider.points[3], provider.points[0])) / 4f;
        leftCanonicalPointTransforms.localScale = Vector3.one * avgSideLength;
        rightCanonicalPointTransforms.localScale = Vector3.one * avgSideLength;

        bool leftUpdate = false, rightUpdate = false;
        for (int i = 0; i < provider.points.Length; i++) {
          measuredPointTransforms[i].position = provider.points[i];
          if (Input.GetKey(AddLeftMeasurementKey)) {
            drawer.DrawLine(leftCanonicalPointTransforms.GetChild(i).position, provider.points[i]);
            leftOptimizer.drawCorrespondence(leftCanonicalPointTransforms.GetChild(i).position, provider.points[i], drawer);
          } else if (Input.GetKey(AddRightMeasurementKey)) {
            drawer.DrawLine(rightCanonicalPointTransforms.GetChild(i).position, provider.points[i]);
            rightOptimizer.drawCorrespondence(rightCanonicalPointTransforms.GetChild(i).position, provider.points[i], drawer);
          }

          if (Input.GetKeyUp(AddLeftMeasurementKey)) {
            leftOptimizer.AddPosition(leftCanonicalPointTransforms.GetChild(i).position, provider.points[i]);
            leftUpdate = true;
          } else if (Input.GetKeyUp(AddRightMeasurementKey)) {
            rightOptimizer.AddPosition(rightCanonicalPointTransforms.GetChild(i).position, provider.points[i]);
            rightUpdate = true;
          }
        }

        //Advance the canonical point transforms (also ensure they're at the same width as the measured aruco marker
        if (leftUpdate) {
          Ray cameraRay = leftOptimizer.raytracer.eyePerspective.ViewportPointToRay(((Vector3)Random.insideUnitCircle * 0.4f) + (Vector3.one * 0.5f));
          leftCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.25f);
          leftCanonicalPointTransforms.LookAt(cameraRay.origin);
        }
        if (rightUpdate) {
          Ray cameraRay = rightOptimizer.raytracer.eyePerspective.ViewportPointToRay(((Vector3)Random.insideUnitCircle * 0.4f) + (Vector3.one * 0.5f));
          rightCanonicalPointTransforms.position = cameraRay.origin + (cameraRay.direction * 0.25f);
          rightCanonicalPointTransforms.LookAt(cameraRay.origin);
        }

        if (Input.GetKeyUp(SaveMeasurementKey)) {
          rightOptimizer.Save();
          leftOptimizer.Save();
        }*/
      }
    }
  }
}