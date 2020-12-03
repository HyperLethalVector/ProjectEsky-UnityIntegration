/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity.AR {

  [CustomEditor(typeof(WindowOffsetManager))]
  public class WindowOffsetManagerEditor : CustomEditorBase<WindowOffsetManager> {
    //THESE USED TO BOTH BE 22, BUT NOW IT DOESN'T SEEM LIKE IT
    //I WONDER IF PRO EDITORS HAVE A DIFFERENT MENU HEIGHT
    const int UNITY_MENU_HEIGHT_SIZE = 11;
    const int UNITY_MENU_HEIGHT_POSITION = 12;
    EditorWindow headsetView, calibrationView;

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");

      if (GUILayout.Button("Move Game View to Headset")) {
        target.enabled = true;
        LayoutViews(gameViewType);
      }
      if (GUILayout.Button("Move Calibration View to Monitor")) {
        target.enabled = true;
        LayoutViews(gameViewType, true);
      }
      if (GUILayout.Button("Enforce Positions")) {
        target.enabled = true;
        EnforceViewPositions();
      }
      if (GUILayout.Button("Close All Game Views")) {
        CloseAllViews(gameViewType);
      }
    }

    // Instantiate and layout game views based on the setting.
    void LayoutViews(System.Type GameViewType, bool isCalibrationView = false) {
      //CloseAllViews(GameViewType);
      if (!isCalibrationView) {
        headsetView = (EditorWindow)CreateInstance(GameViewType);
      } else {
        calibrationView = (EditorWindow)CreateInstance(GameViewType);
      }

      EditorWindow view = isCalibrationView ? calibrationView : headsetView;
      view.Show();

      if (target.GetComponent<CalibrationWindowOffset>()) {
        // Setup for the calibration rig.
        ChangeTargetDisplay(view, isCalibrationView ? 1 : 2);
      } else {
        // The normal North Star display scheme pre-SteamVR.
        ChangeTargetDisplay(view, 0);
      }

      if (isCalibrationView) {
        SendViewToScreen(view, new Vector2Int(-1920, 0), new Vector2Int(1920, 1080));
      } else {
        SendViewToScreen(view, WindowOffsetManager.WindowShift, new Vector2Int(2880, 1600));
      }
    }

    // Send a game view to a given screen.
    void SendViewToScreen(EditorWindow view, Vector2Int position, Vector2Int resolution) {

      var size = new Vector2(resolution.x, resolution.y + UNITY_MENU_HEIGHT_SIZE);


      Vector2 windowPosition = position + Vector2Int.down * (UNITY_MENU_HEIGHT_POSITION);
      view.position = new Rect(windowPosition, size);
      view.minSize = view.maxSize = size;

      bool prevEnabled = target.enabled;
      target.enabled = true;

      EditorApplication.delayCall += () => {
        //Debug.Log(view.position.size);
        view.position = new Rect(windowPosition, view.position.size);
        //Debug.Log(view.position.position);
        target.enabled = prevEnabled;
      };
    }

    // Send a game view to a given screen.
    void EnforceViewPositions() {
      EditorApplication.delayCall += () => {
        if (calibrationView != null) { calibrationView.position = new Rect(new Vector2Int(-1920, 0) + Vector2Int.down * (UNITY_MENU_HEIGHT_POSITION), calibrationView.position.size); }
        if (headsetView != null) { headsetView.position = new Rect(WindowOffsetManager.WindowShift + Vector2Int.down * (UNITY_MENU_HEIGHT_POSITION), headsetView.position.size); }
        //Debug.Log(headsetView.position.position);
        //Debug.Log(calibrationView.position.position);
      };
    }

    // Change the target display of a game view.
    static void ChangeTargetDisplay(EditorWindow view, int displayIndex) {
      var serializedObject = new SerializedObject(view);
      var targetDisplay = serializedObject.FindProperty("m_TargetDisplay");
      targetDisplay.intValue = displayIndex;
      serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    // Close all the game views.
    static void CloseAllViews(System.Type GameViewType) {
      foreach (EditorWindow view in Resources.FindObjectsOfTypeAll(GameViewType))
        view.Close();
    }
  }
}
