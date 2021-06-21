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
using BEERLabs.Esky.Networking;
using BEERLabs.Esky.Networking.WebAPI;
using UnityEngine.Networking;

namespace BEERLabs.ProjectEsky.Networking.WebRTC.Discovery{
    public class NetworkingUtils{
        public static string GetLocalIPAddress()
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
    }    
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
        
       
        [HideInInspector]
        public bool isAdding = false;
        public Dictionary<string, float> ClientsDiscovered = new Dictionary<string, float>();
        [HideInInspector]
        public List<string> cancellationsToSend = new List<string>();
        float internalHearbeatRate = 0f;
        float internalTimeoutRate = 0f;
        [Range(1.0f, 10f)]
        public float TimeoutBeforeReactivatingDiscovery = 5f;//5 second timeout before re-activating auto discovery
        [Range(0.0f,9.0f)]
        public float HeartbeatRate = 2f;

        public int ClientsFound = 0;
        public bool isHosting = false;
        [Range(0,5000)]
        public int TimeMSBetweenChecks;
        public bool isConnected = false;
        
        [Range(0.0f,5f)]
        public float TimeoutBeforeRemovalFromList = 3f;

        public static WebRTCAutoDiscoveryHandler instance;
        BackgroundWorker objWorkerDiscovery;
        AutoDiscoverySender ads;
        AutoDiscoveryReceiver adr;
        public bool LogInfo;
        
        [HideInInspector]
        public string HostingIP = "";
        public List<byte[]> messagesQueue = new List<byte[]>();
        public UnityEvent<byte[]> onDataReceivedFromDataTrack;
        public UnityEvent onConnectionHandled;
        public UnityEvent onConnectionDropped;
        public bool hasMessagePrepared = false;
        public WebrtcShakeClass shake = new WebrtcShakeClass();
        bool createOffer = false;
        bool receiveAnswer = false;
        bool receiveOffer = false;
        bool initConnection = false;
        bool discConnection = false;
        SdpMessage sdpOffer = null;
        SdpMessage sdpAnswer = null;
        [HideInInspector]
        public int connected = 0;
        public void Awake(){
            
        }
        public void ReceiveHeartbeat(){
            if(isConnected){
                Debug.Log("Heartbeat Received");
                internalTimeoutRate = 0f;
            }else{
                Debug.Log("Heartbeat received at invalid times stopping the discovery anyways?");
                internalTimeoutRate = 0f;                
                isConnected = true;
                if(adr != null){
                    adr.Stop();
                }
            }
        }
        public void StopDiscoveryAnswering(){
            if(!isConnected){
                Debug.Log("Stopping Discovery from Answering");
                isConnected = true;
                if(adr != null){
                    adr.Stop();
                }
            }else{
                Debug.Log("We are already connected");
            }
        }
        public void Start() {
            instance = this;
            
        }
        public void StartMyConnection(){
            WebAPIInterface.instance.SubscribeWebEvent("StopDiscovery",StopDiscoveryAnswering);
            StartReceiver();
            if(isHosting){
                StartSender();
            }else{
                WebAPIInterface.instance.SubscribeWebEvent("Heartbeat",ReceiveHeartbeat);
            }
            WebAPIInterface.instance.SubscribeEvent(HandleRequest);
        }
        void StartReceiver(){
            objWorkerDiscovery = new BackgroundWorker();
            objWorkerDiscovery.WorkerReportsProgress = true;
            objWorkerDiscovery.WorkerSupportsCancellation = true;
            adr = new AutoDiscoveryReceiver(ref objWorkerDiscovery,this,CreateOffer);
            objWorkerDiscovery.DoWork += new DoWorkEventHandler(adr.Start);
            objWorkerDiscovery.ProgressChanged += new ProgressChangedEventHandler(LogProgressChanged);
            objWorkerDiscovery.RunWorkerAsync();
        }
        public void StartSender(){
            isHosting = true;
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
            ads = new AutoDiscoverySender(ref objWorkerDiscovery,this);
            objWorkerDiscovery.DoWork += new DoWorkEventHandler(ads.Start);
            objWorkerDiscovery.ProgressChanged += new ProgressChangedEventHandler(LogProgressChanged);
            objWorkerDiscovery.RunWorkerAsync();          
        }
        private void FixedUpdate() {
            ProcessHeartBeat();
            if(createOffer){createOffer = false;PeerConnection.StartConnection();}     
            if(receiveOffer){receiveOffer = false;
                PeerConnection.HandleConnectionMessageAsync(sdpOffer).ContinueWith(_ =>
                {
                    // If the remote description was successfully applied then immediately send
                    // back an answer to the remote peer to acccept the offer.
                    waitingToCreateAnswer = true;
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
        public IEnumerator SendHeartbeat(){            
            Debug.Log("Sending Heartbeat");
            List<string> keysToRemove = new List<string>();
            foreach(KeyValuePair<string,float> clientConnected in ClientsDiscovered){
                Debug.Log("Sending Heartbeat to: " + clientConnected.Key);
                WWWForm form = new WWWForm();
                form.AddField("APIType","Base");
                form.AddField("EventID","Heartbeat");                
                UnityWebRequest request = UnityWebRequest.Post("http://"+clientConnected.Key+":8079/",form);
                yield return request.SendWebRequest();
                Debug.Log("Done Sending Heartbeat to: " + clientConnected.Key);
                if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
            		Debug.Log("Issue sending heartbeat, remove from list client: " + clientConnected.Key);
                    keysToRemove.Add(clientConnected.Key);
                }                
                else {
                    Debug.Log("Done Sending Heartbeat");
                }
            }
            while(keysToRemove.Count > 0){
                ClientsDiscovered.Remove(keysToRemove[0]);
                keysToRemove.RemoveAt(0);
            }
            
        }

        [HideInInspector]
        public bool waitingToCreateAnswer = false;
        [HideInInspector]        
        public bool createdAnswer = false;
        public IEnumerator CompleteConnection(string IP){
            WWWForm form = new WWWForm();
            form.AddField("APIType","Base");
            form.AddField("EventID","StopDiscovery");
            UnityWebRequest request = UnityWebRequest.Post("http://"+IP+":8079/",form);
            yield return request.SendWebRequest();
            if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
        		Debug.Log("Issue sending complete connect request, trying again: " + request.error);
                StartCoroutine(CompleteConnection(IP));
            }
            
            else {
              Debug.Log("Done Sending Completion request");
            }

        }        
        void ProcessHeartBeat(){
            //we should rely on the webapi to do things related to cleanup, but for now letting the list accumulate is OK
            //iterate over each entry in the clients dictionary
            if(isHosting){
                if(ClientsFound > 0){
                    internalHearbeatRate += Time.fixedDeltaTime;
                    if(internalHearbeatRate > HeartbeatRate){
                        internalHearbeatRate = 0;
                        StartCoroutine(SendHeartbeat());
                    }
                }
            }else{
                if(isConnected){
                    internalTimeoutRate += Time.fixedDeltaTime;
                    if(internalTimeoutRate > TimeoutBeforeReactivatingDiscovery){
                        Debug.Log("Heartbeat Timeout! Stopping the receiver");
                        isConnected = false;
                        StartReceiver();
                    }
                }
            }
            ClientsFound = ClientsDiscovered.Count;
        }
        protected override void Update(){
            base.Update();
             while(cancellationsToSend.Count > 0){
                StartCoroutine(CompleteConnection(cancellationsToSend[0]));
                cancellationsToSend.RemoveAt(0);
            }            
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
            messagesQueue.Add(b); 
        }
        public override void SendBytes(byte[] b){
                knownDataChannels[1].SendMessage(b);
        }
        public void SendBytesReliable(byte[] b){
                knownDataChannels[1].SendMessage(b);
        }
        public void Disconnect(){
                discConnection = true;
                isConnected = false;                
        }
        public Dictionary<int,DataChannel> knownDataChannels = new Dictionary<int, DataChannel>();
        public void DataChannelAddedDelegate(DataChannel channel){
            Debug.LogError("Data Channel Added, ID: " + channel.ID + ", Label: " + channel.Label);
            channel.MessageReceived += ReceiveMessageData;
            channel.StateChanged += DataChannelOpen;
            knownDataChannels.Add(channel.ID,channel);
        }
        public void DataChannelOpen(){
            switch(knownDataChannels[0].State){
                case DataChannel.ChannelState.Open:
                initConnection = true;                
                isConnected = true;
                break;
                case DataChannel.ChannelState.Closed:
                discConnection = true;
                isConnected = false;
                break;
            } 
        }
        public override void OnPeerInitialized(){ 
            Debug.Log("On initialized");
            base.OnPeerInitialized();
            PeerConnection.Peer.DataChannelAdded += DataChannelAddedDelegate;
            PeerConnection.Peer.AddDataChannelAsync(0, "message_transfer", true, true).ContinueWith((prevTask) => 
            { 
                if (prevTask.Exception != null) 
                { 
                    throw prevTask.Exception; 
                } 
            });    
            PeerConnection.Peer.AddDataChannelAsync(1, "unreliabletransferChannel", true, true).ContinueWith((prevTask) => 
            { 
                if (prevTask.Exception != null) 
                { 
                    throw prevTask.Exception; 
                } 
            });      
            PeerConnection.Peer.AddDataChannelAsync(2, "reliabletransferChannel", true, true).ContinueWith((prevTask) => 
            { 
                if (prevTask.Exception != null) 
                { 
                    throw prevTask.Exception; 
                } 
            });              
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
            StartCoroutine(SendSDPOffer());
        }
        protected override void OnSdpAnswerReadyToSend(SdpMessage answer)
        {

            SdpMessageWebsocket sdpws = new SdpMessageWebsocket(answer.Content,"answer");
            shake.sdpMessage = sdpws;    
            StartCoroutine(SendSDPAnswer());

        }
        public bool HandleRequest(Request request, Response response){
            string key = request.formData["EventID"].Value.Trim();
            

            switch(key){
                case "SDPOffer":
                Debug.Log("Checking: " + key + "," + request.formData["Offer"].Value);                
                shake = JsonUtility.FromJson<WebrtcShakeClass>(request.formData["Offer"].Value);
                receiveOffer = true;                
                response.statusCode = 200;
                response.message = "OK";
                response.Write(request.uri.LocalPath + " OK");                
                return true;
                case "SDPAnswer":
                Debug.Log("Checking: " + key + "," + request.formData["Answer"].Value);                
                shake = JsonUtility.FromJson<WebrtcShakeClass>(request.formData["Answer"].Value);
                receiveAnswer = true;                
                response.statusCode = 200;
                response.message = "OK";
                response.Write(request.uri.LocalPath + " OK");                
                return true;

            }
            return false;
        }
        public IEnumerator SendSDPOffer(){
            JSONRequest jsonreq = new JSONRequest();
            string offer = JsonUtility.ToJson(shake);            
            jsonreq.ModifyRequest("EventID","SDPOffer");
            jsonreq.ModifyRequest("Offer",offer);            
            string location = "http://"+HostingIP+":"+WebAPIInterface.instance.port+"/";
            UnityWebRequest request = UnityWebRequest.Get(location);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(jsonreq)));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type","application/json");
            yield return request.SendWebRequest();
            Debug.Log("Issue sending offer to: " + location);            
            if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
                Debug.Log("Issue sending offer to: " + location);
            }                
            else {
                Debug.Log("Done Sending offer to: " + location);            
            } 
            yield return null;
        }
        public IEnumerator SendSDPAnswer(){
            JSONRequest jsonreq = new JSONRequest();
            string offer = JsonUtility.ToJson(shake);            
            jsonreq.ModifyRequest("EventID","SDPAnswer");
            jsonreq.ModifyRequest("Answer",offer);       
            foreach(KeyValuePair<string,float> client in ClientsDiscovered){                       
                string location = "http://"+client.Key+":"+WebAPIInterface.instance.port+"/";
                UnityWebRequest request = UnityWebRequest.Post(location,JsonUtility.ToJson(jsonreq));
                yield return request.SendWebRequest();
                if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
                    Debug.Log("Issue sending Answer to: " + location);
                }                
                else {
                    Debug.Log("Done Sending Answer to: " + location);            
                }
                yield return null;            
            }

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

        public AutoDiscoveryReceiver(ref BackgroundWorker workerUDP, WebRTCAutoDiscoveryHandler hookedDiscovery, CreateOfferDelegate offerDelegate)
        {
            this.workerUDP = workerUDP;
            this.BroadCastDaemonPort = AutoDiscoveryPort;
            this.addrDaemonListenIP = IPAddress.Parse("0.0.0.0");
            this.hookedAutoDiscovery = hookedDiscovery;
            createOfferDelegate = offerDelegate;
        }

        public void Stop()
        {
            workerUDP.CancelAsync();
            workerUDP.Dispose();
            this.disposing = true;
        }

        /// <summary>
        /// Start the listener.
        /// </summary>
        public void Start(object sender, DoWorkEventArgs e)
        {
            try
            {
//                this.workerUDP.ReportProgress(30, "AutoDiscoveryReceiver::Service Listening " + this.AutoDiscoveryPort + "/UDP");
                byte[] ReceivedData;


                // Local End-Point
                IPEndPoint LocalEP = new IPEndPoint(IPAddress.Any, AutoDiscoveryPort);
                this.workerUDP.ReportProgress(30, "AutoDiscoveryReceiver::Service Listening " +NetworkingUtils.GetLocalIPAddress()+ ":" + this.AutoDiscoveryPort + "/UDP");
                
                IPEndPoint RemoteEP = new IPEndPoint(IPAddress.Any, 0);
                UdpClient newsock = new UdpClient(LocalEP);
                ReceivedData = newsock.Receive(ref RemoteEP);
                IPEndPoint IncomingIP = (IPEndPoint)RemoteEP;
                while (!disposing)
                {
                        if (ReceivedData.SequenceEqual(packetBytes))
                        {
                            // Use ReportProgress from BackgroundWorker as communication channel between main app and the worker thread.
                            this.workerUDP.ReportProgress(1, "Discovery from " + IncomingIP + "/UDP");
                            // Here we reply the Service IP and Port (TCP).. 
                            // You must point to your server and service port. For example a webserver: sending the correct IP and port 80.
                            this.workerUDP.ReportProgress(1, "Got discovered, sending response: bleh");
                            WebRTCAutoDiscoveryHandler.instance.HostingIP = IncomingIP.Address.ToString();
                            this.workerUDP.ReportProgress(1,"Got response from IP: " + IncomingIP.Address.ToString());
                            byte[] packetBytesAck = Encoding.Unicode.GetBytes("ACK*"+NetworkingUtils.GetLocalIPAddress()); // Acknowledged
                            newsock.Send(packetBytesAck, packetBytesAck.Length, RemoteEP);
                            this.workerUDP.ReportProgress(1, "Answering(ACK) " + packetBytesAck.Length + " bytes to " + IncomingIP);
                            createOfferDelegate.Invoke();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    } 
    public class AutoDiscoverySender
        {
            public WebRTCAutoDiscoveryHandler hookedAutoDiscovery;
            public bool disposing = false;

            // Fixed Port for broadcast.
            // You may change it but CLIENT and SERVER must be configured with the same port.
            private int AutoDiscoveryPort = 18500;
            
            private int AutoDiscoveryTimeout = 4000;

            // Sample byte sequency that Identify a Server Address Request. You may change on the client-side also.
            // Implementing other byte sequences for other actions are also valid. You as developer may know that ;)
            byte[] packetBytes = new byte[] { 0x1, 0x2, 0x3 };

            // Do not change:
            // We will set this variable when Auto Discovery server reply with ACK;
            // Do not change. This will be set if Server is found and check for TCP Connections is OK
            public string ServerAddress = String.Empty;
            public int ServerPort = 0;

            private BackgroundWorker worker;

            public AutoDiscoverySender(ref BackgroundWorker worker, WebRTCAutoDiscoveryHandler hookedDiscovery)
            {
                this.worker = worker;
                worker.ReportProgress(1, "AutoDiscoverySender::Started at " + AutoDiscoveryPort + "/UDP");
                this.hookedAutoDiscovery = hookedDiscovery;
            }

            public void Start(object sender, DoWorkEventArgs e)
            {
                try
                {
                    while (this.disposing == false)
                    {
                        // Must look for server.. Repeat until configured.
                            if (ServerAddress == String.Empty)
                            {
                                this.worker.ReportProgress(2, "AutoDiscovery::Looking for server..");
                                // Broadcast the query
                                sendBroadcastSearchPacket();
                            }
                            Thread.Sleep(hookedAutoDiscovery.TimeMSBetweenChecks);
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
                    if (returnDataSpl[0] == "NAK")
                    {
                        this.worker.ReportProgress(3, "AutoDiscovery::INVALID REQUEST");
                    }else if(returnDataSpl[0] == "ACK"){//everything ok
                        this.worker.ReportProgress(3,"It was ACK! " + returnDataSpl[1]);
                        Debug.Log("Received IP Address: " + returnDataSpl[1]);
                        hookedAutoDiscovery.ClientsDiscovered[returnDataSpl[1]] = hookedAutoDiscovery.TimeoutBeforeRemovalFromList;                             
                        hookedAutoDiscovery.cancellationsToSend.Add(returnDataSpl[1]);
                    }else{
                        this.worker.ReportProgress(3,"RECEIVED GARBAGE?");   
                    }
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