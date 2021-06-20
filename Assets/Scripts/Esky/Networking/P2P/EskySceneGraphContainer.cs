using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProtoBuf;
using System.IO;
using BEERLabs.ProjectEsky.Networking.WebRTC;

namespace BEERLabs.ProjectEsky.Networking{
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
        public ProtoVector3 scaleRelativeToAnchor = new ProtoVector3();
        [ProtoMember(4)]
        public int RegisteredPrefabIndex;
        [ProtoMember(5)]
        public NetworkOwnership ownership;
        [ProtoMember(6)]
        public string UUID;
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
                    n.scaleRelativeToAnchor = value.scaleRelativeToAnchor;
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
        public void UpdateOwnership(EskySceneGraphNode value){
             if(Items.ContainsKey(value.UUID)){
                    //Needtoupdate
                    EskySceneGraphNode n = Items[value.UUID];
                    n.ownership = value.ownership;
                    Items[value.UUID] = n;
                    ValueChangedEvent(value.UUID,n);
                }else{ 
                    Debug.LogError("Object doesn't exist?");
                }
        }
        public EskySceneGraphNode SetOwnerShip(string Key, NetworkOwnership newOwnership,NetworkObject valueToSet){
            if(Items.ContainsKey(Key)){
                EskySceneGraphNode n = Items[Key];
                n.ownership = newOwnership;
                return n;
            }else{
                EskySceneGraphNode n = new EskySceneGraphNode();
                n.ownership = valueToSet.ownership;
                n.positionRelativeToAnchor.SetFromVector3(valueToSet.localPosition);
                n.rotationRelativeToAnchor.SetFromQuaternion(valueToSet.localRotation);
                n.scaleRelativeToAnchor.SetFromVector3(valueToSet.localScale);
                return n;
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
                    n.scaleRelativeToAnchor.SetFromVector3(value.localScale);
                    n.ownership = value.ownership;
                    n.UUID = key;
                    Items[key] = n; 
                    return n;
                }else{

                    EskySceneGraphNode n = new EskySceneGraphNode();
                    n.RegisteredPrefabIndex = RegisteredPrefabIndex;
                    n.positionRelativeToAnchor.SetFromVector3(value.localPosition);
                    n.rotationRelativeToAnchor.SetFromQuaternion(value.localRotation);
                    n.scaleRelativeToAnchor.SetFromVector3(value.localScale);
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
                return Items[key];                
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
        public BEERLabs.ProjectEsky.Tracking.EskyAnchor SceneOrigin;
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
        public void TakeOwnershipLocally(NetworkObject obj){
            using(MemoryStream bnStream = new MemoryStream()){
                EskySceneGraphNode n = mySyncedSceneGraph.SetOwnerShip(obj.UUID,NetworkOwnership.Local,obj);
                EskySceneGraphNode nNew = new EskySceneGraphNode();
                nNew.UUID = n.UUID;
                nNew.ownership = NetworkOwnership.Other;
                obj.ownership = NetworkOwnership.Local;                
                Serializer.Serialize<EskySceneGraphNode>(bnStream,nNew);

                WebRTCPacket packet = new WebRTCPacket();
                packet.packetType = WebRTCPacketType.NewObjectOwnership;
                packet.packetData = bnStream.ToArray();
                WebRTCDataStreamManager.instance.SendPacket(packet);
            }

        }
        public void RevokeOwnershipLocally(NetworkObject obj){
            using(MemoryStream bnStream = new MemoryStream()){
                EskySceneGraphNode n = mySyncedSceneGraph.SetOwnerShip(obj.UUID,NetworkOwnership.None,obj);
                EskySceneGraphNode nNew = new EskySceneGraphNode();
                nNew.UUID = n.UUID;
                nNew.ownership = NetworkOwnership.None;
                obj.ownership = NetworkOwnership.None;
                Serializer.Serialize<EskySceneGraphNode>(bnStream,nNew);

                WebRTCPacket packet = new WebRTCPacket();
                packet.packetType = WebRTCPacketType.NewObjectOwnership;
                packet.packetData = bnStream.ToArray();
                WebRTCDataStreamManager.instance.SendPacket(packet);
            }
        }
        public void ReceiveNewOwnershipPacket(WebRTCPacket packet){
            using(MemoryStream bnStream = new MemoryStream(packet.packetData)){
                EskySceneGraphNode p = Serializer.Deserialize<EskySceneGraphNode>(bnStream);
                mySyncedSceneGraph.UpdateOwnership(p);
                objectsInScene[p.UUID].ownership = p.ownership;
            }
        }
        public void ReceiveSceneGraphPacket(WebRTCPacket packet){
            using(MemoryStream bnStream = new MemoryStream(packet.packetData)){
                EskySceneGraphNode p = Serializer.Deserialize<EskySceneGraphNode>(bnStream);
                mySyncedSceneGraph.UpdateValue(p);
            }
        }
        public void SubscribeNewItem(string UUID, NetworkObject go){//should be called by all items created on the local player
            Debug.LogWarning("Subscribed: " + UUID);
            if(objectsInScene.ContainsKey(UUID)){ 
                Debug.LogError("Already subscribed!: " + UUID);                 
            }else{
                objectsInScene.Add(UUID,go);
                EskySceneGraphNode n = new EskySceneGraphNode();
                n.UUID = UUID;
                n.positionRelativeToAnchor.SetFromVector3(go.localPosition);
                n.rotationRelativeToAnchor.SetFromQuaternion(go.localRotation);
                n.scaleRelativeToAnchor.SetFromVector3(go.localScale);
                n.ownership = go.ownership;
                OnItemUpdated(UUID,n);
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
                    n.localScale = node.scaleRelativeToAnchor.GetVector3();
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
                    no.localScale = node.scaleRelativeToAnchor.GetVector3();
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
                    no.localScale = node.scaleRelativeToAnchor.GetVector3();                 
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