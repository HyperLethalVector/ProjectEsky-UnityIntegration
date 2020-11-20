using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace ProjectEsky.Networking{
    public enum SceneGraphPoseSyncType{
        Instant,
        Interpolate
    }
    public class EskySceneGraphNode : MonoBehaviour {
        public EskySceneGraphSync mySceneGraph;
        public bool usesSmoothing = false;
        public int ID;
        public float SmoothingTranslationValue = 2f;
        public float SmoothingRotationValue = 90f;
        public SceneGraphPoseSyncType SyncType;
        bool UpdatesViaNetwork;
        Vector3 localPositionNetwork = Vector3.zero;
        Quaternion localRotationNetwork = Quaternion.identity;
        public void Start(){
            if(mySceneGraph != null)
            mySceneGraph.AddIDToList(ID,this);
        }
        public void Update(){
            if(mySceneGraph == null){
                if(EskyClient.myClient != null){
                    mySceneGraph = EskyClient.myClient.GetComponent<EskySceneGraphSync>();
                    mySceneGraph.AddIDToList(ID,this);
                }
            }else{
                if(UpdatesViaNetwork){
                    switch(SyncType){
                        case SceneGraphPoseSyncType.Instant:
                        transform.localPosition = localPositionNetwork;
                        transform.localRotation = localRotationNetwork;
                        break;
                        case SceneGraphPoseSyncType.Interpolate:
                        transform.localPosition = Vector3.MoveTowards(transform.localPosition,localPositionNetwork,Time.deltaTime*SmoothingTranslationValue);
                        transform.localRotation = Quaternion.RotateTowards(transform.localRotation,localRotationNetwork,Time.deltaTime*SmoothingRotationValue);
                        break;
                    }
                }
            }
        }
        public void UpdatePose(Vector3 localPosition, Quaternion localRotation){//technically this should only ever be called by a non-local networking client
            localPositionNetwork = localPosition;
            localRotationNetwork = localRotation;
            UpdatesViaNetwork = true;
        }
    }
}