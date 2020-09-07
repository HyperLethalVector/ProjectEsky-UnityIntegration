using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class EskyTracker : MonoBehaviour
{
    bool didInitializeTracker = false;
    Vector3 velocity = Vector3.zero;
    public float smoothing = 0.1f;
    float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
    public void ObtainPose(){
        IntPtr ptr = GetLatestPose();                
        Marshal.Copy(ptr, currentRealsensePose, 0, 7);
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition,            new Vector3(currentRealsensePose[0],currentRealsensePose[1],-currentRealsensePose[2]),ref velocity,smoothing); 
        Quaternion q = new Quaternion(currentRealsensePose[3],currentRealsensePose[4],currentRealsensePose[5],currentRealsensePose[6]);            
        transform.localRotation = Quaternion.Euler(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z);    
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
