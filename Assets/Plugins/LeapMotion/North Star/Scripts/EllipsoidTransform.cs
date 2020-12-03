/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.AR {
  public class EllipsoidTransform : MonoBehaviour, IRuntimeGizmoComponent {
    public Transform Foci1;
    public Transform Foci2;
    public float MinorAxis = 0.1f;
    [HideInInspector]
    public float MajorAxis;
    [HideInInspector]
    public Matrix4x4 worldToSphereSpace;
    [HideInInspector]
    public Matrix4x4 sphereToWorldSpace;

    public bool drawGizmos = true;

    void Start() {
      UpdateEllipsoid();
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!enabled || !gameObject.activeInHierarchy || !drawGizmos) {
        return;
      }
      DrawEllipse(drawer, Foci1.position, Foci2.position, MinorAxis);
    }

    /// <summary>
    /// Return true if the ellipsoid actually updated, false otherwise.
    /// The ellipsoid will only actually update if it's enabled.
    /// It's only supposed to be enabled while calibrating, but not when loading
    /// a calibration, because loading a calibration doesn't actually adjust the
    /// EllipsoidTransform. So instead the calibration manager turns off the
    /// ellipsoids when a calibration is loaded, instead of just adjusting it.
    /// _Sometimes._ Or something.
    /// Anyway you need to know whether the ellipsoid was _actually_ updated
    /// because otherwise you might transform to correct its values when the
    /// values were actually in the correct space the whole time, e.g., when
    /// the head is tracked but the ellipsoid isn't actually updating and
    /// reflects headset space already.
    /// </summary>
    public bool UpdateEllipsoid() {
      if (!enabled) { return false; }
      Vector3 ellipseCenter = (Foci1.position + Foci2.position) / 2f;
      Quaternion ellipseRotation = Quaternion.LookRotation(Foci1.position - Foci2.position);
      MajorAxis = Mathf.Sqrt(Mathf.Pow(Vector3.Distance(Foci1.position, Foci2.position) / 2f, 2f) + Mathf.Pow(MinorAxis / 2f, 2f)) * 2f;
      Vector3 ellipseScale = new Vector3(MinorAxis, MinorAxis, MajorAxis);
      sphereToWorldSpace = Matrix4x4.TRS(ellipseCenter, ellipseRotation, ellipseScale);
      worldToSphereSpace = sphereToWorldSpace.inverse;
      return true;
    }

    public void DrawEllipse(RuntimeGizmoDrawer drawer, Vector3 foci1, Vector3 foci2, float MinorAxis) {
      drawer.PushMatrix();
      if (enabled) {
        Vector3 ellipseCenter = (foci1 + foci2) / 2f;
        Quaternion ellipseRotation = Quaternion.LookRotation(foci1 - foci2);
        MajorAxis = Mathf.Sqrt(Mathf.Pow(Vector3.Distance(foci1, foci2) / 2f, 2f) + Mathf.Pow(MinorAxis / 2f, 2f)) * 2f;
        Vector3 ellipseScale = new Vector3(MinorAxis, MinorAxis, MajorAxis);

        drawer.matrix = Matrix4x4.TRS(ellipseCenter, ellipseRotation, ellipseScale);
        sphereToWorldSpace = drawer.matrix;
        worldToSphereSpace = sphereToWorldSpace.inverse;
      } else {
        drawer.matrix = sphereToWorldSpace;
      }

      drawer.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);
      drawer.DrawWireSphere(Vector3.zero, 0.5f);
      drawer.PopMatrix();
    }
  }
}
