using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ProjectEsky.Renderer{
    [System.Serializable]
    public class EyeBorders{
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
        [HideInInspector]
        public float[] myBorders = new float[4];
        public void UpdateBorders(){
            myBorders[0] = minX;
            myBorders[1] = maxX;
            myBorders[2] = minY;
            myBorders[3] = maxY;
        }
    }
public class EskyNativeRenderer : MonoBehaviour
{

        public ProjectEsky.Tracking.EskyTracker myTracker;
        public Transform Tracker;        
        public Transform RigCenter;
        public Transform LeapMotionCamera;

        public Camera LeftCamera;
        public Camera RightCamera;
        public int WindowPositionX;
        public int WindowPositionY;
        public int WindowWidth;
        public int WindowHeight;
        static RenderTexture myTexLeft;
        static RenderTexture myTexRight;    
        public Matrix4x4 TransformFromTrackerToCenter;
        public NorthstarV2Calibration calibration;
        public bool requiresRotation = false;
        public bool allowsSaving = true;
        public bool load6DOFCalibration = false;
        public bool loadLeapCalibration = false;
        void LoadCalibration(){
            calibration = new NorthstarV2Calibration();
            calibration.baseline = 0.0f;
            calibration = JsonUtility.FromJson<NorthstarV2Calibration>(File.ReadAllText("NorthStarCalibration.json")); 
            if(calibration.baseline == 0.0f){
                Debug.LogError("There was a problem loading the calibration");
                this.enabled = false;
                return;
            }          
            if(loadLeapCalibration){              
                LeapMotionCamera.localPosition = new Vector3(calibration.localPositionLeapMotion[0],calibration.localPositionLeapMotion[1],calibration.localPositionLeapMotion[2]);
                LeapMotionCamera.localRotation = new Quaternion(calibration.localRotationLeapMotion[0],calibration.localRotationLeapMotion[1],calibration.localRotationLeapMotion[2],calibration.localRotationLeapMotion[3]);            
            }            
            if(load6DOFCalibration){                
                Tracker.transform.localPosition  = new Vector3(calibration.localPositionRigFromTracker[0],calibration.localPositionRigFromTracker[1],calibration.localPositionRigFromTracker[2]);
                Tracker.transform.localRotation = new Quaternion(calibration.localRotationRigFromTracker[0],calibration.localRotationRigFromTracker[1],calibration.localRotationRigFromTracker[2],calibration.localRotationRigFromTracker[3]);            
            }
            if(myTracker != null){
                myTracker.RigCenter = RigCenter;                        
                if(calibration.transformBetweenTrackerAndCenter != null){
                    TransformFromTrackerToCenter = new Matrix4x4();         
                    SetMatrixToArray(calibration.transformBetweenTrackerAndCenter);                
                    myTracker.TransformFromTrackerToCenter = TransformFromTrackerToCenter;
                }
            }
        }
        void Start()
        {
            Application.targetFrameRate = 60;
            StartCoroutine(PostUpdateSendBuffers());
            LoadCalibration();
            LeftCamera.transform.localPosition=new Vector3(-(calibration.baseline/2.0f),0,0);
            RightCamera.transform.localPosition=new Vector3((calibration.baseline/2.0f),0,0);
            if(requiresRotation){
                LeftCamera.transform.Rotate(new Vector3(0,0,90),Space.Self);
                RightCamera.transform.Rotate(new Vector3(0,0,90),Space.Self);        
            }
            GL.IssuePluginEvent(GetUnityContext(),1);            
            updateOffsets = true;
            RegisterDebugCallback(OnDebugCallback);    
//            GL.IssuePluginEvent(GetUnityContext(),1);    
//            StartCoroutine(StartSpatialMappingDelayed(2));
        }
        public IEnumerator StartSpatialMappingDelayed(float seconds){
            yield return new WaitForSeconds(seconds);
            //myTracker.StartSpatialMapping();
        }
        public Vector2 LeftScreenSpaceOffset;
        public Vector2 RightScreenSpaceOffset;

        public bool updateOffsets = false;
        bool launchRenderer = false;
        bool initialized = false;
        float[] LeftArray;
        float[] RightArray;
        float[] leftOffset = new float[2]{0,0};
        float[] rightOffset = new float[2]{0,0};
        float[] leftOffsetCheck = new float[2]{0,0};
        float[] rightOffsetCheck = new float[2]{0,0};
        public bool UpdateBorders;
        public EyeBorders leftBoarders;
        public EyeBorders rightBoarders;        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {

            Debug.Log("Before scene loaded");
        }
        public void Initialization(){
                if(myTexLeft == null){    
                    myTexLeft = new RenderTexture(WindowHeight,WindowWidth,16,RenderTextureFormat.ARGBFloat);
                    myTexLeft.Create();        

                }
                if(myTexRight == null){
                    myTexRight = new RenderTexture(WindowHeight,WindowWidth,16,RenderTextureFormat.ARGBFloat);                    
                    myTexRight.Create();                            
                }                

                      
                LeftCamera.targetTexture = myTexLeft;
                RightCamera.targetTexture = myTexRight;
                int left = (int)myTexLeft.GetNativeTexturePtr();
                int right = (int)myTexRight.GetNativeTexturePtr();
                Debug.LogError(left);
                Debug.LogError(right);              
                LeftArray = MatrixToFloat(LeftCamera.projectionMatrix);
                RightArray = MatrixToFloat(RightCamera.projectionMatrix);                
                setLeftRightPointers(left,right);
                setCalibration(calibration.left_uv_to_rect_x,calibration.left_uv_to_rect_y,calibration.right_uv_to_rect_x,calibration.right_uv_to_rect_y);
                setLeftRightCameraMatricies(LeftArray,RightArray);                    
                initialized = true;
                PerformUpdateOfOffsets();
        }
        float TimeLeft = 0f;
        public void ReloadLevel(){
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }    

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.R)){
                ReloadLevel();       
            }

            if(launchRenderer){
                launchRenderer = false;  
                Initialization();      
            }
            if(UpdateBorders){
                UpdateBorders = false;
                leftBoarders.UpdateBorders();
                rightBoarders.UpdateBorders();
                SetEyeBorders(leftBoarders.myBorders,rightBoarders.myBorders);
            }
            if(requiresRotation){
                rightOffsetCheck[0] = LeftScreenSpaceOffset.y;
                rightOffsetCheck[1] = LeftScreenSpaceOffset.x;
                leftOffsetCheck[0] = RightScreenSpaceOffset.y;
                leftOffsetCheck[1] = RightScreenSpaceOffset.x;                    
            }else{
                rightOffsetCheck[0] = LeftScreenSpaceOffset.x;
                rightOffsetCheck[1] = LeftScreenSpaceOffset.y;
                leftOffsetCheck[0] = RightScreenSpaceOffset.x;
                leftOffsetCheck[1] = RightScreenSpaceOffset.y;                    
            }
            if(updateOffsets){
                bool needsUpdating = false;
                if(rightOffset[0] != rightOffsetCheck[0]){needsUpdating = true;rightOffset[0] = rightOffsetCheck[0];}
                if(rightOffset[1] != rightOffsetCheck[1]){needsUpdating = true;rightOffset[1] = rightOffsetCheck[1];}
                if(leftOffset[0] != leftOffsetCheck[0]){needsUpdating = true;leftOffset[0] = leftOffsetCheck[0];}
                if(leftOffset[1] != leftOffsetCheck[1]){needsUpdating = true;leftOffset[1] = leftOffsetCheck[1];}                
                if(needsUpdating){
                    PerformUpdateOfOffsets();
                }
            }
            if(allowsSaving){
                if(Input.GetKeyDown(KeyCode.S)){
                    SaveCalibration();
                }

            }
        }

        void PerformUpdateOfOffsets(){
            if(requiresRotation){
                rightOffset[0] = LeftScreenSpaceOffset.y;
                rightOffset[1] = LeftScreenSpaceOffset.x;
                leftOffset[0] = RightScreenSpaceOffset.y;
                leftOffset[1] = RightScreenSpaceOffset.x;                    
            }else{
                rightOffset[0] = LeftScreenSpaceOffset.x;
                rightOffset[1] = LeftScreenSpaceOffset.y;
                leftOffset[0] = RightScreenSpaceOffset.x;
                leftOffset[1] = RightScreenSpaceOffset.y;                    
            }
            setScreenSpaceOffset(rightOffset,leftOffset);
        }
        float[] MatrixToFloat(Matrix4x4 input){
            float[] mm = new float[16];
            mm[0] = input.m00;
            mm[1] = input.m01;
            mm[2] = input.m02;               
            mm[3] = input.m03;

            mm[4] = input.m10;
            mm[5] = input.m11;
            mm[6] = input.m12;               
            mm[7] = input.m13;

            mm[8] = input.m20;
            mm[9] = input.m21;
            mm[10] = input.m22;               
            mm[11] = input.m23;

            mm[12] = input.m30;
            mm[13] = input.m31;
            mm[14] = input.m32;               
            mm[15] = input.m33;                                
            return mm;
        }
        public void GetMatrixAsArray(Matrix4x4 input, ref float[] arrayToSet){
            for(int i = 0; i < arrayToSet.Length; i++){
                arrayToSet[i] = input[i];
            }
        }
        public void SetMatrixToArray(float[] arrayToSet){
            TransformFromTrackerToCenter = new Matrix4x4();
            for(int i = 0; i < arrayToSet.Length; i++){
                TransformFromTrackerToCenter[i] = arrayToSet[i];
            }            
        }
        public void SaveCalibration(){

            calibration.left_eye_offset = leftOffset;
            calibration.right_eye_offset = rightOffset;            
            Vector3 localPosLeapMotion = LeapMotionCamera.localPosition;
            Quaternion localRotLeapMotion = LeapMotionCamera.localRotation;
            calibration.localPositionLeapMotion = new float[3]{localPosLeapMotion.x,localPosLeapMotion.y,localPosLeapMotion.z};
            calibration.localRotationLeapMotion = new float[4]{localRotLeapMotion.x,localRotLeapMotion.y,localRotLeapMotion.z,localRotLeapMotion.w};


            Vector3 localPosTrackingRig = Tracker.localPosition;
            Quaternion localRotTrackingRig = Tracker.localRotation;
            calibration.localPositionRigFromTracker = new float[3]{localPosTrackingRig.x,localPosTrackingRig.y,localPosTrackingRig.z};            
            calibration.localRotationRigFromTracker = new float[4]{localRotTrackingRig.x,localRotTrackingRig.y,localRotTrackingRig.z,localRotTrackingRig.w};

            Matrix4x4 WtoTracker = Tracker.worldToLocalMatrix;
            Matrix4x4 WtoRC =  RigCenter.worldToLocalMatrix;

            Matrix4x4 AtoL = WtoTracker.inverse * WtoRC;
            Debug.Log(AtoL);
            float[] transformAsArray = new float[16];
            GetMatrixAsArray(AtoL,ref transformAsArray);
            calibration.transformBetweenTrackerAndCenter = transformAsArray;
            string json = JsonUtility.ToJson(calibration);
            System.IO.File.WriteAllText("NorthStarCalibration.json", json);
            Debug.Log("Saved Calibration");
        }
        void OnDestroy(){
            if(initialized)
            stop();
        }
        [DllImport("libProjectEskyLLAPI", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterDebugCallback(debugCallback cb);

        [DllImport("libProjectEskyLLAPI")]
        static extern void stop();
        //Create string param callback delegate
        delegate void debugCallback(IntPtr request, int color, int size);
        enum Color { red, green, blue, black, white, yellow, orange };
        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            //Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);

            //Add Specified Color
            debug_string =
                String.Format("{0}{1}{2}{3}{4}",
                "<color=",
                ((Color)color).ToString(),
                ">",
                debug_string,
                "</color>"
                );

            UnityEngine.Debug.Log(debug_string);
        }
        public IEnumerator PostUpdateSendBuffers(){
            yield return new WaitForEndOfFrame(); // wait til the end of the first frame to start the process for launching
            yield return new WaitForSeconds(2);
            initialize(WindowPositionX,WindowPositionY,WindowWidth,WindowHeight);              
            yield return new WaitForSeconds(1);            
            launchRenderer = true;
        }


        #region nativeMethods
        [DllImport("libProjectEskyLLAPI")]
        // Update is called once per frame
        private static extern IntPtr GetUnityContext();
        [DllImport("libProjectEskyLLAPI")]
        private static extern void SetEyeBorders(float[] leftBorders, float[] rightBoarders);

        [DllImport("libProjectEskyLLAPI")]
        private static extern void setScreenSpaceOffset(float[] leftOffset, float[] rightOffset);
        [DllImport("libProjectEskyLLAPI")]
        private static extern void setLeftRightPointers(int Left, int Right);
        [DllImport("libProjectEskyLLAPI")]
        private static extern void setLeftRightCameraMatricies(float[] leftCameraMatrix,float[] rightCameraMatrix);

        [DllImport("libProjectEskyLLAPI")]
        private static extern void initialize(int xPos, int yPos, int w, int h);
        [DllImport("libProjectEskyLLAPI")]
        private static extern void setCalibration(float[] leftuvtorectx, float[] leftuvtorecty, float[] rightuvtorectx, float[] rightuvtorecty);

        #endregion
    }
    [System.Serializable]
    public class NorthstarV2Calibration{
        [SerializeField]
        public float focusDistance;
        [SerializeField]
        public float baseline;
        [SerializeField]        
        public float[] left_uv_to_rect_x ;
        [SerializeField]
        public float[] left_uv_to_rect_y ;
        [SerializeField]
        public float[] right_uv_to_rect_x;
        [SerializeField]
        public float[] right_uv_to_rect_y;
        [SerializeField]
        public float[] left_eye_offset;
        [SerializeField]
        public float[] right_eye_offset;
        
        [SerializeField]
        public float[] localPositionLeapMotion;
        [SerializeField]
        public float[] localRotationLeapMotion;
        [SerializeField]
        public float[] localPositionRigFromTracker;
        [SerializeField]
        public float[] localRotationRigFromTracker;
        [SerializeField]
        public float[] transformBetweenTrackerAndCenter;
    }
}