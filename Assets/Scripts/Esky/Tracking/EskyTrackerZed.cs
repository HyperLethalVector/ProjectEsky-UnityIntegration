using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
namespace ProjectEsky.Tracking{
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
        public Material spatialMappingMaterial;
        public GameObject MeshParent;
        public static EskyTrackerZed zedInstance;
        Dictionary<int,Chunk> myMeshChunks = new Dictionary<int, Chunk>();
        public override void AfterInitialization(){
            zedInstance = this;
            RegisterMeshCallback(OnMeshReceivedCallback);
            RegisterMeshCompleteCallback(OnTransferComplete);
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
        bool processMeshList = false;
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

        public bool StartSpatialMappingTest = false;
        public bool StopSpatialMappingTest = false;
        public override void AfterUpdate() {
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
            StartSpatialMapping(45);
            if(MeshParent == null){
                MeshParent = new GameObject("MeshParent");                
            }
        }
        public void DoStopSpatialMapping(){
            StopSpatialMapping(45);
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
        delegate void MeshChunkTransferCompleted();
        delegate void MeshChunksReceivedCallback(int ChunkID, IntPtr vertices, int verticesLength, IntPtr normals, int normalsLength, IntPtr uvs, int uvsLength, IntPtr triangleIndices, int triangleIndicesLength);
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
        static extern void CompletedMeshUpdate();

    }
}