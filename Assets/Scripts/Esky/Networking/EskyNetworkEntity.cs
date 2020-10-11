﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
namespace ProjectEsky.Networking{
    [RequireComponent(typeof(NetworkIdentity))]
    public class EskyNetworkEntity : NetworkBehaviour
    {
        [SyncVar,SerializeField]
        public Vector3 LocalPosition;
        [SyncVar,SerializeField]
        public Vector3 LocalRotation;
        GameObject closestAnchor;
        public float TranslationSmoothingFactor;
        public float RotationSmoothingFactor;
        public float DistanceBeforeSnap; 
        // Start is called before the first frame update
        [SyncVar]
        string closestAnchorName;
        // Update is called once per frame
        float _timeBeforeSend = 0f;
        float timeBetweenChecks = 0f;
        float timeBetweenUpdates = 1f;
        float timeAtCheck = 0f;
        public virtual void FixedUpdate()
        {               
            if(closestAnchor == null)
            if(Tracking.EskyAnchor.instance != null){
                closestAnchor = ProjectEsky.Tracking.EskyAnchor.instance.gameObject;
            }
            AfterFixedUpdate();
        }
        public virtual void AfterFindAnchor(){

        }
        public virtual void AfterFixedUpdate(){

        }

        public bool ServerControlledEntity = false;
        public void Update()
        {
            if (isLocalPlayer)
            {
                if(Tracking.EskyHMDOrigin.instance != null){
                    Vector3 targetPosition = Tracking.EskyHMDOrigin.instance.transform.position;
                    Quaternion targetRotation = Tracking.EskyHMDOrigin.instance.transform.rotation;
                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                    if(closestAnchor != null)
                    {
                        LocalPosition = closestAnchor.transform.InverseTransformPoint(transform.position);
                        LocalRotation = closestAnchor.transform.InverseTransformDirection(transform.rotation.eulerAngles);
                        if(_timeBeforeSend > syncInterval){
                            CmdUpdatePose(closestAnchorName,LocalPosition, LocalRotation);
                            OnPushMyClientUpdate();
                            _timeBeforeSend = 0;
                        }else{
                            _timeBeforeSend += Time.deltaTime;
                        }
                    }
                    OnMyClientCallback();
                }else{
                    Debug.LogError("ERROR: There must be a HMD origin attached to a gameobject within the scene");//")
                }
            }
            else if(ServerControlledEntity)
            {
                if(isServer){                
                    if(closestAnchor != null)
                    {
                        LocalPosition = closestAnchor.transform.InverseTransformPoint(transform.position);
                        LocalRotation = closestAnchor.transform.InverseTransformDirection(transform.rotation.eulerAngles);
                        if(_timeBeforeSend > syncInterval){
                            CmdUpdatePose(closestAnchorName, LocalPosition, LocalRotation);
                            OnPushMyClientUpdate();
                            _timeBeforeSend = 0;                         
                        }else{
                            _timeBeforeSend += Time.deltaTime;
                        }
                    }
                }else{
                    if (closestAnchor != null)
                    {
                        Vector3 targetPosition = closestAnchor.transform.TransformPoint(LocalPosition);
                        Quaternion targetRotation = Quaternion.Euler(closestAnchor.transform.TransformDirection(LocalRotation));                    
                        transform.position = targetPosition;
                        transform.rotation = targetRotation;
                    }                    
                }
                OnOtherClientCallback();

            }else{ // this entities information is controlled by the server
                if (closestAnchor != null)
                {
                    Vector3 targetPosition = closestAnchor.transform.TransformPoint(LocalPosition);
                    Quaternion targetRotation = Quaternion.Euler(closestAnchor.transform.TransformDirection(LocalRotation));                    
                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                }
                OnOtherClientCallback();
            }
            if(isServer){
                OnServerCallback();
            }
            OnAllCallback();
        }
        public virtual void OnPushMyClientUpdate(){
        
        }
        [Command(channel = 0)]
        public virtual void CmdUpdatePose(string localName, Vector3 _LocalPos, Vector3 _LocalRot)
        {
            closestAnchorName = localName;
            LocalPosition = _LocalPos;
            LocalRotation = _LocalRot;
        }
        protected void SetAnchorGO(GameObject newAnchor)
        {
            closestAnchor = newAnchor;
        }
        public void OnClientDisconnect(NetworkConnection conn)
        {
            Destroy(this.gameObject);
        }
        public virtual void OnMyClientCallback(){

        }
        public virtual void OnServerCallback(){
            
        }
        public virtual void OnOtherClientCallback(){

        }
        public virtual void OnAllCallback(){

        }
        public virtual void GetClosestAnchor(){

        }
        public GameObject FindClosestTarget(string trgt)
        {
            Vector3 position = transform.position;
            return GameObject.FindGameObjectsWithTag(trgt)
                .OrderBy(o => (o.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault();
        }

    }
}