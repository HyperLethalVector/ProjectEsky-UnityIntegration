using System.Collections;
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
        public bool useSmoothing;
        public virtual void FixedUpdate()//this will be modified when the large scale mapping system becomes prevalent
        {               
            if(closestAnchor == null)
            if(EskyTrackingOrigin.OriginsInScene.Count > 0){
                foreach( KeyValuePair<string,EskyTrackingOrigin> originkeys in EskyTrackingOrigin.OriginsInScene){
                    closestAnchor = originkeys.Value.gameObject;
                    break;
                }
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
            else if(ServerControlledEntity)// this entities information is controlled by the server
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
                        if(useSmoothing){
                            if(Vector3.Distance(transform.position, targetPosition) > DistanceBeforeSnap){
                                transform.position = targetPosition;
                                transform.rotation = targetRotation;
                            }else{
                                transform.position = Vector3.MoveTowards(transform.position,targetPosition,Time.deltaTime*TranslationSmoothingFactor);
                                transform.rotation = Quaternion.RotateTowards(transform.rotation,targetRotation,Time.deltaTime*RotationSmoothingFactor);
                            }
                        }else{
                            transform.position = targetPosition;
                            transform.rotation = targetRotation;
                        }
                    }                    
                }
                OnOtherClientCallback();

            }else{ // this entities information is controlled by another client
                if (closestAnchor != null)
                {
                    Vector3 targetPosition = closestAnchor.transform.TransformPoint(LocalPosition);
                    Quaternion targetRotation = Quaternion.Euler(closestAnchor.transform.TransformDirection(LocalRotation));                    
                    if(useSmoothing){
                        if(Vector3.Distance(transform.position, targetPosition) > DistanceBeforeSnap){
                            transform.position = targetPosition;
                            transform.rotation = targetRotation;
                        }else{
                            transform.position = Vector3.MoveTowards(transform.position,targetPosition,Time.deltaTime*TranslationSmoothingFactor);
                            transform.rotation = Quaternion.RotateTowards(transform.rotation,targetRotation,Time.deltaTime*RotationSmoothingFactor);
                        }
                    }else{
                        transform.position = targetPosition;
                        transform.rotation = targetRotation;
                    }
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