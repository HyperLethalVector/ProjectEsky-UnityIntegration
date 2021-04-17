using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Net;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using Microsoft.MixedReality.WebRTC;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace ProjectEsky.Networking.WebRTC.Discovery{
    public class PackageManagerHookBehaviour : Microsoft.MixedReality.WebRTC.Unity.Signaler{
        public delegate void BytesReceivedDelegate(byte[] b);
        public BytesReceivedDelegate BytesReceived;
        public virtual void SendBytes(byte[] b){
            
        }
        public override Task SendMessageAsync(SdpMessage message)
        {
            return null;
        }

        /// <inheritdoc/>
        public override Task SendMessageAsync(IceCandidate candidate)
        {
            return null;
        }
    }
    

    [System.Serializable]
    public class IceMessageWebsocket{
        public string candidate;
        public int sdpMlineIndex;
        public string sdpMid;
        public IceMessageWebsocket(string cand, int sdpmline, string sdpmid){
            candidate = cand;
            sdpMlineIndex = sdpmline;
            sdpMid = sdpmid;
        }
    }
    [System.Serializable]
    public class SdpMessageWebsocket{
        public string sdp;
        public string type;
        public SdpMessageWebsocket(string sdpMessage, string sdpType){
            sdp = sdpMessage;
            type = sdpType; 
        }
    }
    
    [System.Serializable]
    public class WebrtcShakeClass{
        public List<IceMessageWebsocket> iceMessages;
        public SdpMessageWebsocket sdpMessage;
    }
    public delegate void CreateOfferDelegate();
    public delegate void CreateResponseDelegate();

    public class WebRTCAutoDiscoveryHandler : PackageManagerHookBehaviour
    {
        public List<byte[]> messagesQueue = new List<byte[]>();
        public static WebRTCAutoDiscoveryHandler instance;
        public UnityEvent<byte[]> onDataReceivedFromDataTrack;
        public UnityEvent onConnectionHandled;
        public UnityEvent onConnectionDropped;
        BackgroundWorker objWorkerDiscovery;
        AutoDiscoverySender ads;
        AutoDiscoveryReceiver adr;
        public bool hasMessagePrepared = false;
        public WebrtcShakeClass shake = new WebrtcShakeClass();
        bool createOffer = false;
        bool createAnswer = false;
        bool receiveAnswer = false;
        bool receiveOffer = false;
        bool initConnection = false;
        bool discConnection = false;
        SdpMessage sdpOffer = null;
        SdpMessage sdpAnswer = null;
        [HideInInspector]
        public int connected = 0;
        public bool LogInfo;
        public void Awake(){
            instance = this;
        }
        public bool isConnected(){
            return connected > 1;
        }
        public void Start() {

            objWorkerDiscovery = new BackgroundWorker();
            objWorkerDiscovery.WorkerReportsProgress = true;
            objWorkerDiscovery.WorkerSupportsCancellation = true;
            adr = new AutoDiscoveryReceiver(ref objWorkerDiscovery,CreateOffer,this);
            objWorkerDiscovery.DoWork += new DoWorkEventHandler(adr.Start);
            objWorkerDiscovery.ProgressChanged += new ProgressChangedEventHandler(LogProgressChanged);
            objWorkerDiscovery.RunWorkerAsync();
        }
        private void FixedUpdate() {
            if(createOffer){createOffer = false;PeerConnection.StartConnection();}    
            if(createAnswer){createAnswer = false;PeerConnection.Peer.CreateAnswer();}        
            if(receiveOffer){receiveOffer = false;
                PeerConnection.HandleConnectionMessageAsync(sdpOffer).ContinueWith(_ =>
                {
                    // If the remote description was successfully applied then immediately send
                    // back an answer to the remote peer to acccept the offer.
                    _nativePeer.CreateAnswer();
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);     
            }
            if(receiveAnswer){receiveAnswer = false;
                connected += 1;
                PeerConnection.HandleConnectionMessageAsync(sdpAnswer).ContinueWith(_ =>
                {
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);
            }
            if(initConnection){initConnection = false;onConnectionHandled.Invoke();}
            if(discConnection){discConnection = false;onConnectionDropped.Invoke();}
        }
        protected override void Update(){
            base.Update();
            while(messagesQueue.Count > 0){
                byte[] b = messagesQueue[0];
                messagesQueue.RemoveAt(0);
                onDataReceivedFromDataTrack.Invoke(b);
            }
        }
        public void Finish(){
        }
        public void ReceivedConnection(){
        }
        public void StoppedConnection(){
                onConnectionDropped.Invoke();
        }
        public void ReceiveMessageData(byte[] b){   
            Debug.Log("Receiving Message Data");
            messagesQueue.Add(b); 
        }
        public override void SendBytes(byte[] b){
                knownDataChannels[knownDataChannels.Count-1].SendMessage(b);
        }
        public void Disconnect(){
            PeerConnection.Peer.Close();
        }
        public List<DataChannel> knownDataChannels = new List<DataChannel>();
        public void DataChannelAddedDelegate(DataChannel channel){
            Debug.LogError("Data Channel Added, ID: " + channel.ID + ", Label: " + channel.Label);
            channel.MessageReceived += ReceiveMessageData;
            channel.StateChanged += DataChannelOpen;
            knownDataChannels.Add(channel);
        }
        public void DataChannelOpen(){
            switch(knownDataChannels[0].State){
                case DataChannel.ChannelState.Open:
                initConnection = true;                
                break;
                case DataChannel.ChannelState.Closed:
                discConnection = true;

                break;
            } 
        }
        public override void OnPeerInitialized(){ 
            Debug.Log("On initialized");
            base.OnPeerInitialized();
            PeerConnection.Peer.DataChannelAdded += DataChannelAddedDelegate;
            PeerConnection.Peer.AddDataChannelAsync(0, "transfer", true, true).ContinueWith((prevTask) => 
            { 
                if (prevTask.Exception != null) 
                { 
                    throw prevTask.Exception; 
                } 
                Debug.Log("Added Transfer Channel");
                knownDataChannels.Add(prevTask.Result); 
            });           
        }
        public void StartSender(){
            Debug.Log("Starting Sender");
            shake.iceMessages.Clear();
            if(objWorkerDiscovery != null){
                objWorkerDiscovery.CancelAsync();
            }
            if(adr != null){
                adr.Stop();
                adr = null;
            }
            objWorkerDiscovery = new BackgroundWorker();
            objWorkerDiscovery.WorkerReportsProgress = true;
            objWorkerDiscovery.WorkerSupportsCancellation = true;
            ads = new AutoDiscoverySender(ref objWorkerDiscovery,CreateAnswer,this);
            objWorkerDiscovery.DoWork += new DoWorkEventHandler(ads.Start);
            objWorkerDiscovery.ProgressChanged += new ProgressChangedEventHandler(LogProgressChanged);
            objWorkerDiscovery.RunWorkerAsync();            
        }
        private void LogProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Report thread messages to Console
            if(LogInfo)
            Debug.Log(e.UserState.ToString());
        }

        public void OnDestroy(){
            if(ads != null){
                ads.Stop();
            }
            if(adr != null){

                adr.Stop();
            }
        }
        
        public void CreateOffer(){
            createOffer = true;
        }
        public void CreateAnswer(){
            createAnswer = true;
        }

        public void ReceiveCompletedOffer(WebrtcShakeClass receivedClass){
            ReceiveIceCandidate(receivedClass);            
            sdpOffer = new SdpMessage { Type = SdpMessageType.Offer, Content = receivedClass.sdpMessage.sdp};
            receiveOffer = true;
        }
        public void ReceiveCompletedAnswer(WebrtcShakeClass receivedClass){
            ReceiveIceCandidate(receivedClass);
            sdpAnswer = new SdpMessage { Type = SdpMessageType.Answer, Content = receivedClass.sdpMessage.sdp};                            
            receiveAnswer = true;
        }
        public void ReceiveIceCandidate(WebrtcShakeClass receivedClass){
            for(int i = 0; i < receivedClass.iceMessages.Count; i++){
                _nativePeer.AddIceCandidate(new IceCandidate{SdpMid = receivedClass.iceMessages[i].sdpMid,SdpMlineIndex = receivedClass.iceMessages[i].sdpMlineIndex,Content = receivedClass.iceMessages[i].candidate});
            }
        }
        
        protected override void OnIceCandidateReadyToSend(IceCandidate candidate)
        {
          //Data = string.Join(IceSeparatorChar, candidate.Content, candidate.SdpMlineIndex.ToString(), candidate.SdpMid);      
            IceMessageWebsocket imws = new IceMessageWebsocket(candidate.Content,candidate.SdpMlineIndex,candidate.SdpMid);
            shake.iceMessages.Add(imws);
        }
        protected override void OnSdpOfferReadyToSend(SdpMessage offer)
        {

            SdpMessageWebsocket sdpws = new SdpMessageWebsocket(offer.Content,"offer");
            shake.sdpMessage = sdpws;
        }
        protected override void OnSdpAnswerReadyToSend(SdpMessage answer)
        {

            SdpMessageWebsocket sdpws = new SdpMessageWebsocket(answer.Content,"answer");
            shake.sdpMessage = sdpws;            
        }
    }
    public class AutoDiscoveryReceiver
    {
        public CreateOfferDelegate createOfferDelegate;
        public WebRTCAutoDiscoveryHandler hookedAutoDiscovery;
        private System.ComponentModel.BackgroundWorker workerUDP;

        // Port the UDP server will listen to broadcast packets from UDP Clients.
        private int AutoDiscoveryPort = 18500;

        // Sample byte sequency that Identify a Server Address Request. You may change on the client-side also.
        // Implementing other byte sequences for other actions are also valid. You as developer may know that ;)
        byte[] packetBytes = new byte[] { 0x1, 0x2, 0x3 };

        // In the following example code we reply to incoming client an IP Address that
        // Client must use as server for any purpose. (TCP Server not implemented)
        public IPAddress addrDaemonListenIP;
        
        // Which port we will broadcast as TCP Server (not implemented).
        public int BroadCastDaemonPort = 0;

        private bool disposing = false;

        public AutoDiscoveryReceiver(ref BackgroundWorker workerUDP, CreateOfferDelegate offerDelegate, WebRTCAutoDiscoveryHandler hookedDiscovery)
        {
            this.workerUDP = workerUDP;
            this.BroadCastDaemonPort = AutoDiscoveryPort;
            this.addrDaemonListenIP = IPAddress.Parse("0.0.0.0");
            this.createOfferDelegate = offerDelegate;
            this.hookedAutoDiscovery = hookedDiscovery;
        }

        public void Stop()
        {
            workerUDP.CancelAsync();
            workerUDP.Dispose();
            this.disposing = true;
        }
        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        /// <summary>
        /// Start the listener.
        /// </summary>
        public void Start(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.workerUDP.ReportProgress(30, "AutoDiscoveryReceiver::Service Listening " + this.AutoDiscoveryPort + "/UDP");
                byte[] ReceivedData;


                // Local End-Point
                IPEndPoint LocalEP = new IPEndPoint(IPAddress.Any, AutoDiscoveryPort);
                IPEndPoint RemoteEP = new IPEndPoint(IPAddress.Any, 0);

                UdpClient newsock = new UdpClient(LocalEP);

                ReceivedData = newsock.Receive(ref RemoteEP);

                IPEndPoint IncomingIP = (IPEndPoint)RemoteEP;

                while (!disposing)
                {
                    if(hookedAutoDiscovery.connected < 1){
                        if (ReceivedData.SequenceEqual(packetBytes))
                        {
                            // Use ReportProgress from BackgroundWorker as communication channel between main app and the worker thread.
                            this.workerUDP.ReportProgress(1, "Discovery from " + IncomingIP + "/UDP");

                            // Here we reply the Service IP and Port (TCP).. 
                            // You must point to your server and service port. For example a webserver: sending the correct IP and port 80.
                            createOfferDelegate.Invoke();
                            Thread.Sleep(2000);//wait for the candidates to be generated
                            string s = JsonUtility.ToJson(hookedAutoDiscovery.shake);
                            this.workerUDP.ReportProgress(1, "Got discovered, sending offer: " + s);
                            byte[] packetBytesAck = Encoding.Unicode.GetBytes("ACK*" + s); // Acknowledged
                            newsock.Send(packetBytesAck, packetBytesAck.Length, RemoteEP);
                            this.workerUDP.ReportProgress(1, "Answering(ACK) " + packetBytesAck.Length + " bytes to " + IncomingIP);
                            byte[] packetsToRead = newsock.Receive(ref RemoteEP);
                            if(packetsToRead.Length > 0){

                                string returnedAnswer = Encoding.Unicode.GetString(packetsToRead, 0, packetsToRead.Length);
                                string[] splitAns = returnedAnswer.Split('*');
                                if(splitAns[0] == "RSP"){
                                    this.workerUDP.ReportProgress(1, "Received Answer:" + splitAns[1]);
                                    WebrtcShakeClass retAnswer = JsonUtility.FromJson<WebrtcShakeClass>(splitAns[1]);
                                    hookedAutoDiscovery.ReceiveCompletedAnswer(retAnswer);
                                }                            
                            }
                        }
                        else
                        {
                            // Unknown packet type.
                            this.workerUDP.ReportProgress(1, "Answering(NAK) " + packetBytes.Length + " bytes to " + IncomingIP);
                            byte[] packetBytesNak = Encoding.Unicode.GetBytes("NAK"); // Not Acknowledged

                            newsock.Send(packetBytesNak, packetBytesNak.Length, RemoteEP);
                        }
                        ReceivedData = newsock.Receive(ref RemoteEP);
                    }
                }

                

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
    public class AutoDiscoverySender
        {
            public CreateResponseDelegate createAnswerDelegate;
            public WebRTCAutoDiscoveryHandler hookedAutoDiscovery;
            public bool disposing = false;

            // Fixed Port for broadcast.
            // You may change it but CLIENT and SERVER must be configured with the same port.
            private int AutoDiscoveryPort = 18500;

            // Specify timeout since UDP is a state-less protocol
            // 5000ms - 5 seconds.
            private int ServerSyncTimeout = 5000;
            
            private int AutoDiscoveryTimeout = 10000;

            // Sample byte sequency that Identify a Server Address Request. You may change on the client-side also.
            // Implementing other byte sequences for other actions are also valid. You as developer may know that ;)
            byte[] packetBytes = new byte[] { 0x1, 0x2, 0x3 };

            // Do not change:
            // We will set this variable when Auto Discovery server reply with ACK;
            // Do not change. This will be set if Server is found and check for TCP Connections is OK
            public string ServerAddress = String.Empty;
            public int ServerPort = 0;

            private BackgroundWorker worker;

            public AutoDiscoverySender(ref BackgroundWorker worker, CreateResponseDelegate offerDelegate, WebRTCAutoDiscoveryHandler hookedDiscovery)
            {
                this.worker = worker;
                worker.ReportProgress(1, "AutoDiscoverySender::Started at " + AutoDiscoveryPort + "/UDP");
                this.hookedAutoDiscovery = hookedDiscovery;
                this.createAnswerDelegate = offerDelegate;
            }

            public void Start(object sender, DoWorkEventArgs e)
            {
                try
                {
                    while (this.disposing == false)
                    {
                        if(hookedAutoDiscovery.connected < 1){
                        // Must look for server.. Repeat until configured.
                            if (ServerAddress == String.Empty)
                            {
                                this.worker.ReportProgress(2, "AutoDiscovery::Looking for server..");

                                // Broadcast the query
                                sendBroadcastSearchPacket();
                            }
                            Thread.Sleep(3000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            public void Stop()
            {
                this.disposing = true;
            }

            // Here is to check if the server replied by Auto Discovery is Alive.
            private bool sendBroadcastSearchPacket()
            {

                bool returnVal = false;
                UdpClient udp = new UdpClient();
                udp.EnableBroadcast = true;

                udp.Client.ReceiveTimeout = AutoDiscoveryTimeout;

                IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse("255.255.255.255"), AutoDiscoveryPort);


                try
                {
                    udp.Send(packetBytes, packetBytes.Length, groupEP);

                    byte[] receiveBytes = udp.Receive(ref groupEP);

                    string returnData = Encoding.Unicode.GetString(receiveBytes, 0, receiveBytes.Length);
                    this.worker.ReportProgress(3,"Received Data: " + returnData);   
                    string[] returnDataSpl = returnData.Split('*');
  //                  returnData = returnData.Remove(0,3);  
//                    string substr = returnData.Substring(3,returnData.Length);

                    if (returnDataSpl[0] == "NAK")
                    {
                        this.worker.ReportProgress(3, "AutoDiscovery::INVALID REQUEST");
                    }else if(returnDataSpl[0] == "ACK"){//everything ok
                        this.worker.ReportProgress(3,"It was ACK! " + returnDataSpl[1]);
                        WebrtcShakeClass wrsc = JsonUtility.FromJson<WebrtcShakeClass>(returnDataSpl[1]);
                        hookedAutoDiscovery.ReceiveCompletedOffer(wrsc);
                        Thread.Sleep(4000);
                        string sendOffer = JsonUtility.ToJson(hookedAutoDiscovery.shake);
                        byte[] packetBytesResponse = Encoding.Unicode.GetBytes("RSP*" + sendOffer); // Acknowledged
                        udp.Send(packetBytesResponse,packetBytesResponse.Length,groupEP);                          
//                        newsock.Send(packetBytesResponse, packetBytesResponse.Length, RemoteEP);
                        // Check if the server is reachable! Try to connect it using TCP.
                    }else{
                        this.worker.ReportProgress(3,"RECEIVED GARBAGE?");   
                    }
                    Thread.Sleep(100);
                }
                catch (SocketException e)
                {
                    this.worker.ReportProgress(1, "AutoDiscovery::Timeout. Retrying "+e.Message);

                }

                udp.Close();

                return (returnVal);

            }

        }

}