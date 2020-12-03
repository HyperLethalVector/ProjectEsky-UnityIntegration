using UnityEngine;
public class CalibrationWindowOffset : MonoBehaviour {
  void Start() {
    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    Screen.fullScreen = false;
    if (Display.displays.Length == 3) {
      for (int i = 0; i < Display.displays.Length; i++) {
        if (!Display.displays[i].active) {
          Display.displays[i].Activate();
        }
      }
    }
  }
}
