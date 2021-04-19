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
        public Vector3 localScale;
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
        [Range(0.01f,10)]
        public float SmoothingFactorScale;        
        [Range(1f,90f)]
        public float SmoothingFactorRotation;
        public PoseSynctype myPoseSyncType;
        // Start is called before the first frame update
        public void Start() {
            
            if(EskySceneGraphContainer.instance.SceneOrigin != null){
                localPosition = EskySceneGraphContainer.instance.SceneOrigin.transform.InverseTransformPoint(transform.position);
                localRotation= (transform.localToWorldMatrix *EskySceneGraphContainer.instance.SceneOrigin.transform.worldToLocalMatrix).rotation;
            }else{
                localPosition = transform.position;
                localRotation = transform.rotation;
            }
            localScale = transform.localScale;

            if(UUID == ""){ //this should insist code is only called once
                UUID = Guid.NewGuid().ToString();
                EskySceneGraphContainer.instance.SubscribeNewItem(UUID,this);  
                if(myLabel){
                    myLabel.text = UUID;
                }              
            }
            internalSyncRate = 1.0f/SyncRate;

        }
        public void ActivateNetwork() {
            internalSyncRate = 1.0f/SyncRate;
            if(myLabel){
                myLabel.text = UUID;
            }              
        }
        // Update is called once per frame
        void Update()
        {
            if(ownership == NetworkOwnership.Local){
                internalSyncTimer += Time.deltaTime;
                if(internalSyncTimer > internalSyncRate){//sync the network node
                    internalSyncTimer = 0f;
                    (Vector3, Quaternion, Vector3) vals = getPoseRelative();
                    localPosition = vals.Item1;
                    localRotation = vals.Item2;
                    localScale = transform.localScale;
                    EskySceneGraphContainer.instance.UpdateSceneGraphLocally(this);
                }
            }else{
                setPoseRelative();
            }
        }
        public (Vector3, Quaternion, Vector3) getPoseRelative(){
            if(EskySceneGraphContainer.instance.SceneOrigin != null){
                Vector3 pos = EskySceneGraphContainer.instance.SceneOrigin.transform.InverseTransformPoint(transform.position);
                Quaternion q = (transform.localToWorldMatrix *EskySceneGraphContainer.instance.SceneOrigin.transform.worldToLocalMatrix).rotation;
                return (pos,q,transform.localScale);
            }else{
                return (transform.localPosition,transform.localRotation,transform.localScale);
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
            //scaling is separate since it's independent of the synchronized anchors existence, plus the fact it doesn't scale
            transform.localScale = (PoseSyncronisationType == PoseSynctype.Smooth)? Vector3.MoveTowards(transform.localScale,localScale,Time.deltaTime * SmoothingFactorScale): localScale;
        }
        public void TakeOwnership(){
            EskySceneGraphContainer.instance.TakeOwnershipLocally(this);
            Debug.Log("Taking ownership locally");            
        }
        public void RelinquishOwnership(){
            if(ownership == NetworkOwnership.Local){
                Debug.Log("Reqlinquishing ownership locally");
                EskySceneGraphContainer.instance.RevokeOwnershipLocally(this);
            }else{
                Debug.Log("Someone already has control!");                
            }
        }
        public int GetRegisteredPrefabIndex(){
            return RegisteredPrefabIndex;
        }
        public void SetRegisteredPrefabIndex(int newIndex){
            RegisteredPrefabIndex = newIndex;
        }
    }
}