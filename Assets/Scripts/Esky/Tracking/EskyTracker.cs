using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
namespace ProjectEsky.Tracking{
    #region 
    using UnityEngine;
    using System.Runtime.Serialization;
    using System.Collections;
    public enum Color { red, green, blue, black, white, yellow, orange };
    public class Vector3SerializationSurrogate : ISerializationSurrogate
    {
    
        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj,SerializationInfo info, StreamingContext context)
        {
    
            Vector3 v3 = (Vector3)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
        }
    
        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj,SerializationInfo info,
                                        StreamingContext context,ISurrogateSelector selector)
        {
    
            Vector3 v3 = (Vector3)obj;
            v3.x = (float)info.GetValue("x", typeof(float));
            v3.y = (float)info.GetValue("y", typeof(float));
            v3.z = (float)info.GetValue("z", typeof(float));
            obj = v3;
            return obj;
        }
    }
    public class QuaternionSerializationSurrogate : ISerializationSurrogate
    {
    
        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj,SerializationInfo info, StreamingContext context)
        {
    
            Quaternion v3 = (Quaternion)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
            info.AddValue("w",v3.w);
        }
    
        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj,SerializationInfo info,
                                        StreamingContext context,ISurrogateSelector selector)
        {
    
            Quaternion v3 = (Quaternion)obj;
            v3.x = (float)info.GetValue("x", typeof(float));
            v3.y = (float)info.GetValue("y", typeof(float));
            v3.z = (float)info.GetValue("z", typeof(float));
            v3.w = (float)info.GetValue("w", typeof(float));
            obj = v3;
            return obj;
        }
    }
    #endregion
   [System.Serializable]
    public class EskyAnchorContentInfo{
        [SerializeField]
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    [System.Serializable]
    public class EskyMap{
        public byte[] mapBLOB;
        public byte[] meshDataArray;        
        public Dictionary<string,EskyAnchorContentInfo> contentLocations;
        public byte[] GetBytes(){
            MemoryStream ms = new MemoryStream();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(Vector3),new StreamingContext(StreamingContextStates.All),new Vector3SerializationSurrogate());
            surrogateSelector.AddSurrogate(typeof(Quaternion),new StreamingContext(StreamingContextStates.All),new QuaternionSerializationSurrogate());                        
            BinaryFormatter bf  = new BinaryFormatter();
            bf.SurrogateSelector = surrogateSelector;
            bf.Serialize(ms,this);
            byte[] b = ms.ToArray();
            ms.Close();
            return b;
        }
        public static EskyMap GetMapFromArray(byte[] data){
            MemoryStream ms = new MemoryStream(data);
            BinaryFormatter bf  = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(Vector3),new StreamingContext(StreamingContextStates.All),new Vector3SerializationSurrogate());
            surrogateSelector.AddSurrogate(typeof(Quaternion),new StreamingContext(StreamingContextStates.All),new QuaternionSerializationSurrogate());                        
            bf.SurrogateSelector = surrogateSelector;                        
            try{
                EskyMap em = (EskyMap)bf.Deserialize(ms);
                return em;
            }catch(System.Exception e){
                UnityEngine.Debug.LogError("Couldn't load esky map from data!: " + e);
                return null;
            }
        }
    }

    public static class QuaternionUtil {	
        public static Quaternion AngVelToDeriv(Quaternion Current, Vector3 AngVel) {
            var Spin = new Quaternion(AngVel.x, AngVel.y, AngVel.z, 0f);
            var Result = Spin * Current;
            return new Quaternion(0.5f * Result.x, 0.5f * Result.y, 0.5f * Result.z, 0.5f * Result.w);
        } 

        public static Vector3 DerivToAngVel(Quaternion Current, Quaternion Deriv) {
            var Result = Deriv * Quaternion.Inverse(Current);
            return new Vector3(2f * Result.x, 2f * Result.y, 2f * Result.z);
        }

        public static Quaternion IntegrateRotation(Quaternion Rotation, Vector3 AngularVelocity, float DeltaTime) {
            if (DeltaTime < Mathf.Epsilon) return Rotation;
            var Deriv = AngVelToDeriv(Rotation, AngularVelocity);
            var Pred = new Vector4(
                    Rotation.x + Deriv.x * DeltaTime,
                    Rotation.y + Deriv.y * DeltaTime,
                    Rotation.z + Deriv.z * DeltaTime,
                    Rotation.w + Deriv.w * DeltaTime
            ).normalized;
            return new Quaternion(Pred.x, Pred.y, Pred.z, Pred.w);
        }
        
        public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time) {
            if (Time.deltaTime < Mathf.Epsilon) return rot;
            // account for double-cover
            var Dot = Quaternion.Dot(rot, target);
            var Multi = Dot > 0f ? 1f : -1f;
            target.x *= Multi;
            target.y *= Multi;
            target.z *= Multi;
            target.w *= Multi;
            // smooth damp (nlerp approx)
            var Result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;
            
            // ensure deriv is tangent
            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;		
            
            return new Quaternion(Result.x, Result.y, Result.z, Result.w);
        }
    }
    [System.Serializable]
    public class EskyTrackerOffset{
        public Vector3 LocalRigTranslation;
        public Quaternion LocalRigRotation;
        bool CalibrationLoaded = false;
        public void SetCalibrationLoaded(){
            CalibrationLoaded = true;
        }
        public bool GetCalibrationLoaded(){
            return CalibrationLoaded;
        }
        public Vector3 CameraPreviewTranslation;
        public Vector3 CameraPreviewRotation;
        public bool UsesCameraPreview;
        public bool AllowsSaving = true;
    }
    
    public class EskyPoseCallbackData{
        public string PoseID;
        public Quaternion rotation;
        public Vector3 position;
    }
    [System.Serializable]
    public class MapSavedCallback : UnityEngine.Events.UnityEvent<EskyMap>{

    }
    public class EskyTracker : MonoBehaviour
    {
        
        public static string TrackerCalibrationsFolder = "./TrackingCalibrations/";
        public string TrackerCalibrationFileName = "TrackerOffset.json";
        public bool ApplyPoses = true;
        [HideInInspector]
        public EskyTrackerOffset myOffsets;
        public UnityEngine.Events.UnityEvent ReLocalizationCallback;
        [SerializeField] 
        public MapSavedCallback mapCollectedCallback;
        protected EskyMap retEskyMap;
        public GameObject subscribedAnchor;// = new Dictionary<string, GameObject>();
        public static EskyTracker instance;
        public delegate void EventCallback(int Result);
        public delegate void MapDataCallback(IntPtr data, int Length);
        public delegate void PoseReceivedCallback(string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw);
        public delegate void DeltaMatrixConvertCallback(IntPtr writebackArray, IntPtr writeBackInvArray,
                                                        float tx_A, float ty_A, float tz_A, float qx_A, float qy_A, float qz_A, float qw_A,
                                                        float tx_B, float ty_B, float tz_B, float qx_B, float qy_B, float qz_B, float qw_B);
        [HideInInspector]        
        public Vector3 velocity = Vector3.zero;
        [HideInInspector]
        public Vector3 velocityRotation = Vector3.zero;
        public float smoothing = 0.1f;
        public float smoothingRotation= 0.1f;
        [HideInInspector]
        public float[] currentRealsensePose = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        [HideInInspector]        
        public float[] currentRealsenseObject = new float[7]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        public Transform RigCenter;
        public Transform EyeLeft;
        public Transform EyeRight;
        public Transform CameraPreview;
        [HideInInspector]
        public Vector3 currentEuler = Vector3.zero;
        protected EskyPoseCallbackData callbackEvents = null;
        public GameObject MeshParent;
        Matrix4x4 matrixEyeLeft = Matrix4x4.identity;
        Matrix4x4 matrixEyeRight = Matrix4x4.identity;   
        Matrix4x4 matrixEyeLeftInv = Matrix4x4.identity;

        Matrix4x4 matrixEyeRightInv = Matrix4x4.identity;             
        public float[] EyeTransformLeft;
        public float[] EyeTransformRight;
        public float[] EyeTransformLeftInv;
        public float[] EyeTransformRightInv;        
        // Start is called before the first frame update
        public virtual void LoadCalibration(){
            if(File.Exists(TrackerCalibrationsFolder + TrackerCalibrationFileName)){
                myOffsets =  JsonUtility.FromJson<EskyTrackerOffset>(File.ReadAllText(TrackerCalibrationsFolder + TrackerCalibrationFileName));
                myOffsets.SetCalibrationLoaded();
                if(RigCenter != null){
                    RigCenter.transform.localPosition = myOffsets.LocalRigTranslation;
                    RigCenter.transform.localRotation = myOffsets.LocalRigRotation;
                    UnityEngine.Debug.Log("Loaded 6DOF tracker offsets!");
                }
/*                matrixEyeLeft.SetTRS(EyeLeft.transform.localPosition,EyeLeft.transform.localRotation,Vector3.one);
                matrixEyeRight.SetTRS(EyeRight.transform.localPosition,EyeRight.transform.localRotation,Vector3.one);                */
//                matrixEyeLeft = EyeLeft.worldToLocalMatrix;
  //              matrixEyeRight = EyeRight.worldToLocalMatrix;
    //            matrixEyeLeftInv = matrixEyeLeft.inverse;
      //          matrixEyeRightInv = matrixEyeRight.inverse;
                 
                EyeTransformLeft = new float[16]{
                    matrixEyeLeft.m00,matrixEyeLeft.m10,matrixEyeLeft.m20,matrixEyeLeft.m30,
                    matrixEyeLeft.m01,matrixEyeLeft.m11,matrixEyeLeft.m21,matrixEyeLeft.m31,
                    matrixEyeLeft.m02,matrixEyeLeft.m12,matrixEyeLeft.m22,matrixEyeLeft.m32,                                        
                    matrixEyeLeft.m03,matrixEyeLeft.m13,matrixEyeLeft.m23,matrixEyeLeft.m33,                    
                };
                EyeTransformRight = new float[16]{
                    matrixEyeRight.m00,matrixEyeRight.m10,matrixEyeRight.m20,matrixEyeRight.m30,
                    matrixEyeRight.m01,matrixEyeRight.m11,matrixEyeRight.m21,matrixEyeRight.m31,
                    matrixEyeRight.m02,matrixEyeRight.m12,matrixEyeRight.m22,matrixEyeRight.m32,                                        
                    matrixEyeRight.m03,matrixEyeRight.m13,matrixEyeRight.m23,matrixEyeRight.m33,                    
                };  

                EyeTransformLeftInv = new float[16]{
                    matrixEyeLeftInv.m00,matrixEyeLeftInv.m10,matrixEyeLeftInv.m20,matrixEyeLeftInv.m30,
                    matrixEyeLeftInv.m01,matrixEyeLeftInv.m11,matrixEyeLeftInv.m21,matrixEyeLeftInv.m31,
                    matrixEyeLeftInv.m02,matrixEyeLeftInv.m12,matrixEyeLeftInv.m22,matrixEyeLeftInv.m32,                                        
                    matrixEyeLeftInv.m03,matrixEyeLeftInv.m13,matrixEyeLeftInv.m23,matrixEyeLeftInv.m33,                    
                };
                EyeTransformRightInv = new float[16]{
                    matrixEyeRightInv.m00,matrixEyeRightInv.m10,matrixEyeRightInv.m20,matrixEyeRightInv.m30,
                    matrixEyeRightInv.m01,matrixEyeRightInv.m11,matrixEyeRightInv.m21,matrixEyeRightInv.m31,
                    matrixEyeRightInv.m02,matrixEyeRightInv.m12,matrixEyeRightInv.m22,matrixEyeRightInv.m32,                                        
                    matrixEyeRightInv.m03,matrixEyeRightInv.m13,matrixEyeRightInv.m23,matrixEyeRightInv.m33,                    
                };                                          
            }else{
                Debug.LogError("Walp, no tracking calibration was loaded, does the file exist?");
            }
        }
        public virtual void SaveCalibration(){
            string json = JsonUtility.ToJson(myOffsets,true);
            System.IO.File.WriteAllText(TrackerCalibrationsFolder+TrackerCalibrationFileName, json);
            UnityEngine.Debug.Log("Saved 6DOF tracker offsets!");
        }
        public virtual void AfterInitialization(){

        }
        public void SetEskyMapInstance(EskyMap em){
            ShouldCallBackMap = true;
            retEskyMap = em;
        }
        public virtual void LoadEskyMap(EskyMap m){
           
        }
        public virtual void SaveEskyMapInformation(){
        }
        public virtual void ObtainPose(){
           
        }    
        void Awake(){
            instance = this;
        }
        public virtual void ObtainObjectPoses(){

        }
        // Update is called once per frame
        void Update()
        {
            ObtainPose();
            if(myOffsets.AllowsSaving){
                if(Input.GetKeyDown(KeyCode.S)){
                    SaveCalibration();
                }
            }
            if(UpdateLocalizationCallback){
                UpdateLocalizationCallback = false;
                ObtainObjectPoses();
                if(instance.ReLocalizationCallback != null){
                    instance.ReLocalizationCallback.Invoke();
                }                
            }
            if(ShouldCallBackMap){                
                if(EskyAnchor.instance != null){
                    (Dictionary<string,EskyAnchorContentInfo>,byte[]) returnvals = EskyAnchor.instance.GetEskyMapInfo();
                    retEskyMap.contentLocations = returnvals.Item1;
                    retEskyMap.meshDataArray = returnvals.Item2;
                }                
                if(instance.mapCollectedCallback != null){
                    #if ZED_SDK
                    instance.mapCollectedCallback.Invoke(retEskyMap);
                    #else
                    instance.mapCollectedCallback.Invoke(retEskyMap);                    
                    #endif
                }
                ShouldCallBackMap = false;
            }
            if(callbackEvents != null){
                if(EskyAnchor.instance != null){
                    EskyAnchor.instance.transform.position = callbackEvents.position;
                    EskyAnchor.instance.transform.rotation = callbackEvents.rotation;                        
                }
                callbackEvents = null;                
            }
            AfterUpdate();
        }
        public virtual void AfterUpdate(){

        }

        public (Vector3,Quaternion) IntelPoseToUnity(float[] inputPose){
            Quaternion q = new Quaternion(inputPose[3],inputPose[4],inputPose[5],inputPose[6]);
            Vector3 p = new Vector3(inputPose[0],inputPose[1],-inputPose[2]);
            q = Quaternion.Euler(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z);
            return (p,q);
        }
        public (Vector3,Quaternion) IntelPoseToUnity(float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            Quaternion q = new Quaternion(qx,qy,qz,qw);
            Vector3 p = new Vector3(tx,ty,-tz);
            q = Quaternion.Euler(-q.eulerAngles.x,-q.eulerAngles.y,q.eulerAngles.z);
            return (p,q);
        }
        public (Vector3,Quaternion) UnityPoseToIntel(Vector3 position, Quaternion orientation){
            Quaternion qq = Quaternion.Euler(-orientation.eulerAngles.x,-orientation.eulerAngles.y,orientation.eulerAngles.z);
            Vector3 pp = new Vector3(position.x,position.y,-position.z);
            return (pp,qq);
        }
        bool UpdateLocalizationCallback = false;
        [MonoPInvokeCallback(typeof(EventCallback))]
        public static void OnEventCallback(int Response)
        {
            switch(Response){
                case 1:
                UnityEngine.Debug.Log("We received the localization event!!");
                if(instance != null){
                    instance.UpdateLocalizationCallback = true;
                }
                break;
                default:
                UnityEngine.Debug.Log("We received a callback I'm unfamiliar with, sorry!: " + Response);
                break;
            }
            //Ptr to string
        }
      
        public delegate void debugCallback(IntPtr request, int color, int size);        
        byte[] callbackMemoryMapInfo;
        bool ShouldCallBackMap= false;
        [HideInInspector]
        public bool hasInitializedTexture = false;
        [HideInInspector]
        public int textureWidth;
        [HideInInspector]                
        public int textureHeight;
        [HideInInspector]        
        public int textureChannels;
        [HideInInspector]
        public float fx,fy,cx,cy,fovx,fovy,focalLength;

        
        #region TrackerSpecific

        
        #endregion
    }
}