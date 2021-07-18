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
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    int Width = 2880;
    int Height = 1600;
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
    public void SetManually(float offsetX, float offsetY, float width, float height){
      Debug.Log("Setting Manually via Esky");
      xShift = (int)offsetX;
      yShift = (int)offsetY;
      Width = (int)width;
      Height = (int)height;
      if (GetComponent<CalibrationWindowOffset>() != null) enabled = false;
        if (Application.isPlaying && !Application.isEditor) {
          Application.targetFrameRate = 120;

//          if (Config.TryRead<int>("windowManagerXShift", ref xShift)) {
  //          Debug.Log("Loaded X shift from Config: " + xShift);
    //      }

          if (!enabled) { return; }
          if (!Screen.fullScreen) {
            StartCoroutine(Position());
          } else {
            SetPosition((int)offsetX, (int)offsetY, (int)height, (int)width);
          }
        }
        #if UNITY_EDITOR
        Debug.Log("Opening the game view window via esky");
        LayoutViewsInternal((int)offsetX, (int)offsetY, (int)width, (int)height);
        #endif
    }
    IEnumerator Position() {
      Screen.fullScreen = false;

      yield return new WaitForSeconds(2f);
      SetPosition(xShift, yShift, Height, Width);
      yield return new WaitForSeconds(1f);
      Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;      
      Screen.fullScreen = true;
      yield return new WaitForSeconds(1f);
      //if (SystemInfo.graphicsDeviceType ==
      //    UnityEngine.Rendering.GraphicsDeviceType.Direct3D11) {
      //  Application.Quit();
      //}
      yield return null;

    }
    void OnDestroy(){
      #if UNITY_EDITOR
      CloseWindow();
      #endif
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

    #if UNITY_EDITOR
    const int UNITY_MENU_HEIGHT_SIZE = 11;
    const int UNITY_MENU_HEIGHT_POSITION = 12;
    EditorWindow headsetView;
    System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
    public void LayoutViewsInternal(int x, int y, int width, int height) {
      //CloseAllViews(GameViewType);
      headsetView = (EditorWindow)ScriptableObject.CreateInstance(gameViewType);
      EditorWindow view = headsetView;
      view.Show();
      ChangeTargetDisplay(view, 0);
      SendViewToScreenInternal(view, new Vector2Int(x,y), new Vector2Int(width, height));
    }
     static void ChangeTargetDisplay(EditorWindow view, int displayIndex) {
      var serializedObject = new SerializedObject(view);
      var targetDisplay = serializedObject.FindProperty("m_TargetDisplay");
      targetDisplay.intValue = displayIndex;
      serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
    public void SendViewToScreenInternal(EditorWindow view, Vector2Int position, Vector2Int resolution) {

      var size = new Vector2(resolution.x, resolution.y + UNITY_MENU_HEIGHT_SIZE);


      Vector2 windowPosition = position + Vector2Int.down * (UNITY_MENU_HEIGHT_POSITION);
      view.position = new Rect(windowPosition, size);
      view.minSize = view.maxSize = size;
      EditorApplication.delayCall += () => {
        //Debug.Log(view.position.size);
        view.position = new Rect(windowPosition, view.position.size);
      };
    }    
    // Send a game view to a given screen.
    void CloseWindow(){
      headsetView.Close();
    }
    #endif
    //#endif
  }
}
