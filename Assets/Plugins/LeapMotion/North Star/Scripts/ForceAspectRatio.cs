/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
namespace Leap.Unity.AR {
  [ExecuteInEditMode]
  public class ForceAspectRatio : MonoBehaviour {
    public float aspect = 1.8f;
    void Start() {
      GetComponent<Camera>().aspect = aspect;
    }
  }
}
