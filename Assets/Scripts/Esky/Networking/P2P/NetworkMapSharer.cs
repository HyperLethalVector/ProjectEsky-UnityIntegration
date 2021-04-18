using System.Collections;
using System.Collections.Generic;
using ProjectEsky.Tracking;
using UnityEngine;
namespace ProjectEsky.Networking{
    public class NetworkMapSharer : MonoBehaviour
    {
        public static NetworkMapSharer instance;
        public void Awake(){
            instance = this;
        }
        public ProjectEsky.Tracking.EskyTrackerIntel myAttachedTracker;
        public void ReceiveMap(byte[] b){
            EskyMap m = new EskyMap();
            m.mapBLOB = b;
            myAttachedTracker.LoadEskyMap(m);
        }
        public void SendMap(EskyMap m){
            Debug.Log("Sending map of size: " + m.mapBLOB.Length);
            WebRTC.WebRTCPacket p = new WebRTC.WebRTCPacket();
            p.packetType = WebRTC.WebRTCPacketType.MapBLOBShare;
            p.packetData = m.mapBLOB;
            WebRTC.WebRTCDataStreamManager.instance.SendPacket(p);
        }
    }
}
