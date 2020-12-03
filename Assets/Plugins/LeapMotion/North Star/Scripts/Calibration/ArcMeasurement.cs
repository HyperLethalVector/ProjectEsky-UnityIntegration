using UnityEngine;
using Leap.Unity.RuntimeGizmos;

public class ArcMeasurement : MonoBehaviour, IRuntimeGizmoComponent {
  public Transform a, b;
	public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    drawer.color = Color.green;
    Vector3 aRay = a.position - transform.position;
    Vector3 bRay = b.position - transform.position;
    Debug.Log(gameObject.name +": "+Vector3.Angle(aRay, bRay) + "°");

    float distance = aRay.magnitude;
    b.position = ((b.position - transform.position).normalized * distance) + transform.position;
    for (float i = 0f; i < 1f; i+=0.01f) {
      drawer.DrawLine(((Vector3.Lerp(a.position, b.position, i) - transform.position).normalized* distance) + transform.position, 
                      ((Vector3.Lerp(a.position, b.position, i+0.01f) - transform.position).normalized* distance) + transform.position);
    }
  }
}
