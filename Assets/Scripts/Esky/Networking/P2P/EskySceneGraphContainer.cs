using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProtoBuf;
using System.IO;
using ProjectEsky.Networking.WebRTC;

namespace ProjectEsky.Networking{
    public delegate void ItemUpdateDelegate(string key,EskySceneGraphNode value);
    [ProtoContract]
    public enum NetworkOwnership{
        None = 0,
        Local = 1,
        Other = 2
    }
    [ProtoContract]
    public class ProtoVector3{
        [ProtoMember(1)]        
        float x;
        [ProtoMember(2)]
        float y;
        [ProtoMember(3)]        
        float z;
        public Vector3 GetVector3(){
            return new Vector3(x,y,z);
        }
        public void SetFromVector3(Vector3 input){
            x = input.x;
            y = input.y;
            z = input.z;
        }
    }
    [ProtoContract]
    public class ProtoQuaternion{
        [ProtoMember(1)]        
        public float x;
        [ProtoMember(2)]
        public float y;
        [ProtoMember(3)]        
        public float z;
        [ProtoMember(4)]        
        public float w;        
        public Quaternion GetQuaternion(){
            return new Quaternion(x,y,z,w);
        } 
        public void SetFromQuaternion(Quaternion q){
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }
    }
    [ProtoContract]    
    public class EskySceneGraphNode{
        [ProtoMember(1)]        
        public ProtoVector3 positionRelativeToAnchor = new ProtoVector3();
        [ProtoMember(2)]
        public ProtoQuaternion rotationRelativeToAnchor = new ProtoQuaternion();
        [ProtoMember(3)]
        public int RegisteredPrefabIndex;
        [ProtoMember(4)]
        public NetworkOwnership ownership;
        [ProtoMember(5)]
        public string UUID;
        public GameObject hookedObject;
    }
    public class EskySynchronizedSceneGraph{
        public Dictionary<string,EskySceneGraphNode> Items = new Dictionary<string,EskySceneGraphNode>();
        public EskySynchronizedSceneGraph(){}
        public ItemUpdateDelegate ValueChangedEvent;
        public ItemUpdateDelegate ValueAddedEvent;
        public ItemUpdateDelegate ValueRemovedEvent;    
        public void UpdateValue(EskySceneGraphNode value)
        {
            try
            {
                if(Items.ContainsKey(value.UUID)){
                    //Needtoupdate
                    EskySceneGraphNode n = Items[value.UUID];
                    n.positionRelativeToAnchor = value.positionRelativeToAnchor;
                    n.rotationRelativeToAnchor = value.rotationRelativeToAnchor;
                    n.ownership = value.ownership;
                    Items[value.UUID] = n;
                    ValueChangedEvent(value.UUID,n);
                }else{ 
                    Items.Add(value.UUID, value);
                    ValueAddedEvent(value.UUID,Items[value.UUID]);
                }
            }
            catch (System.Exception ex)
            {                
                throw ex;
            }            
        }
        public EskySceneGraphNode UpdateValueLocally(string key, NetworkObject value,int RegisteredPrefabIndex){
           try
            {
                if(Items.ContainsKey(key)){
                    //Needtoupdate
                    EskySceneGraphNode n = Items[key];
                    n.positionRelativeToAnchor.SetFromVector3(value.localPosition);
                    n.rotationRelativeToAnchor.SetFromQuaternion(value.localRotation);
                    n.ownership = value.ownership;
                    n.UUID = key;
                    Items[key] = n; 
                    return n;
                }else{

                    EskySceneGraphNode n = new EskySceneGraphNode();
                    n.RegisteredPrefabIndex = RegisteredPrefabIndex;
                    n.positionRelativeToAnchor.SetFromVector3(value.localPosition);
                    n.rotationRelativeToAnchor.SetFromQuaternion(value.localRotation);
                    n.ownership = value.ownership;
                    n.UUID = key;
                    Items[key] = n; 
                    Items.Add(key, n);
                    return n;
                }
            }
            catch (System.Exception ex)
            {   
                Debug.LogError(ex);             
                throw ex;
            }                        
        }
        public void RemoveValue(string key){
            try{
                if(Items.ContainsKey(key)){
                    ValueRemovedEvent(key,Items[key]);
                    Items.Remove(key);
                }else{
                    throw new System.Exception("Key doesn't exist");
                }
            }catch(System.Exception ex){
                Debug.LogError(ex);                
                throw ex;                
            }
        }
    }
    public class EskySceneGraphContainer : MonoBehaviour
    {
        // Start is called before the first frame update
        public NetworkObject RegisteredClient;
        public List<NetworkObject> RegisteredPrefabs;
        public Dictionary<string,NetworkObject> objectsInScene = new Dictionary<string, NetworkObject>();//key is UUID, value is the associated game object
        public EskySynchronizedSceneGraph mySyncedSceneGraph = new EskySynchronizedSceneGraph();
        public static EskySceneGraphContainer instance;
        public ProjectEsky.Tracking.EskyAnchor SceneOrigin;
        void Awake(){
            instance = this;
            mySyncedSceneGraph.ValueChangedEvent += OnItemUpdated;
            mySyncedSceneGraph.ValueRemovedEvent += OnItemRemoved;
        }
        public void UpdateSceneGraphLocally(NetworkObject networkObject){//this should only ever be done by objects that the local player owns
            EskySceneGraphNode n = mySyncedSceneGraph.UpdateValueLocally(networkObject.UUID,networkObject,networkObject.GetRegisteredPrefabIndex());
            WebRTCPacket p = new WebRTCPacket();
            p.packetType = WebRTCPacketType.PoseGraphSync;
            using(MemoryStream bnStream = new MemoryStream()){
                Serializer.Serialize<EskySceneGraphNode>(bnStream,n);
                p.packetData = bnStream.ToArray();                
            }
            WebRTCDataStreamManager.instance.SendPacket(p);

        }
        public void ReceiveSceneGraphPacket(WebRTCPacket packet){
            using(MemoryStream bnStream = new MemoryStream(packet.packetData)){
                EskySceneGraphNode p = Serializer.Deserialize<EskySceneGraphNode>(bnStream);
                mySyncedSceneGraph.UpdateValue(p);
            }
        }
        public void SubscribeNewItem(string UUID, NetworkObject go){//should be called by all items created on the local player
            if(objectsInScene.ContainsKey(UUID)){  
            }else{
                objectsInScene.Add(UUID,go);
            }
        }
        public NetworkObject SpawnNewNetworkObject(int id){
            if(id > 0 && id < RegisteredPrefabs.Count){
                GameObject g = Instantiate<GameObject>(RegisteredPrefabs[id].gameObject);
                NetworkObject no = g.GetComponent<NetworkObject>();
                no.SetRegisteredPrefabIndex(id);               
                no.Start();
                return no;
            }else{

                return null;
            }
        }
        void OnItemUpdated(string key, EskySceneGraphNode node){
            if(objectsInScene.ContainsKey(key)){
                NetworkObject n = objectsInScene[key];
                if(n.ownership != NetworkOwnership.Local){
                    n.localPosition = node.positionRelativeToAnchor.GetVector3();
                    n.localRotation = node.rotationRelativeToAnchor.GetQuaternion();
                }
            }else{//object doesn't appear to exist in our local scene, we need to create it!
                if(node.RegisteredPrefabIndex >= 0){//above 0 is registered prefabs
                    GameObject g = Instantiate<GameObject>(RegisteredPrefabs[node.RegisteredPrefabIndex].gameObject);
                    NetworkObject no;
                    objectsInScene.Add(key,no = g.GetComponent<NetworkObject>()); 
                    no.UUID = key;                    
                    no.SetRegisteredPrefabIndex(node.RegisteredPrefabIndex);                    
                    no.ActivateNetwork();
                    no.localPosition = node.positionRelativeToAnchor.GetVector3();
                    no.localRotation = node.rotationRelativeToAnchor.GetQuaternion();
                }else{//below 0 is the registered client
                    GameObject g = Instantiate<GameObject>(RegisteredClient.gameObject);
                    NetworkObject no;
                    objectsInScene.Add(key,no = g.GetComponent<NetworkObject>()); 
                    no.UUID = key;
                    no.ActivateNetwork();
                    no.SetRegisteredPrefabIndex(-1);
                    no.ownership = NetworkOwnership.Other;
                    no.localPosition = node.positionRelativeToAnchor.GetVector3();
                    no.localRotation = node.rotationRelativeToAnchor.GetQuaternion();                    
                }
            }
        }
        void OnItemRemoved(string key, EskySceneGraphNode node){
            if(objectsInScene.ContainsKey(key)){
                Destroy(objectsInScene[key].gameObject);
                objectsInScene.Remove(key);
            }
        }
    }
}