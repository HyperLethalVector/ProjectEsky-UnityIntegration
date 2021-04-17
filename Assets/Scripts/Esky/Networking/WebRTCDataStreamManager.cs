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
        CustomClass =  3
    }
    [ProtoContract]
    public class WebRTCPacket{
        [ProtoMember(1)]
        public WebRTCPacketType packetType;
        [ProtoMember(2)]
        public byte[] packetData;
    }
    public class PackageManagerHookBehaviour : MonoBehaviour
    {
        public string ClientUUID;
        public bool isConnected;        
        public bool canTimeout = false;
        public PackageManagerHookBehaviour instance;
        public UnityEvent<byte[]> onCustomPacketReceive;
        // Start is called before the first frame update
        [Range(0.1f,3f)]
        public float HeartBeatInterval = 1f; // do a heartbeat every second
        [Range(10f,120f)]
        public float DisconnectTimeout = 30f;// Timeout after 30 seconds, then close the webrtc port;


        float timeSinceLastSeenHeartbeat = 0;
        float timeSinceLastSentHeartbeat = 0;
        UnityAction<byte[]> actionToAdd;        
        void Awake(){
            instance = this;
            ClientUUID = Guid.NewGuid().ToString();
        }
        void Start()
        {
            actionToAdd += OnReceiveByte;
            Discovery.WebRTCAutoDiscoveryHandler.instance.onDataReceivedFromDataTrack.AddListener(OnReceiveByte);
        }
        void FixedUpdate(){
            if(isConnected){
                timeSinceLastSentHeartbeat += Time.fixedDeltaTime;
                if(timeSinceLastSentHeartbeat > HeartBeatInterval){
                    timeSinceLastSentHeartbeat = 0;
                    WebRTCPacket p = new WebRTCPacket();
                    p.packetType = WebRTCPacketType.Heartbeat;
                    p.packetData = System.Text.Encoding.Unicode.GetBytes(ClientUUID);
                    SendPacket(p);
                }
                if(canTimeout){
                    timeSinceLastSeenHeartbeat += Time.fixedDeltaTime;
                    if(timeSinceLastSeenHeartbeat > DisconnectTimeout){
                        timeSinceLastSeenHeartbeat = 0;
                        Debug.Log("Timout! Disconnecting");
                        Discovery.WebRTCAutoDiscoveryHandler.instance.Disconnect();
                        isConnected = false;
                    }
                }
            }
        }
        void OnConnected(){

        }
        void OnDisconnected(){
            isConnected = false;
        }
        void OnReceiveByte(byte[] b){
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
                UnityEngine.Debug.Log("Received Heartbeat");
                timeSinceLastSeenHeartbeat = 0;
                break;
                default:
                break;
            }
        }
        public void SendPacket(WebRTCPacket packet){
            using (MemoryStream bnStream = new MemoryStream()){
                Serializer.Serialize<WebRTCPacket>(bnStream,packet);
                WebRTC.Discovery.WebRTCAutoDiscoveryHandler.instance.SendBytes(bnStream.GetBuffer());
                bnStream.Dispose();
            }
        }
    }
}
