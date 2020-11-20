using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace ProjectEsky.Networking{
    public struct NetworkTransformation{
        public Vector3 localPosition;
        public Quaternion localRotation;        
        public void UpdateTransform(NetworkTransformation networkTransformation){
            localPosition = networkTransformation.localPosition;
            localRotation = networkTransformation.localRotation;
        }
        public void UpdateTransform(Transform transform, SceneGraphPoseSyncType syncType, float SpeedModifier, float RotationModifier){
            switch(syncType){
                case SceneGraphPoseSyncType.Instant:
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;                
                break;
                case SceneGraphPoseSyncType.Interpolate:                
                transform.localPosition = Vector3.MoveTowards(transform.localPosition,localPosition,Time.deltaTime * SpeedModifier);
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation,localRotation,Time.deltaTime * RotationModifier);
                break;
            }
        }
    }
    public struct Node{
        public int ObjectID;
        public NetworkTransformation transform;
    }

    public struct FingerGraph{
        public NetworkTransformation Knuckle;
        public NetworkTransformation UpperJoint;
        public NetworkTransformation MiddleJoint;
        public NetworkTransformation Tip;
        public void SetFingerInfo(FingerGraph fg){
            Knuckle.UpdateTransform(fg.Knuckle);
            UpperJoint.UpdateTransform(fg.UpperJoint);
            MiddleJoint.UpdateTransform(fg.MiddleJoint);
            Tip.UpdateTransform(fg.Tip);  
        }
    }
    public struct HandGraph{
        public int HandID;
        public FingerGraph IndexFinger;
        public FingerGraph MiddleFinger;
        public FingerGraph RingFinger;
        public FingerGraph PinkyFinger;
        public FingerGraph Thumb;
        public NetworkTransformation Palm;
        public void SetHandGraphInfo(HandGraph info){
            IndexFinger.SetFingerInfo(info.IndexFinger);
            MiddleFinger.SetFingerInfo(info.MiddleFinger);
            RingFinger.SetFingerInfo(info.RingFinger);
            PinkyFinger.SetFingerInfo(info.PinkyFinger);
            Thumb.SetFingerInfo(info.Thumb);
            Palm.UpdateTransform(info.Palm);
        }
    }
    public class SceneGraphNetwork: SyncDictionary<int,Node>{

    }
    public class HandSceneGraph: SyncDictionary<int,HandGraph>{

    }
    public class EskySceneGraphSync : NetworkBehaviour
    {
        public readonly SceneGraphNetwork SceneGraph = new SceneGraphNetwork();
        public readonly HandSceneGraph HandSceneGraphs = new HandSceneGraph(); 
        Dictionary<int,EskySceneGraphNode> SubscribedObjects = new Dictionary<int, EskySceneGraphNode>();
        Dictionary<int,EskyHandSync> SubscribedSyncHands = new Dictionary<int, EskyHandSync>();
        // Start is called before the first frame update
        public override void OnStartClient(){
            SceneGraph.Callback += OnGraphChange;
            HandSceneGraphs.Callback += OnHandGraphChange;
        }
        public void AddHandIDToList(int ID, EskyHandSync ehs){
            if(SubscribedSyncHands.ContainsKey(ID)){
                Debug.LogError("WARNING: multiple handIDs were detected for this scene graph, you should never do this!");
                SubscribedSyncHands[ID] = ehs;
            }else{
                SubscribedSyncHands.Add(ID,ehs);
            }
        }
        public void AddIDToList(int ID, EskySceneGraphNode n){
            if(SubscribedObjects.ContainsKey(ID)){
                Debug.LogError("WARNING: multiple IDs were detected for this scene graph, you should never do this!");
                SubscribedObjects[ID] = n;
            }else{
                SubscribedObjects.Add(ID,n);
            }
        }
        float _timeBeforeSend = 0f;
        Dictionary<int,string> mydict = new Dictionary<int, string>();
        // Update is called once per frame
        void Update()
        {
            if (isLocalPlayer)//local player updates the server scene graph
            {
                if(_timeBeforeSend > syncInterval){
                    _timeBeforeSend = 0;
                    foreach(KeyValuePair<int,EskySceneGraphNode> subbedObj in SubscribedObjects){                     
                        CmdUpdatePose(subbedObj.Key,subbedObj.Value.transform.localPosition,subbedObj.Value.transform.localRotation);
                    }
                    foreach(KeyValuePair<int,EskyHandSync> subbedHand in SubscribedSyncHands){
                        CmdUpdateHand(subbedHand.Value.GetHandInfo());
                    }                    
                }else{
                    _timeBeforeSend += Time.deltaTime;
                }
            }
        }
        [Command]
        public void CmdUpdateHand(HandGraph handGraph){
            if(!HandSceneGraphs.ContainsKey(handGraph.HandID)){
                HandGraph hg = new HandGraph();
                hg.SetHandGraphInfo(handGraph);
                if(SubscribedSyncHands.ContainsKey(handGraph.HandID)){
                    SubscribedSyncHands[handGraph.HandID].UpdateHand(handGraph);
                }else{
                    Debug.LogError("Couldn't find HandID: " + handGraph.HandID);
                }
                HandSceneGraphs.Add(handGraph.HandID,handGraph);
            }else{
                HandGraph hg = HandSceneGraphs[handGraph.HandID];
                hg.SetHandGraphInfo(handGraph);
                if(SubscribedSyncHands.ContainsKey(handGraph.HandID)){
                    SubscribedSyncHands[handGraph.HandID].UpdateHand(handGraph);
                }else{
                    Debug.LogError("Couldn't find HandID: " + handGraph.HandID);
                }
                HandSceneGraphs[handGraph.HandID] = hg;//(handGraph.HandID,handGraph);
            }
        }
        [Command]
        public void CmdUpdatePose(int ID, Vector3 localPosition, Quaternion localRotation){
            if(!SceneGraph.ContainsKey(ID)){
                Node n = new Node();
                n.ObjectID = ID;
                n.transform.localPosition = localPosition;
                n.transform.localRotation = localRotation;                
                if(SubscribedObjects.ContainsKey(n.ObjectID)){
                    SubscribedObjects[n.ObjectID].UpdatePose(n.transform.localPosition,n.transform.localRotation);
                }else{
                    Debug.LogError("Couldn't find ID: " + n.ObjectID);
                }
                SceneGraph.Add(n.ObjectID,n);
            }else{
                Node n = SceneGraph[ID];
                n.transform.localPosition = localPosition;
                n.transform.localRotation = localRotation;
                if(SubscribedObjects.ContainsKey(n.ObjectID)){
                    SubscribedObjects[n.ObjectID].UpdatePose(n.transform.localPosition,n.transform.localRotation);
                }else{
                    Debug.LogError("Couldn't find ID: " + n.ObjectID);
                }
                SceneGraph[ID] = n;
            }
        }
        void OnGraphChange(SyncDictionary<int,Node>.Operation op, int itemIndex, Node newItem)
        {
            if(op == SyncDictionary<int,Node>.Operation.OP_SET || op == SyncDictionary<int,Node>.Operation.OP_ADD){
                if(!isLocalPlayer){
                    Debug.Log("Updating Scene Graph for node");
                    if(SubscribedObjects.ContainsKey(newItem.ObjectID)){
                        SubscribedObjects[newItem.ObjectID].UpdatePose(newItem.transform.localPosition,newItem.transform.localRotation);
                    }else{
                        Debug.LogError("Couldn't find ID: " + newItem.ObjectID);
                    }
                }
            }
        }
        void OnHandGraphChange(SyncDictionary<int,HandGraph>.Operation op, int itemIndex, HandGraph newItem){
            if(op == SyncDictionary<int,HandGraph>.Operation.OP_SET || op == SyncDictionary<int,HandGraph>.Operation.OP_ADD){
                if(!isLocalPlayer){
                    if(SubscribedSyncHands.ContainsKey(newItem.HandID)){
                        SubscribedSyncHands[newItem.HandID].UpdateHand(newItem);
                    }else{
                        Debug.LogError("Couldn't find Hand ID: " + newItem.HandID);
                    }
                }
            }
        }
    }
}