/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.AR.Testing {

  public class KeyCodeChildToggle : MonoBehaviour {

    public bool childrenStartEnabled = false;
    public KeyCode toggleKey = KeyCode.Alpha2;

    private void Start() {
      for (int i = 0; i < transform.childCount; i++) {
        transform.GetChild(i).gameObject.SetActive(childrenStartEnabled);
      }
    }

    private void Update() {
      if (Input.GetKeyDown(toggleKey)) {
        for (int i = 0; i < transform.childCount; i++) {
          transform.GetChild(i).gameObject.SetActive(!transform.GetChild(i).gameObject.activeSelf);
        }
      }
    }

  }

}
