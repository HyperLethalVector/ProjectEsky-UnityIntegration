using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    public class Chunk
    {
        public GameObject o;
        public Mesh mesh;
        public MeshCollider meshCollider;
        public MeshRenderer meshRenderer;
        public void UpdateChunkInformation(float[] chunkVertices, float[] chunkNormals, float[] chunkUVs, int[] chunkIndices){
            this.chunkVertices = chunkVertices;
            this.chunkNormals = chunkNormals;
            this.chunkUVs = chunkUVs;
            this.chunkIndices = chunkIndices;
        }
        public float[] chunkVertices;
        public float[] chunkNormals;
        public float[] chunkUVs;
        public int[] chunkIndices;
        public bool isDirty;
    }
    public class EskyTrackerZed : EskyTracker
    {
        public bool useRelativeTransform = false;
        float cam_v_fov = 0;//stub initialization for  later use
        public Material spatialMappingMaterial;
       
        public static EskyTrackerZed zedInstance;
        Dictionary<int,Chunk> myMeshChunks = new Dictionary<int, Chunk>();
        void Start()
        {
            LoadCalibration();
            InitializeTrackerObject();
            RegisterBinaryMapCallback(OnMapCallback);
            RegisterObjectPoseCallback(OnPoseReceivedCallback);
            RegisterLocalizationCallback(OnLocalization);            
            StartTrackerThread(false);        
            AfterInitialization();
        }
        public override void AfterInitialization(){
            zedInstance = this;
            RegisterMeshCallback(OnMeshReceivedCallback);
            RegisterMeshCompleteCallback(OnTransferComplete);
            SetTextureInitializedCallback(OnTextureInitialized);
        }
        public override void SaveEskyMapInformation(){
            ObtainMap();
        }
         public override void LoadEskyMap(EskyMap m){
            retEskyMap = m;
            if(File.Exists("temp.raw"))File.Delete("temp.raw");
            System.IO.File.WriteAllBytes("temp.raw",m.mapBLOB);
            SetMapData(new byte[]{},0);
        }
        void OnDestroy(){
            StopTrackers();
        }
        [MonoPInvokeCallback(typeof(MapDataCallback))]
        static void OnMapCallback(int TrackerID, IntPtr receivedData, int Length)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while(!File.Exists("temp.raw.area")){
                TimeSpan ts = sw.Elapsed;
                if(ts.TotalSeconds > 4)break;                
            }
            sw.Restart();
            bool didComplete = false;
            byte[] received = null;
            while(!didComplete){//this sucks, but it's the only way to wait til the zed has actually _finished_ writing the file
                try{
                    #if ZED_SDK
                    received = System.IO.File.ReadAllBytes("temp.raw.area");
                    #else
                    received = System.IO.File.ReadAllBytes("temp.raw");            
                    #endif

                didComplete = true;//will flag the pass is complete                    
                }catch(System.Exception e){
                    Debug.LogError(e);                    
                    TimeSpan ts = sw.Elapsed;
                    if(ts.TotalSeconds > 4)break; 
                    
                }
            }
            if(didComplete){
                UnityEngine.Debug.Log("Received map data of length: " + received.Length);
                if(instances[TrackerID] != null){
                    EskyMap retEskyMap = new EskyMap();
                    retEskyMap.mapBLOB = received;
                    instances[TrackerID].SetEskyMapInstance(retEskyMap);
                    //I should collect the mesh data here
                }else{
                    UnityEngine.Debug.LogError("The instance of the tracker was null, cancelling data map export");
                }
            }else{
                UnityEngine.Debug.LogError("Problem exporting the map, *shrug*");
            }
            //System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
        }
        public void AddPoseFromCallback(EskyPoseCallbackData epcd){
            callbackEvents = epcd;
        }
        [MonoPInvokeCallback(typeof(LocalizationPoseReceivedCallback))]
        static void OnPoseReceivedCallback(int TrackerID, string ObjectID, float tx, float ty, float tz, float qx, float qy, float qz, float qw){
            EskyPoseCallbackData epcd = new EskyPoseCallbackData();
            (Vector3, Quaternion) vq = instances[TrackerID].IntelPoseToUnity(tx,ty,tz,qx,qy,qz,qw);            
            epcd.PoseID = ObjectID;
            epcd.position = vq.Item1;
            epcd.rotation = vq.Item2;
            ((EskyTrackerZed)instances[TrackerID]).AddPoseFromCallback(epcd);
            UnityEngine.Debug.Log("Received a pose from the relocalization");
        }
        [MonoPInvokeCallback(typeof(MeshChunksReceivedCallback))]
        static void OnMeshReceivedCallback(int ChunkID, IntPtr vertices, int verticesLength, IntPtr normals, int normalsLength, IntPtr uvs, int uvsLength, IntPtr triangleIndices, int triangleIndicesLength)
        {            //System.IO.File.WriteAllBytes("Assets/Resources/Maps/mapdata.txt",received);
            float[] chunkVertices = new float[verticesLength];
            float[] chunkNormals = new float[normalsLength];
            float[] chunkUVs = new float[uvsLength];
            int[] chunkIndices = new int[triangleIndicesLength];
            Marshal.Copy(vertices, chunkVertices, 0, verticesLength);
            Marshal.Copy(normals, chunkNormals, 0, normalsLength);
            Marshal.Copy(triangleIndices, chunkIndices,0,triangleIndicesLength);            
            Debug.Log("Received Mesh ID: " + ChunkID + " with " + verticesLength + "vertices, " + normalsLength + " normals and " + triangleIndicesLength + " triangles!");
            if(zedInstance != null){
                if(!zedInstance.myMeshChunks.ContainsKey(ChunkID)){
                    Chunk chk = new Chunk();
                    chk.UpdateChunkInformation(chunkVertices,chunkNormals,chunkUVs,chunkIndices);
                    zedInstance.myMeshChunks.Add(ChunkID,chk);
                    chk.isDirty = true;
                }else{
                    zedInstance.myMeshChunks[ChunkID].UpdateChunkInformation(chunkVertices,chunkNormals,chunkUVs,chunkIndices);
                    zedInstance.myMeshChunks[ChunkID].isDirty = true;
                }
            }
        }
        [MonoPInvokeCallback(typeof(MeshChunkTransferCompleted))]
        static void OnTransferComplete(){
            Debug.Log("Transfer complete");
            if(zedInstance != null){
                zedInstance.processMeshList = true;               
            }
        }
        [MonoPInvokeCallback(typeof(RenderTextureInitialized))]
        static void OnTextureInitialized(int textureWidth, int textureHeight, int textureChannels,float v_fov){
            if(zedInstance != null){
                zedInstance.textureWidth = textureWidth;
                zedInstance.textureHeight = textureHeight;
                zedInstance.textureChannels = textureChannels;
                zedInstance.hasInitializedTexture = true;
                zedInstance.cam_v_fov = v_fov;
            }
        }
 
        bool processMeshList = false;
        public float ConvertRadiansToDegrees(double radians)
        {
            float degrees = (float)((180f / Math.PI) * radians);
            return (degrees);
        }

        public override void ObtainPose(){
            if(ApplyPoses){
                IntPtr ptr = GetLatestPose();                
                Marshal.Copy(ptr, currentRealsensePose, 0, 7);
                if(useRelativeTransform){
                    transform.localPosition = Vector3.SmoothDamp(transform.position, new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]),ref velocity,smoothing);                     
                    currentEuler = Vector3.SmoothDamp(transform.localRotation.eulerAngles,new Vector3(ConvertRadiansToDegrees(currentRealsensePose[3]),ConvertRadiansToDegrees(currentRealsensePose[4]),ConvertRadiansToDegrees(currentRealsensePose[5])),ref velocityRotation,smoothingRotation);
                    transform.localRotation = Quaternion.Euler(currentEuler);                                                    
                }else{
                    transform.position = Vector3.SmoothDamp(transform.position, new Vector3(currentRealsensePose[0],currentRealsensePose[1],currentRealsensePose[2]),ref velocity,smoothing); 
                    currentEuler = Vector3.SmoothDamp(transform.rotation.eulerAngles,new Vector3(ConvertRadiansToDegrees(currentRealsensePose[3]),ConvertRadiansToDegrees(currentRealsensePose[4]),ConvertRadiansToDegrees(currentRealsensePose[5])),ref velocityRotation,smoothingRotation);
                    transform.rotation = Quaternion.Euler(currentEuler);                
                }

            }
        }

        public bool StartSpatialMappingTest = false;
        public bool StopSpatialMappingTest = false;
        public RenderTexture tex;
        bool canRenderImages = false;
        public UnityEngine.UI.RawImage myImage;
        public Camera previewCamera;
        public override void AfterUpdate() {
            if(hasInitializedTexture){
                HookDeviceToZed();
                Debug.Log("Creating texture with: " + textureChannels + " channels");
                hasInitializedTexture = false;
                if(textureChannels == 4){
                    previewCamera.fieldOfView = cam_v_fov;
                    tex = new RenderTexture(textureWidth,textureHeight,0,RenderTextureFormat.BGRA32);
                    tex.Create();
                    SetRenderTexturePointer(tex.GetNativeTexturePtr());
                    if(myImage != null){
                        myImage.texture = tex;
                        myImage.gameObject.SetActive(true);
                    }
                    canRenderImages = true;
                    StartCoroutine(WaitEndFrameCameraUpdate());
                }
            }
            if(StartSpatialMappingTest){
                StartSpatialMappingTest = false;
                DoStartSpatialMapping();
            }
            if(StopSpatialMappingTest){
                StopSpatialMappingTest = false;
                DoStopSpatialMapping();
            }
            if(processMeshList){
                processMeshList = false;
                CheckChunks();
                CompletedMeshUpdate();
            }
        }
        public void DoStartSpatialMapping(){
            StartSpatialMapping(500);
            if(MeshParent == null){
                MeshParent = new GameObject("MeshParent");                
            }
        }
        public void DoStopSpatialMapping(){
            StopSpatialMapping(500);
        }
        IEnumerator WaitEndFrameCameraUpdate(){
            while(true){
                yield return new WaitForEndOfFrame();
                if(canRenderImages){
                    GL.IssuePluginEvent(GetRenderEventFunc(), 1);
                }
            }
        }
        void CheckChunks(){
            Dictionary<int,Chunk> chunksToUpdateInMain = new Dictionary<int, Chunk>();
            foreach(KeyValuePair<int,Chunk> chunkPairs in myMeshChunks){
                if(chunkPairs.Value.isDirty){
                    chunkPairs.Value.isDirty = false;
                    if(chunkPairs.Value.o != null){
                        List<Vector3> transformedChunkVertices = new List<Vector3>();
                        for(int i = 0; i < chunkPairs.Value.chunkVertices.Length; i+=3){
                            transformedChunkVertices.Add(new Vector3(chunkPairs.Value.chunkVertices[i],chunkPairs.Value.chunkVertices[i+1],chunkPairs.Value.chunkVertices[i+2]));
                        } 
                        List<Vector3> transformedChunkNormals = new List<Vector3>();
                        for(int i = 0; i < chunkPairs.Value.chunkNormals.Length; i+=3){
                            transformedChunkNormals.Add(new Vector3(chunkPairs.Value.chunkNormals[i],chunkPairs.Value.chunkNormals[i+1],chunkPairs.Value.chunkNormals[i+2]));
                        }
                        List<Vector2> transformedChunkUVs = new List<Vector2>();       
                        for(int i = 0; i < chunkPairs.Value.chunkUVs.Length; i+=2){
                            transformedChunkNormals.Add(new Vector3(chunkPairs.Value.chunkUVs[i],chunkPairs.Value.chunkUVs[i+1]));
                        }                           
                        Chunk modifiedJunk = chunkPairs.Value;                        
                        modifiedJunk.mesh.Clear();
                        modifiedJunk.mesh.vertices = transformedChunkVertices.ToArray();
                        modifiedJunk.mesh.normals = transformedChunkNormals.ToArray();                    
                        modifiedJunk.mesh.triangles = chunkPairs.Value.chunkIndices; 
                        chunksToUpdateInMain.Add(chunkPairs.Key,modifiedJunk);                                    
                    }else{
                        List<Vector3> transformedChunkVertices = new List<Vector3>();
                        for(int i = 0; i < chunkPairs.Value.chunkVertices.Length; i+=3){
                            transformedChunkVertices.Add(new Vector3(chunkPairs.Value.chunkVertices[i],chunkPairs.Value.chunkVertices[i+1],chunkPairs.Value.chunkVertices[i+2]));
                        }
                        List<Vector3> transformedChunkNormals = new List<Vector3>();
                        for(int i = 0; i < chunkPairs.Value.chunkNormals.Length; i+=3){
                            transformedChunkNormals.Add(new Vector3(chunkPairs.Value.chunkNormals[i],chunkPairs.Value.chunkNormals[i+1],chunkPairs.Value.chunkNormals[i+2]));
                        }
                        List<Vector2> transformedChunkUVs = new List<Vector2>();       
                        for(int i = 0; i < chunkPairs.Value.chunkUVs.Length; i+=2){
                            transformedChunkNormals.Add(new Vector3(chunkPairs.Value.chunkUVs[i],chunkPairs.Value.chunkUVs[i+1]));
                        }                                     

                        Chunk modifiedJunk = chunkPairs.Value;
                        modifiedJunk.mesh = new Mesh();
                        modifiedJunk.mesh.MarkDynamic();
                        modifiedJunk.mesh.SetVertices(transformedChunkVertices);
                        modifiedJunk.mesh.SetNormals(transformedChunkNormals);                    
                        modifiedJunk.mesh.SetIndices(chunkPairs.Value.chunkIndices,MeshTopology.Triangles,0);
                        modifiedJunk.mesh.UploadMeshData(false);
                        GameObject g = new GameObject("Mesh Chunk - " + chunkPairs);
                        g.AddComponent<MeshRenderer>();
                        g.GetComponent<MeshRenderer>().material = spatialMappingMaterial;
                        g.AddComponent<MeshFilter>();
                        g.GetComponent<MeshFilter>().mesh = chunkPairs.Value.mesh;
                        g.GetComponent<MeshFilter>().sharedMesh = chunkPairs.Value.mesh;
                        modifiedJunk.o = g;
                        chunksToUpdateInMain.Add(chunkPairs.Key,modifiedJunk);
                        g.transform.parent = MeshParent.transform;                                        
                    }
                }
            }
            foreach(KeyValuePair<int,Chunk> kvp in chunksToUpdateInMain){
                myMeshChunks[kvp.Key] = kvp.Value;
            }                                  
        }
        

        #region TrackerSpecific

        [DllImport("libProjectEskyLLAPIZED")]        
        public static extern void SaveOriginPose();

        [DllImport("libProjectEskyLLAPIZED")]        
        public static extern IntPtr GetLatestPose();

        [DllImport("libProjectEskyLLAPIZED")]        
        public static extern void InitializeTrackerObject();

        [DllImport("libProjectEskyLLAPIZED")]        
        public static extern void StartTrackerThread(bool useLocalization);

        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterDebugCallback(debugCallback cb);

        [DllImport("libProjectEskyLLAPIZED")]        
        static extern void StopTrackers();



        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterObjectPoseCallback(LocalizationPoseReceivedCallback poseReceivedCallback);

        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterLocalizationCallback(LocalizationEventCallback cb);

        [DllImport("libProjectEskyLLAPIZED", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterBinaryMapCallback(MapDataCallback cb);
        
        [DllImport("libProjectEskyLLAPIZED")]        
        static extern void SetBinaryMapData(string inputBytesLocation);
        
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void SetObjectPoseInLocalizedMap(string objectID,float tx, float ty, float tz, float qx, float qy, float qz, float qw);

        [DllImport("libProjectEskyLLAPIZED")]        
        static extern void ObtainObjectPoseInLocalizedMap(string objectID);
        [DllImport("libProjectEskyLLAPIZED")]        
        static extern void ObtainMap();
        #endregion
        #region ZED Specific
        delegate void MeshChunkTransferCompleted();
        delegate void MeshChunksReceivedCallback(int ChunkID, IntPtr vertices, int verticesLength, IntPtr normals, int normalsLength, IntPtr uvs, int uvsLength, IntPtr triangleIndices, int triangleIndicesLength);
        delegate void RenderTextureInitialized(int textureWidth, int textureHeight, int textureChannels,float v_fov);
        [DllImport("libProjectEskyLLAPIZED")]
        public static extern IntPtr GetRenderEventFunc();
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void HookDeviceToZed();
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void SetTextureInitializedCallback(RenderTextureInitialized callback);

        [DllImport("libProjectEskyLLAPIZED")]
        static extern void RegisterMeshCompleteCallback(MeshChunkTransferCompleted callback);
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void RegisterMeshCallback(MeshChunksReceivedCallback meshReceivedCallback);        
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void SetMapData(byte[] inputData, int Length);
        
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void StartSpatialMapping(int ChunkSizes);
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void StopSpatialMapping(int ChunkSizes);
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void SetRenderTexturePointer(IntPtr texPointer);
        [DllImport("libProjectEskyLLAPIZED")]
        static extern void CompletedMeshUpdate();
        #endregion
    }
}