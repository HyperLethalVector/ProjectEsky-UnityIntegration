using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
namespace ProjectEsky.Networking{
    public enum PoseSynctype{
        Instant = 0,
        Smooth = 1
    }
    public class NetworkObject : MonoBehaviour
    {
        public TMPro.TextMeshPro myLabel;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public NetworkOwnership ownership;
        public string UUID = "";
        [Range(1,200)]
        public float SyncRate = 5;
        float internalSyncRate = 0f;
        float internalSyncTimer = 0f;
        int RegisteredPrefabIndex = -1;
        public PoseSynctype PoseSyncronisationType;
        [Range(0.01f,10)]
        public float SmoothingFactorTranslation;
        [Range(1f,90f)]
        public float SmoothingFactorRotation;
        public PoseSynctype myPoseSyncType;
        // Start is called before the first frame update
        public void Awake() {
            if(UUID == ""){ //this should insist code is only called once
                UUID = Guid.NewGuid().ToString();
                EskySceneGraphContainer.instance.SubscribeNewItem(UUID,this);  
                if(myLabel){
                    myLabel.text = UUID;
                }              
            }
            internalSyncRate = 1.0f/SyncRate;

        }
        // Update is called once per frame
        void Update()
        {
            if(ownership == NetworkOwnership.Local){
                internalSyncTimer += Time.deltaTime;
                if(internalSyncTimer > internalSyncRate){//sync the network node
                    internalSyncTimer = 0f;
                    (Vector3, Quaternion) vals = getPoseRelative();
                    localPosition = vals.Item1;
                    localRotation = vals.Item2;
                    EskySceneGraphContainer.instance.UpdateSceneGraphLocally(this);
                }
            }else{
                setPoseRelative();
            }
        }
        public (Vector3, Quaternion) getPoseRelative(){
            if(EskySceneGraphContainer.instance.SceneOrigin != null){
                Vector3 pos = EskySceneGraphContainer.instance.SceneOrigin.transform.InverseTransformPoint(transform.position);
                Quaternion q = (EskySceneGraphContainer.instance.SceneOrigin.transform.localToWorldMatrix *transform.worldToLocalMatrix).rotation;
                return (pos,q);
            }else{
                return (transform.localPosition,transform.localRotation);
            }
        }
        public void setPoseRelative(){
            if(EskySceneGraphContainer.instance.SceneOrigin != null){
                Vector3 pos = EskySceneGraphContainer.instance.SceneOrigin.transform.TransformPoint(localPosition);
                Quaternion q = EskySceneGraphContainer.instance.SceneOrigin.transform.rotation * localRotation;                
                if(PoseSyncronisationType == PoseSynctype.Smooth){
                    transform.position = Vector3.MoveTowards(transform.position,pos,Time.deltaTime * SmoothingFactorTranslation);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,q,Time.deltaTime * SmoothingFactorRotation);
                }else{
                    transform.position = pos;
                    transform.rotation = q;
                }
            }else{
                if(PoseSyncronisationType == PoseSynctype.Smooth){
                    transform.position = Vector3.MoveTowards(transform.position,localPosition,Time.deltaTime * SmoothingFactorTranslation);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,localRotation,Time.deltaTime * SmoothingFactorRotation);
                }else{
                    transform.position = localPosition;
                    transform.rotation = localRotation;
                }
            }
        }
        public void TakeOwnership(){
            ownership = NetworkOwnership.Local;
        }
        public void RelinquishOwnership(NetworkOwnership newOwner){
            ownership = newOwner;
        }
        public int GetRegisteredPrefabIndex(){
            return RegisteredPrefabIndex;
        }
        public void SetRegisteredPrefabIndex(int newIndex){
            RegisteredPrefabIndex = newIndex;
        }
    }
}