using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyTrackerZed : EskyTracker
    {
        public List<Mesh> myMeshes;

        [MonoPInvokeCallback(typeof(MeshChunksReceivedCallback))]
        static void OnMapCallback()
        {            //System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
        }
        public override void ObtainPose(){
            IntPtr ptr = GetLatestPose();                
            Marshal.Copy(ptr, currentRealsensePose, 0, 7);
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]),ref velocity,smoothing); 
            Quaternion q = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);
            currentEuler = Vector3.SmoothDamp(transform.localRotation.eulerAngles,q.eulerAngles,ref velocityRotation,smoothingRotation);
            transform.localRotation = Quaternion.Euler(currentEuler);    

            Matrix4x4 m = Matrix4x4.TRS(transform.transform.position,transform.transform.rotation,Vector3.one);
            m = m * TransformFromTrackerToCenter.inverse;
            if(RigCenter != null){
                RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
                RigCenter.transform.rotation = m.rotation;
            }
        }
        delegate void MeshChunksReceivedCallback();
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void SetMapData(byte[] inputData, int Length);
    }
}