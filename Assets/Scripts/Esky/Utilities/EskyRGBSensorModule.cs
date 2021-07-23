using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine;

namespace BEERLabs.ProjectEsky.Extras.Modules{
    public enum Color { red, green, blue, black, white, yellow, orange };
    
    public class EskyRGBSensorModule : SensorImageSource
    {
        public static EskyRGBSensorModule instance;
        public static string RGBSensorInfoLocation = "./RGBSensorConfigurations/";
        public string TrackerFileName = "RGBSensorCalibration.json";

        public delegate void TextureInitializedCallback(int textureWidth, int textureHeight,int textureCount);
        public delegate void FuncCallBack(IntPtr message, int color, int size);
        public bool loadCalibration = false;
        public bool allowSavingCalibration = true;
        static bool hasInitializedTexture = false;
        public UnityEngine.UI.RawImage rawImageMap;
        public Camera myPreviewCamera;
        RenderTexture myRenderTex;
        bool canRenderImages = false;
        public int textureChannels;
        static int SensorWidth;
        static int SensorHeight;
        static int SensorChannels;
        // Start is called before the first frame update
        Sprite s;
        void Awake()
        {         
            instance = this;
            RGBImageSource = this;
        }
        void Start(){
            ChangeCameraParam();

        }
        [MonoPInvokeCallback(typeof(FuncCallBack))]
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
            UnityEngine.Debug.Log("RGB Module: " + debug_string);            
        }
        public void ChangeCameraParam(){
            float f = 35.0f;            
            float ax, ay, sizeX, sizeY;
            float x0, y0, shiftX, shiftY;
            int width, height;

            ax = myCalibrations.fx; 
            ay = myCalibrations.fy;
            x0 = myCalibrations.cx;
            y0 = myCalibrations.cy;
            width = myCalibrations.SensorWidth;
            height = myCalibrations.SensorHeight;

            sizeX = f * width / ax;
            sizeY = f * height / ay;

            //PlayerSettings.defaultScreenWidth = width;
            //PlayerSettings.defaultScreenHeight = height;

            shiftX = -(x0 - width / 2.0f) / width;
            shiftY = (y0 - height / 2.0f) / height;

            myPreviewCamera.sensorSize = new Vector2(sizeX, sizeY);     // in mm, mx = 1000/x, my = 1000/y
            myPreviewCamera.focalLength = f;                            // in mm, ax = f * mx, ay = f * my
            myPreviewCamera.lensShift = new Vector2(shiftX, shiftY);    // W/2,H/w for (0,0), 1.0 shift in full W/H in image plane
        }
        // Update is called once per frame
        float timeTilStartup = 7f;
        void Update()
        {
            if(timeTilStartup > 0){
                timeTilStartup -= Time.deltaTime;
                if(timeTilStartup < 0){
                    RegisterDebugCallback(OnDebugCallback);
                    InitializeCameraObject(myCalibrations.camID);
                    SubscribeToInitializedCallback(myCalibrations.camID,OnTextureInitialized);
                    SubscribeCallback(myCalibrations.camID,ReceiveImageCallback);
                    StartCamera(myCalibrations.camID,myCalibrations.fx, myCalibrations.fy,myCalibrations.cx,myCalibrations.cy,myCalibrations.d1,myCalibrations.d2,myCalibrations.d3,myCalibrations.d4);
                }
            }
            if(hasInitializedTexture){
                Debug.Log("Received Feedback texture: " + SensorChannels);
                myCalibrations.SensorChannels = SensorChannels;
                myCalibrations.SensorWidth = SensorWidth;
                myCalibrations.SensorHeight = SensorHeight;
//                HookToSensor(myCalibrations.camID);

                hasInitializedTexture = false;
                if(myCalibrations.SensorChannels == 4){
                    myRenderTex = new RenderTexture(1280,720,0,RenderTextureFormat.BGRA32);
                    myRenderTex.Create();
                    SetTexturePointer(myCalibrations.camID,myRenderTex.GetNativeTexturePtr());
                    if(rawImageMap != null){
                        rawImageMap.texture = myRenderTex;
                        rawImageMap.gameObject.SetActive(true);
                    } 
                    canRenderImages = true;
                    StartCoroutine(CallbackOnGPUThread());
                }else{
                    myRenderTex = new RenderTexture(1280,720,0,RenderTextureFormat.BGRA32);
                    myRenderTex.Create();
                    SetTexturePointer(myCalibrations.camID,myRenderTex.GetNativeTexturePtr());
                    if(rawImageMap != null){
                        rawImageMap.texture = myRenderTex;
                        rawImageMap.gameObject.SetActive(true);
                    }
                    canRenderImages = true;
                    StartCoroutine(CallbackOnGPUThread());                    
                }
            }            
        }
        [MonoPInvokeCallback(typeof(TextureInitializedCallback))]
        public void OnTextureInitialized(int textureWidth, int textureHeight, int textureChannels){
            Debug.Log("Received the texture initializaed callback");
            SensorWidth = textureWidth;
            SensorHeight = textureHeight;
            SensorChannels = textureChannels;
            hasInitializedTexture = true;
        }
        bool activateMe = true;
        public IEnumerator CallbackOnGPUThread(){
            while(activateMe){
                yield return new WaitForEndOfFrame();
                if(canRenderImages){
                    GL.IssuePluginEvent(GetRenderEventFunc(myCalibrations.camID), 1);
                }
            }
        }
        public void OnDestroy(){
            Debug.Log("Stopping");
            activateMe = false;
            StopCamera(myCalibrations.camID);
        }
        [MonoPInvokeCallback(typeof(ReceiveSensorImageCallbackWithInstanceID))]
        public static void ReceiveImageCallback(int TrackerID, IntPtr info, int lengthofarray, int width, int height, int pixelCount){
            instance.SendImageData(info,lengthofarray,width,height,pixelCount);
        }
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void StartCamera(int camID, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void StopCamera(int camID);
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void StopCameras();        
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void SubscribeCallbackWithID(int InstanceID, int camID,ReceiveSensorImageCallbackWithInstanceID callback);        

        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void SubscribeToInitializedCallback(int camID, TextureInitializedCallback callback);
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void HookToSensor(int camID);
        [DllImport("libProjectEskyRGBSensorModule")]
        static extern void SetTexturePointer(int camID, IntPtr textureHandle);
        [DllImport("libProjectEskyRGBSensorModule")]
        public static extern IntPtr GetRenderEventFunc(int camID);        
        [DllImport("libProjectEskyRGBSensorModule")]
        public static extern void InitializeCameraObject(int camID);
        [DllImport("libProjectEskyRGBSensorModule")]
        public static extern void RegisterDebugCallback(FuncCallBack callback);
        public override void SubscribeCallback(int instanceID, ReceiveSensorImageCallbackWithInstanceID callbackWithInstanceID){
            SubscribeCallbackWithID(instanceID,myCalibrations.camID,callbackWithInstanceID);
        }
    }
}