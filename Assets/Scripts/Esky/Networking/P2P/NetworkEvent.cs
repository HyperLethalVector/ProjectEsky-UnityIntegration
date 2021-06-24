using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoBuf;
using System.IO;
using UnityEngine.Events;
using BEERLabs.ProjectEsky.Networking.WebRTC;

namespace BEERLabs.ProjectEsky.Networking{
    [ProtoContract]
    public class NetworkEventPacket{
        [ProtoMember(1)]
        public string TriggerID;
        [ProtoMember(2)]
        public byte[] data;
    }
    public class NetworkEvent : MonoBehaviour
    {
        public static Dictionary<string,NetworkEvent> SubscribedEventReceivers = new Dictionary<string, NetworkEvent>();
        // Start is called before the first frame update
        public string ReceiverID;
        public UnityEvent events;
        
        List<NetworkEventPacket> packetsToProcess = new List<NetworkEventPacket>();
        private void Awake() {
            if(SubscribedEventReceivers.ContainsKey(ReceiverID)){
                Debug.LogWarning("Careful, this event receiver ID exists, overwriting");
                SubscribedEventReceivers[ReceiverID] = this;
            }else{
                SubscribedEventReceivers.Add(ReceiverID,this);    
            }
        }
        public void Update(){
            while(packetsToProcess.Count > 0){
                ProcessEvent(packetsToProcess[0]);
                packetsToProcess.RemoveAt(0);
            }
        }
        protected virtual void ProcessEvent(NetworkEventPacket p){
            events.Invoke();            
        }
        public virtual void TriggerEventTransmission(){
            NetworkEventPacket p = new NetworkEventPacket();
            p.TriggerID = ReceiverID;                        
            SendPacket(p);
        }
        protected virtual void SendPacket(NetworkEventPacket p){
            WebRTCPacket webp = new WebRTCPacket();
            webp.packetType = WebRTCPacketType.EventTrigger;
            using(MemoryStream bnStream = new MemoryStream()){
                Serializer.Serialize<NetworkEventPacket>(bnStream,p);
                webp.packetData = bnStream.ToArray();
                bnStream.Dispose();
            }
            WebRTCDataStreamManager.instance.SendPacket(webp); 
        }
        public void ReceiveEvent(NetworkEventPacket packet){
            packetsToProcess.Add(packet);
        }
        public static void ProcessPacket(WebRTCPacket packet){
            using(MemoryStream bnStream = new MemoryStream(packet.packetData)){
                NetworkEventPacket netp = Serializer.Deserialize<NetworkEventPacket>(bnStream);
                if(SubscribedEventReceivers.ContainsKey(netp.TriggerID)){
                    SubscribedEventReceivers[netp.TriggerID].ReceiveEvent(netp);
                }
                bnStream.Dispose();
            }
        }

    }
}