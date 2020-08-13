using UnityEngine;
using System;
using System.Threading;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using ProjectCupbard.Renderer;
namespace ProjectCupbard.Tracking{
public class CupboardTracker : ZEDManager
{
    public Transform RigCenter;
    public Matrix4x4 TransformFromTrackerToCenter;
    bool doTracking = false;
    protected override void UpdateTracking(){
        if (!zedReady)
            return;
        zedRigRoot.localRotation = zedOrientation;
        if (!ZEDSupportFunctions.IsVector3NaN(zedPosition))
            zedRigRoot.localPosition = zedPosition;

        Matrix4x4 m = Matrix4x4.TRS(zedRigRoot.transform.position,zedRigRoot.transform.rotation,Vector3.one);
        m = m * TransformFromTrackerToCenter.inverse;
        RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
        RigCenter.transform.rotation = m.rotation;

    }
}

}