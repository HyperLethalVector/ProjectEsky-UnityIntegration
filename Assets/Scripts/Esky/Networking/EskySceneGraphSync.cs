using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace ProjectEsky.Networking{
    public struct Node{
        public int ObjectID;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    public class SceneGraphNetwork: SyncDictionary<int,Node>{

    }
    public class EskySceneGraphSync : NetworkBehaviour
    {
        public readonly SceneGraphNetwork SceneGraph = new SceneGraphNetwork();
        Dictionary<int,EskySceneGraphNode> SubscribedObjects = new Dictionary<int, EskySceneGraphNode>();
        List<(int,Node)> nodesToUpdate = new List<(int, Node)>(); 
        // Start is called before the first frame update
        public override void OnStartClient(){
            SceneGraph.Callback += OnGraphChange;
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
                }else{
                    _timeBeforeSend += Time.deltaTime;
                }
            }
        }
        [Command]
        public void CmdUpdatePose(int ID, Vector3 localPosition, Quaternion localRotation){
            if(!SceneGraph.ContainsKey(ID)){
                Node n = new Node();
                n.ObjectID = ID;
                n.localPosition = localPosition;
                n.localRotation = localRotation;                
                if(SubscribedObjects.ContainsKey(n.ObjectID)){
                    SubscribedObjects[n.ObjectID].UpdatePose(n.localPosition,n.localRotation);
                }else{
                    Debug.LogError("Couldn't find ID: " + n.ObjectID);
                }
                SceneGraph.Add(n.ObjectID,n);
            }else{
                Node n = SceneGraph[ID];
                n.localPosition = localPosition;
                n.localRotation = localRotation;
                SceneGraph[ID] = n;
            }
        }
        void OnGraphChange(SyncDictionary<int,Node>.Operation op, int itemIndex, Node newItem)
        {
            if(op == SyncDictionary<int,Node>.Operation.OP_SET || op == SyncDictionary<int,Node>.Operation.OP_ADD){
                if(!isLocalPlayer){
                    Debug.Log("Updating Scene Graph for node");
                    if(SubscribedObjects.ContainsKey(newItem.ObjectID)){
                        SubscribedObjects[newItem.ObjectID].UpdatePose(newItem.localPosition,newItem.localRotation);
                    }else{
                        Debug.LogError("Couldn't find ID: " + newItem.ObjectID);
                    }
                }
            }
        }
    }
}