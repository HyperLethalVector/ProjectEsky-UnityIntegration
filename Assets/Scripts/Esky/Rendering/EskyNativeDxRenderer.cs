#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_WSA_10_0 || WINDOWS_UWP
#define UNITY_WINDOWS  
#endif

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using AOT;
using System.IO;

namespace ProjectEsky.Rendering{
     [System.Serializable]
     public class DisplayCalibration{
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
    }
    [System.Serializable]
    public class EyeBorders{
        public float minXLeft;
        public float maxXLeft;
        public float minYLeft;
        public float maxYLeft;

        public float minXRight;
        public float maxXRight;
        public float minYRight;
        public float maxYRight;        
        [HideInInspector]
        public float[] myBorders = new float[8];
        public void UpdateBorders(){
            myBorders[0] = minXLeft;
            myBorders[1] = maxXLeft;
            myBorders[2] = minYLeft;
            myBorders[3] = maxYLeft;

            myBorders[4] = minXRight;
            myBorders[5] = maxXRight;
            myBorders[6] = minYRight;
            myBorders[7] = maxYRight;
        }
    }
    [System.Serializable]
    public class DisplaySettings{
        public int DisplayXLoc;
        public int DisplayYLoc;
        public int DisplayWidth;
        public int DisplayHeight;
        public int EyeTextureWidth;
        public int EyeTextureHeight;        
        public int RendererWindowID;
        [HideInInspector]
        public bool Initialized = false;
    }
    [System.Serializable] 
    public class RenderTextureSettings{
        [HideInInspector]
        public RenderTexture LeftRenderTexture;
        [HideInInspector]    
        public RenderTexture RightRenderTexture;
        public Camera LeftCamera;
        public Camera RightCamera;
        public bool RequiresRotation = true;
        public float[] LeftProjectionMatrix;
        public float[] RightProjectionMatrix;
        public float[] LeftInvProjectionMatrix;
        public float[] RightInvProjectionMatrix;
        public void UpdateProjectionMatrix(Matrix4x4 input, bool isLeft){
            Matrix4x4 inv = input.inverse;
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

            float[] mminv = new float[16];
            mminv[0] = inv.m00;
            mminv[1] = inv.m01;
            mminv[2] = inv.m02;               
            mminv[3] = inv.m03;

            mminv[4] = inv.m10;
            mminv[5] = inv.m11;
            mminv[6] = inv.m12;               
            mminv[7] = inv.m13;

            mminv[8] = inv.m20;
            mminv[9] = inv.m21;
            mminv[10] = inv.m22;               
            mminv[11] = inv.m23;

            mminv[12] = inv.m30;
            mminv[13] = inv.m31;
            mminv[14] = inv.m32;               
            mminv[15] = inv.m33;                                

            if(isLeft)LeftProjectionMatrix =mm; else RightProjectionMatrix=mm;
            if(isLeft)LeftInvProjectionMatrix =mminv; else RightInvProjectionMatrix=mminv;            
        }
    }
    public class EskyNativeDxRenderer : MonoBehaviour
    {
        public static string OpticalCalibrationsFolder = "./OpticalCalibrations/V2/";
        public bool allowsSavingCalibration = true;
        public bool LoadDisplaySettings = true;
        public DisplaySettings displaySettings;
        public RenderTextureSettings renderTextureSettings;
        public EyeBorders myEyeBorders;
        public static Matrix4x4 leftEyeTransform = Matrix4x4.identity;
        public static Matrix4x4 rightEyeTransform = Matrix4x4.identity;
        IEnumerator backgroundRendererCoroutine;
        public bool loadLeapCalibration = false;
        public bool runInBackgroundInitial;
        RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
        readonly List<int> windowsOn = new List<int>();
        public DisplayCalibration calibration;
        public GameObject LeapMotionCamera;
        public ProjectEsky.Tracking.EskyTrackerIntel myAttachedTracker;
        public GameObject RigCenter;
        void Awake() {
            Application.targetFrameRate = 60;
            SetupDebugDelegate();
            runInBackgroundInitial = Application.runInBackground;
            LoadCalibration();
        }
        void LoadCalibration(){
            calibration = new DisplayCalibration();
            calibration.baseline = 0.0f;

            if(LoadDisplaySettings){
                if(File.Exists("DisplaySettings.json")){
                    displaySettings = JsonUtility.FromJson<DisplaySettings>(File.ReadAllText("DisplaySettings.json"));
                }
            }            
            if(File.Exists(OpticalCalibrationsFolder+"DisplayCalibration.json")){
                calibration = JsonUtility.FromJson<DisplayCalibration>(File.ReadAllText(OpticalCalibrationsFolder+"DisplayCalibration.json")); 
                if(calibration.baseline == 0.0f){
                    Debug.LogError("There was a problem loading the calibration");
                    this.enabled = false;
                    return;
                }          
                if(loadLeapCalibration){
                    if(LeapMotionCamera == null)          
                    LeapMotionCamera = GameObject.Find("LeapMotion");    
                    if(LeapMotionCamera != null){
                        LeapMotionCamera.transform.localPosition = new Vector3(calibration.localPositionLeapMotion[0],calibration.localPositionLeapMotion[1],calibration.localPositionLeapMotion[2]);
                        LeapMotionCamera.transform.localRotation = new Quaternion(calibration.localRotationLeapMotion[0],calibration.localRotationLeapMotion[1],calibration.localRotationLeapMotion[2],calibration.localRotationLeapMotion[3]);                                
                    }else{
                        Debug.LogError("Couldn't find the leapmotion object, did you modify the rig???");
                    }
                }            

                renderTextureSettings.LeftCamera.transform.localPosition=new Vector3(-(calibration.baseline/2.0f),0,0);
                renderTextureSettings.RightCamera.transform.localPosition=new Vector3((calibration.baseline/2.0f),0,0);

                if(renderTextureSettings.RequiresRotation){
                    renderTextureSettings.LeftCamera.transform.Rotate(new Vector3(0,0,90),Space.Self);
                    renderTextureSettings.RightCamera.transform.Rotate(new Vector3(0,0,90),Space.Self);        
                }
                renderTextureSettings.LeftRenderTexture = new RenderTexture(displaySettings.EyeTextureWidth, displaySettings.EyeTextureHeight, 24, renderTextureFormat);
                renderTextureSettings.RightRenderTexture = new RenderTexture(displaySettings.EyeTextureWidth, displaySettings.EyeTextureHeight, 24, renderTextureFormat);
                renderTextureSettings.LeftCamera.targetTexture = renderTextureSettings.LeftRenderTexture;
                renderTextureSettings.RightCamera.targetTexture = renderTextureSettings.RightRenderTexture;
                myEyeBorders.UpdateBorders();
                renderTextureSettings.UpdateProjectionMatrix(renderTextureSettings.LeftCamera.projectionMatrix,true);// = renderTextureSettings..renderTextureSettings.LeftCamera.projectionMatrix,renderTextureSettings.RightCamera.projectionMatrix;
                renderTextureSettings.UpdateProjectionMatrix(renderTextureSettings.RightCamera.projectionMatrix,false);//
                if(renderTextureSettings.RequiresRotation){
                    renderTextureSettings.LeftCamera.fieldOfView = 43.01793f;//52.75 for 1.5 weighting
                    renderTextureSettings.RightCamera.fieldOfView = 43.01793f;//Pre-CALCULATED                
                }
            }else{
                Debug.LogError("Waah! My display calibration file is missing :(");
            }

        }
        public void SaveCalibration(){
            if(LeapMotionCamera != null){
                Vector3 localPosLeapMotion = LeapMotionCamera.transform.localPosition;
                Quaternion localRotLeapMotion = LeapMotionCamera.transform.localRotation;
                calibration.localPositionLeapMotion = new float[3]{localPosLeapMotion.x,localPosLeapMotion.y,localPosLeapMotion.z};
                calibration.localRotationLeapMotion = new float[4]{localRotLeapMotion.x,localRotLeapMotion.y,localRotLeapMotion.z,localRotLeapMotion.w};
            }
            DisplaySettings ds = displaySettings;
            ds.Initialized = false;
            string json = JsonUtility.ToJson(calibration,true);
            System.IO.File.WriteAllText(OpticalCalibrationsFolder+"DisplayCalibration.json", json);
            Debug.Log("Saved Calibration");
            string json2 = JsonUtility.ToJson(ds,true);
            System.IO.File.WriteAllText("DisplaySettings.json",json2);
        }
        public bool StartRendererAfterInitializing;
        bool wasDone = false;
        // Update is called once per frame
        void Update() { if(StartRendererAfterInitializing){StartRendererAfterInitializing = false; ShowExternalWindow(0);} if(Input.GetKeyDown(KeyCode.S) && allowsSavingCalibration){SaveCalibration();}}
        IEnumerator CallPluginAtEndOfFrame(int id) {
            if (backgroundRendererCoroutine != null) {
                Application.runInBackground = true;                                
                StopCoroutine(backgroundRendererCoroutine);
                backgroundRendererCoroutine = null;
            }
            yield return new WaitForEndOfFrame();
            IntPtr ptrLeft = renderTextureSettings.RightRenderTexture.GetNativeTexturePtr();
            SendTextureIdToPluginByIdLeft(id, ptrLeft);
            IntPtr ptrRight = renderTextureSettings.LeftRenderTexture.GetNativeTexturePtr();
            SendTextureIdToPluginByIdRight(id, ptrRight);        
            GL.IssuePluginEvent(InitGraphics(), 0);
            yield return new WaitForEndOfFrame();
            SetRequiredValuesById(id,calibration.left_uv_to_rect_x,calibration.left_uv_to_rect_y,calibration.right_uv_to_rect_x,calibration.right_uv_to_rect_y,
            renderTextureSettings.LeftProjectionMatrix,renderTextureSettings.RightProjectionMatrix,
            renderTextureSettings.LeftInvProjectionMatrix,renderTextureSettings.RightInvProjectionMatrix,
            calibration.left_eye_offset,calibration.right_eye_offset,myEyeBorders.myBorders);                  
            yield return new WaitForEndOfFrame();

            if(!wasDone){
                wasDone = true;
                if (backgroundRendererCoroutine == null) {
                    Application.runInBackground = true;
                    backgroundRendererCoroutine = CallRenderEvent();
                    Debug.Log("Background Render Coroutine starting render thread");                                    
                    StartCoroutine(backgroundRendererCoroutine);
                }
            }
            yield return new WaitForEndOfFrame();
        }

        IEnumerator CallRenderEvent(){
            while (true){
                yield return new WaitForEndOfFrame();
                GL.IssuePluginEvent(GetRenderEventFunc(), 0);
                if(myAttachedTracker != null){
                    myAttachedTracker.RenderResetFlag();
                }
            }		
        }

        void ShowExternalWindow (int id)
        {
            const string windows = "Windows";
            string ErrorMessage = "";
            const string graphicsNotSuported = "Please select the correct option at Build Settings->Player Settings->Other Settings->Auto Graphics API";
            const string restartUnity = "And restart Unity!";
            if (SystemInfo.operatingSystem.StartsWith (windows)) {
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11) {
                    ErrorMessage = "Only Direct3D11 is supported."+graphicsNotSuported+" for Windows."+restartUnity;
                }
            } else {
                ErrorMessage = "Only Windows and MacOSX are suported!";
            }

            if(ErrorMessage.Length == 0){
                //when the external window is created, it is preferable for the texture size to match the window's size
                //the size shouldn't be greater than the screen's size
                if(!displaySettings.Initialized){
                    displaySettings.Initialized = true;
                    if (renderTextureFormat == RenderTextureFormat.ARGB32) {
                        SetColorFormat(0);
                    } else if (renderTextureFormat == RenderTextureFormat.ARGBHalf /*|| renderTextureFormat == GraphicsFormat.R16G16B16A16_SFloat*/) {
                        SetColorFormat(1);
                    }
                    StartWindowById(id, "EskyDirectXWindow " + id, displaySettings.DisplayWidth, displaySettings.DisplayHeight,true);
                    SetRenderTextureWidthHeight(id,displaySettings.EyeTextureWidth,displaySettings.EyeTextureHeight);                
                    SetWindowRectById(id,displaySettings.DisplayXLoc,displaySettings.DisplayYLoc,displaySettings.DisplayWidth,displaySettings.DisplayHeight);
                    windowsOn.Add(id);            

                    StartCoroutine (CallPluginAtEndOfFrame(id));                
                }
            } else {
                Debug.LogError(ErrorMessage);			
            }
        }
        void CloseExternalWindow(int id){
            StartCoroutine(StopWindowCoroutine(id));
        }
        public void SetDeltas(float[] deltaLeft,float[] deltaInvLeft, float[] deltaRight, float[] deltaInvRight){
            SetDeltas(0,deltaLeft,deltaInvLeft,deltaRight, deltaInvRight);
        }
        IEnumerator StopWindowCoroutine(int id) {
            yield return new WaitForEndOfFrame();
            if (windowsOn.Count == 0) {
                Debug.Log("Stopping window thread");           
                StopGraphicsCoroutine();
            }
            StopWindowById(id);
        }

        void OnApplicationQuit (){
            if(windowsOn.Count > 0){
                StopGraphicsCoroutine();
                foreach (int winId in windowsOn) {
                    StopWindowById(winId);
                }
                windowsOn.Clear();
            }
        }

        void StopGraphicsCoroutine() {
            Application.runInBackground = runInBackgroundInitial;
            StopCoroutine(backgroundRendererCoroutine);
            backgroundRendererCoroutine = null;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugDelegate(string message);


        [MonoPInvokeCallback(typeof(MyDele))]
        static void CallbackFunction(string message){
            Debug.Log("--PluginCallback--: "+message);
        }
        
        void SetupDebugDelegate(){
            DebugDelegate callbackDelegate = new DebugDelegate(CallbackFunction);
            IntPtr intPtrDelegate = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
            SetDebugFunction(intPtrDelegate);
        }
        delegate void MyDele(string s);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetRenderTextureWidthHeight(int id, int width, int height);
        [DllImport ("ProjectEskyLLAPIRenderer")]
        static extern void SetDebugFunction (IntPtr fp);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void StartWindowById(int windowId, [MarshalAs(UnmanagedType.LPWStr)] string title, int width, int height, bool borderless = false);//if borderless is true,there's no border and title bar

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern IntPtr StopWindowById(int windowId);

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetWindowRectById(int windowId, int left, int top, int width, int height);

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SendTextureIdToPluginByIdLeft(int windowId, IntPtr texId);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SendTextureIdToPluginByIdRight(int windowId, IntPtr texId);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetWindowAttributesById(int windowId, int color, byte alpha, int flags);
        
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetColorFormat(int colorFormat);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetDeltas(int windowID, float[] deltaLeft,float[] deltaInvLeft, float[] deltaRight, float[] deltaInvRight);

        //DISABLED
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetQualitySettings(int count, int quality);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void StartWindow([MarshalAs(UnmanagedType.LPWStr)] string title, int width, int height, bool borderless = false);//if borderless is true,there's no border and title bar

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern IntPtr StopWindow();

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetWindowRect(int left, int top, int width, int height);

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SendTextureIdToPlugin(IntPtr texId);

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern IntPtr InitGraphics();

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern IntPtr GetRenderEventFunc();

        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void StartRenderThreadById(int windowId);
        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void StopRenderThreadById(int windowId);


        [DllImport("ProjectEskyLLAPIRenderer")]
        static extern void SetRequiredValuesById(int windowID,float[] leftUvToRectX,float[] leftUvToRectY,float[] rightUvToRectX,float[] rightUvToRectY,float[] CameraMatrixLeft,float[] CameraMatrixRight,float[] InvCameraMatrixLeft,float[] InvCameraMatrixRight,float[] leftOffset,float[] rightOffset,float[] eyeBorders);
    }
}