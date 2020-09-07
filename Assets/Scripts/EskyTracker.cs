using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace ProjectEsky.Tracking{
    public class EskyTracker : MonoBehaviour
    {
        bool didInitializeTracker = false;
        Vector3 velocity = Vector3.zero;
        Vector3 velocityRotation = Vector3.zero;
        public float smoothing = 0.1f;
        public float smoothingRotation= 0.1f;
        float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        public Matrix4x4 TransformFromTrackerToCenter;
        public Transform RigCenter;
        Vector3 currentEuler = Vector3.zero;
        public void ObtainPose(){
            IntPtr ptr = GetLatestPose();                
            Marshal.Copy(ptr, currentRealsensePose, 0, 7);
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition,            new Vector3(currentRealsensePose[0],currentRealsensePose[1],-currentRealsensePose[2]),ref velocity,smoothing); 
            Quaternion q = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);            
            currentEuler = Vector3.SmoothDamp(transform.localRotation.eulerAngles,new Vector3(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z),ref velocityRotation,smoothingRotation);
            transform.localRotation = Quaternion.Euler(currentEuler);    

            Matrix4x4 m = Matrix4x4.TRS(transform.transform.position,transform.transform.rotation,Vector3.one);
                            m = m * TransformFromTrackerToCenter.inverse;
                            RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
                            RigCenter.transform.rotation = m.rotation;
        }    
        // Start is called before the first frame update
        void Start()
        {
            StartTrackerThread(false);        
        }

        // Update is called once per frame
        void Update()
        {
            ObtainPose();
        }
        [DllImport("libProjectEskyLLAPI")]
        private static extern IntPtr GetLatestPose();
        [DllImport("libProjectEskyLLAPI")]
        private static extern void StartTrackerThread(bool useLocalization);
    }
}