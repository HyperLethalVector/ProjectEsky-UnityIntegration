using UnityEngine;
using System;
using System.Threading;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using ProjectCupboard.Renderer;
namespace ProjectCupboard.Tracking{
    public class CupboardTracker : MonoBehaviour
    {
        public Transform RigCenter;
        public Matrix4x4 TransformFromTrackerToCenter;
        bool doTracking = false;
        protected virtual void UpdateTracking(){

        }

    }
}