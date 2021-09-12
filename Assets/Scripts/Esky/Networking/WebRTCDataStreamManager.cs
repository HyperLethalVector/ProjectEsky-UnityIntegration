using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoBuf;
using UnityEngine.Events;
using System.IO;
using System;

namespace BEERLabs.ProjectEsky.Networking.WebRTC{
    //These are the possible packet types that can be sent across
    [ProtoContract]
    public enum WebRTCPacketType{
        Heartbeat = 0,
        PoseGraphSync = 1,
        NewObjectOwnership = 2,
        EventTrigger = 3,
        NetworkObjectCreate = 4,
        MapBLOBShare = 5,
        ForcedMapRefresh = 6,
        CustomClass =  7
    }
    //This is the base class for the webrtc packet, including it's data (which can be a custom serialized class' byte array representation)
    [ProtoContract]
    public class WebRTCPacket{
        [ProtoMember(1)]
        public WebRTCPacketType packetType;
        [ProtoMember(2)]
        public byte[] packetData;
    }
    
    public class WebRTCDataStreamManager : MonoBehaviour
    {
        //The 'heartbeat' system is how we keep a connection 'alive'. Should the heart beat fail, we can 
        //time out the connection, close the webrtc connection, and clean it up so that we can re-connect again later.
        //this is only used with the autodiscovery based signaling
        public bool sendHeartBeat = true; // this says whether we are sending the 'heartbeat' to keep the connection alive, if this is false, the auto discovery is redisabled
        public string ClientUUID; // my client ID
        public bool isConnected;        //have we connected?
        public bool canTimeout = false; //can we time out?
        public static WebRTCDataStreamManager instance; // the data stream manager instance
        public UnityEvent<byte[]> onCustomPacketReceive; // the event subscriber 
        // Start is called before the first frame update
        [Range(0.001f,3f)]
        public float HeartBeatInterval = 1f; // do a heartbeat every second
        [Range(10f,120f)]
        public float DisconnectTimeout = 30f;// Timeout after 30 seconds, then close the webrtc port;


        float timeSinceLastSeenHeartbeat = 0; // how long has it been since we have seen a heartbeat?
        float timeSinceLastSentHeartbeat = 0; // how long has it been since we have sent a heartbeat?
        void Awake(){
            instance = this;
            ClientUUID = Guid.NewGuid().ToString();
        }
        void Start()
        {
        }
        void FixedUpdate(){
            if(isConnected){
                if(sendHeartBeat){ //should we be sending the webrtc packet heartbeat? 
                    timeSinceLastSentHeartbeat += Time.fixedDeltaTime;
                    if(timeSinceLastSentHeartbeat > HeartBeatInterval){
                        timeSinceLastSentHeartbeat = 0;
                        WebRTCPacket p = new WebRTCPacket();
                        p.packetType = WebRTCPacketType.Heartbeat;
                        p.packetData = System.Text.Encoding.Unicode.GetBytes(ClientUUID);
                        SendPacket(p);
                    }
                }
                if(canTimeout){ //can we time out and disconnect?
                    timeSinceLastSeenHeartbeat += Time.fixedDeltaTime;
                    if(timeSinceLastSeenHeartbeat > DisconnectTimeout){
                        timeSinceLastSeenHeartbeat = 0;
                        Discovery.WebRTCAutoDiscoveryHandler.instance.Disconnect();
                        isConnected = false;
                    }
                }
            }
        }
        public void OnConnected(){//this is called once the webrtc connection is made
            isConnected = true;
            if(WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance != null){//if there is a auto discovery handler, we attach to it and obtain the device's local map
                if(WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.isHosting){
                    if(NetworkMapSharer.instance != null){
                        Debug.Log("Automatically triggering the map receive");
                        NetworkMapSharer.instance.TriggerObtainMap();
                    }
                }
            }
        }
        void OnDisconnected(){//this is called when the webrtc system detects a disconnect
            isConnected = false;
        }
        //this is the function you need to hook up to whatever webrtc system we're using with a data stream!
        public void OnReceiveByte(byte[] b){//Whenever we receive a byte array from the webrtc package
            using(MemoryStream bnStream = new MemoryStream(b)){//deserialize it, assuming that it is indeed a WebRTC Packet of some kind
                if(!isConnected){
                    isConnected = true;
                }
                WebRTCPacket p = Serializer.Deserialize<WebRTCPacket>(bnStream);
                OnReceivePacket(p);//receive the packet
            }

        }
        //Whenever we receive the packet
        void OnReceivePacket(WebRTCPacket packetIncoming){
            //We process the incoming webrtc packet
            switch(packetIncoming.packetType){
                //If it is a heartbeat, reset the timer
                case WebRTCPacketType.Heartbeat:
                    timeSinceLastSeenHeartbeat = 0;
                break;
                //If it is a pose graph that should be synced up, process it 
                case WebRTCPacketType.PoseGraphSync:
                if(EskySceneGraphContainer.instance != null){
                    EskySceneGraphContainer.instance.ReceiveSceneGraphPacket(packetIncoming);
                }else{
                    Debug.LogError("A scene graph must exist in order to process scene graph packets");
                }
                break;
                //If someone has taken ownership of an object, ensure that is processed
                case WebRTCPacketType.NewObjectOwnership:
                if(EskySceneGraphContainer.instance != null){
                    EskySceneGraphContainer.instance.ReceiveNewOwnershipPacket(packetIncoming);
                }else{
                    Debug.LogError("A scene graph must exist in order to process scene graph packets");
                }
                break;
                //this is no longer used, and is part of the webapi
                case WebRTCPacketType.MapBLOBShare:
                if(NetworkMapSharer.instance != null){
                    Debug.Log("Received cue to obtain map! Loading....");
                    NetworkMapSharer.instance.ObtainMap();
                }
                break;
                //Are we triggering some subscribed function
                case WebRTCPacketType.EventTrigger:
                    NetworkEvent.ProcessPacket(packetIncoming);
                break;
                default:
                break;
            }
        }
        //This is the OTHER half of the packet system, we create the packet externally, then send it via the stream manager
        //One example is the heartbeat, that can be found in the fixedUpdate function
        public void SendPacket(WebRTCPacket packet){
            if(isConnected){
                using (MemoryStream bnStream = new MemoryStream()){
                    Serializer.Serialize<WebRTCPacket>(bnStream,packet);
                    WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.SendBytes(bnStream.ToArray());
                    bnStream.Dispose();
                }
            }
        }
        //This is used only when we have reliable data channels
        public void SendPacketReliable(WebRTCPacket packet){
            if(isConnected){
                using (MemoryStream bnStream = new MemoryStream()){
                    Serializer.Serialize<WebRTCPacket>(bnStream,packet);
                    WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.SendBytesReliable(bnStream.ToArray());
                    bnStream.Dispose();
                }
            }
        }
    }
}
