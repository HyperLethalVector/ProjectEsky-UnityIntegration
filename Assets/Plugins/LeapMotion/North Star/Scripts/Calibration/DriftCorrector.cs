using Leap.Unity.Attributes;
using UnityEngine;

public class DriftCorrector : MonoBehaviour {
  public Transform truth, reference;

  [MinValue(0f), MaxValue(1f)]
  public float strength = 0.004f;
  [MinValue(0f)]
  public float circleOfConfusionRadius = 0.04f;

  public float truthPositionMultiplier = 1f;

  [Header("Full Rotation Settings")]

  [Tooltip("Without this setting (off by default), OptiTrack will only correct yaw drift.")]
  public bool useFullRotation = false;

  [DisableIf("useFullRotation", isEqualTo: false)]
  public float fullRotationStrength = 0.50f;

  Vector3 lastPos = Vector3.zero;

  // Update is called once per frame
  void Update() {
    if (truth.position != lastPos) {
      lastPos = truth.position;

      var effectiveTruthPosition = truth.position * truthPositionMultiplier;
      float distance = (effectiveTruthPosition - reference.position).magnitude;
      if (distance > circleOfConfusionRadius) {
        transform.root.position += (effectiveTruthPosition - reference.position).normalized * (distance - circleOfConfusionRadius);
      } else {
        transform.root.position += (effectiveTruthPosition - reference.position) * strength;
      }

      Quaternion rotCorrection;
      if (useFullRotation) {
        Quaternion truthRotation = truth.rotation;
        Quaternion referenceRotation = reference.rotation;
        rotCorrection = Quaternion.Inverse(referenceRotation) * truthRotation;
      }
      else {
        Vector3 truthForward = Vector3.ProjectOnPlane(truth.rotation * (Vector3.forward + Vector3.up), Vector3.up);
        Vector3 referenceForward = Vector3.ProjectOnPlane(reference.rotation * (Vector3.forward + Vector3.up), Vector3.up);
        rotCorrection = Quaternion.FromToRotation(referenceForward, truthForward);
        //Quaternion correction = truth.rotation * Quaternion.Inverse(reference.rotation);
      }
      Vector3 rootToHere = transform.position - transform.root.position;
      transform.root.position += rootToHere;
      var effRotCorrection = Quaternion.Slerp(Quaternion.identity, rotCorrection, (useFullRotation ? fullRotationStrength : strength));
      transform.root.rotation = transform.root.rotation * effRotCorrection;
      transform.root.position -= effRotCorrection * rootToHere;
      //Vector3 axis; float angle;
      //rotCorrection.ToAngleAxis(out angle, out axis);
      //transform.root.RotateAround(transform.position, axis, angle);
    }
  }
}
