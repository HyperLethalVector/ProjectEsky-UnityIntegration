using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoBuf;
using UnityEngine.Events;
using System.IO;
using System;

namespace ProjectEsky.Networking.WebRTC{
    [ProtoContract]
    public enum WebRTCPacketType{
        Heartbeat = 0,
        PoseGraphSync = 1,
        EventTrigger = 2,
        NetworkObjectCreate = 3,
        MapBLOBShare = 4,
        CustomClass =  5
    }
    [ProtoContract]
    public class WebRTCPacket{
        [ProtoMember(1)]
        public WebRTCPacketType packetType;
        [ProtoMember(2)]
        public byte[] packetData;
    }
    
    public class WebRTCDataStreamManager : MonoBehaviour
    {
        public bool sendHeartBeat = true;
        public string ClientUUID;
        public bool isConnected;        
        public bool canTimeout = false;
        public static WebRTCDataStreamManager instance;
        public UnityEvent<byte[]> onCustomPacketReceive;
        // Start is called before the first frame update
        [Range(0.001f,3f)]
        public float HeartBeatInterval = 1f; // do a heartbeat every second
        [Range(10f,120f)]
        public float DisconnectTimeout = 30f;// Timeout after 30 seconds, then close the webrtc port;


        float timeSinceLastSeenHeartbeat = 0;
        float timeSinceLastSentHeartbeat = 0;
        void Awake(){
            instance = this;
            ClientUUID = Guid.NewGuid().ToString();
        }
        void Start()
        {
        }
        void FixedUpdate(){
            if(isConnected){
                if(sendHeartBeat){
                    timeSinceLastSentHeartbeat += Time.fixedDeltaTime;
                    if(timeSinceLastSentHeartbeat > HeartBeatInterval){
                        timeSinceLastSentHeartbeat = 0;
                        WebRTCPacket p = new WebRTCPacket();
                        p.packetType = WebRTCPacketType.Heartbeat;
                        p.packetData = System.Text.Encoding.Unicode.GetBytes(ClientUUID);
                        SendPacket(p);
                    }
                }
                if(canTimeout){
                    timeSinceLastSeenHeartbeat += Time.fixedDeltaTime;
                    if(timeSinceLastSeenHeartbeat > DisconnectTimeout){
                        timeSinceLastSeenHeartbeat = 0;
                        Discovery.WebRTCAutoDiscoveryHandler.instance.Disconnect();
                        isConnected = false;
                    }
                }
            }
        }
        public void OnConnected(){
            isConnected = true;
            if(WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance != null){
                if(WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.isHosting){
                    if(NetworkMapSharer.instance != null){
                        Debug.Log("Automatically triggering the map receive");
                        NetworkMapSharer.instance.TriggerObtainMap();
                    }
                }
            }
        }
        void OnDisconnected(){
            isConnected = false;
        }
        public void OnReceiveByte(byte[] b){
            using(MemoryStream bnStream = new MemoryStream(b)){
                if(!isConnected){
                    isConnected = true;
                }
                WebRTCPacket p = Serializer.Deserialize<WebRTCPacket>(bnStream);
                OnReceivePacket(p);                
            }

        }
        void OnReceivePacket(WebRTCPacket packetIncoming){

            switch(packetIncoming.packetType){
                case WebRTCPacketType.Heartbeat:
                    timeSinceLastSeenHeartbeat = 0;
                break;
                case WebRTCPacketType.PoseGraphSync:
                if(EskySceneGraphContainer.instance != null){
                    EskySceneGraphContainer.instance.ReceiveSceneGraphPacket(packetIncoming);
                }else{
                    Debug.LogError("A scene graph must exist in order to process scene graph packets");
                }
                break;
                case WebRTCPacketType.MapBLOBShare:
                if(NetworkMapSharer.instance != null){
                    Debug.Log("Received cue to obtain map! Loading....");
                    NetworkMapSharer.instance.ObtainMap();
                }
                break;
                default:
                break;
            }
        }
        public void SendPacket(WebRTCPacket packet){
            if(isConnected){
                using (MemoryStream bnStream = new MemoryStream()){
                    Serializer.Serialize<WebRTCPacket>(bnStream,packet);
                    WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.SendBytes(bnStream.ToArray());
                    bnStream.Dispose();
                }
            }
        }
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
