using UnityEngine;
using Leap;
using Leap.Unity;

public class ImageRetreiverSwitcher : MonoBehaviour {
  public KeyCode[] switchKeys;
	void Update () {
    for (int i = 0; i < switchKeys.Length; i++) {
      if (Input.GetKeyDown(switchKeys[i])) {
        //GetComponent<LeapImageRetriever>().shaderDataDevice = (uint)i + 1;
      }
    }
	}
}
