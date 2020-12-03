/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Leap.Unity.AR {
  [ExecuteInEditMode]
  public class WindowOffsetManager : MonoBehaviour {

    private static Vector2Int s_windowShift = Vector2Int.zero;
    /// <summary>
    /// Statically configured window shift value, set via the WindowOffset component in
    /// the editor -- of which there should only ever be one.
    /// </summary>
    public static Vector2Int WindowShift {
      get {
        return s_windowShift;
      }
    }

    //#if UNITY_STANDALONE_WIN //|| UNITY_EDITOR
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(System.String className, System.String windowName);

    [Tooltip("Shift the window (X coord) to the AR headset monitor.")]
    public int xShift = 1920;
    [Tooltip("Shift the window (Y coord) to the AR headset monitor.")]
    public int yShift = 0;

    public static void SetPosition(int x, int y, int resX = 0, int resY = 0) {
      SetWindowPos(FindWindow(null, Application.productName), 0, x, y, resX, resY, resX * resY == 0 ? 1 : 0);
    }

    void Awake() {
      if (GetComponent<CalibrationWindowOffset>() != null) enabled = false;
      if (Application.isPlaying && !Application.isEditor) {
        Application.targetFrameRate = 120;

        if (Config.TryRead<int>("windowManagerXShift", ref xShift)) {
          Debug.Log("Loaded X shift from Config: " + xShift);
        }

        if (!enabled) { return; }
        if (!Screen.fullScreen) {
          StartCoroutine(Position());
        } else {
          SetPosition(xShift, 0, 2880, 1600);
        }
      }
    }

    IEnumerator Position() {
      Screen.fullScreen = false;
      Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
      yield return new WaitForSeconds(2f);
      SetPosition(xShift, 0, 2880, 1600);
      yield return new WaitForSeconds(1f);
      Screen.fullScreen = true;
      yield return new WaitForSeconds(1f);
      //if (SystemInfo.graphicsDeviceType ==
      //    UnityEngine.Rendering.GraphicsDeviceType.Direct3D11) {
      //  Application.Quit();
      //}
      yield return null;

    }

    private void Update() {
      if (!enabled) { return; }
      if (Application.isPlaying) {
        if (Input.GetKeyDown(KeyCode.V)) {
          if (Application.targetFrameRate == -1000) {
            Application.targetFrameRate = 120;
          } else {
            Application.targetFrameRate = -1000;
          }
        }
      }
#if UNITY_EDITOR
    s_windowShift = new Vector2Int(xShift, yShift);
#endif
    }
    //#endif
  }
}
