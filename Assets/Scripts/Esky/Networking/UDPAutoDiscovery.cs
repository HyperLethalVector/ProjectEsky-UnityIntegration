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
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.Networking;
using BEERLabs.ProjectEsky.Networking;
namespace BEERLabs.ProjectEsky.Networking.Discovery{    
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

    public class DiscoveredMachine{
        string IP;
        float lastTimePingged;
    }
    public class UDPAutoDiscovery : MonoBehaviour
    {
        [HideInInspector]
        public bool isAdding = false;
        public Dictionary<string, float> ClientsDiscovered = new Dictionary<string, float>();
        [HideInInspector]
        public List<string> cancellationsToSend = new List<string>();
        [Range(1.0f, 10f)]
        public float TimeoutBeforeReactivatingDiscovery = 5f;//5 second timeout before re-activating auto discovery
        [Range(0.0f,9.0f)]
        public float HeartbeatRate = 2f; // 1 time heartbeat every 2 seconds
        public int ClientsFound = 0;
        public bool isHosting = false;
        [Range(0,5000)]
        public int TimeMSBetweenChecks;
        public bool isConnected = false;
        
        [Range(0.0f,5f)]
        public float TimeoutBeforeRemovalFromList = 3f;

        public static UDPAutoDiscovery instance;
        BackgroundWorker objWorkerDiscovery;
        AutoDiscoverySender ads;
        AutoDiscoveryReceiver adr;
        public bool LogInfo;
        float internalHearbeatRate = 0f;
        float internalTimeoutRate = 0f;
        public void Awake(){
            instance = this;
        }
        public void Start() {            
            WebAPIInterface.instance.SubscribeWebEvent("StopDiscovery",StopDiscoveryAnswering);
            StartReceiver();
            if(isHosting){
                StartSender();
            }else{
                WebAPIInterface.instance.SubscribeWebEvent("Heartbeat",ReceiveHeartbeat);
            }
        }
        void StartReceiver(){
            objWorkerDiscovery = new BackgroundWorker();
            objWorkerDiscovery.WorkerReportsProgress = true;
            objWorkerDiscovery.WorkerSupportsCancellation = true;
            adr = new AutoDiscoveryReceiver(ref objWorkerDiscovery,this);
            objWorkerDiscovery.DoWork += new DoWorkEventHandler(adr.Start);
            objWorkerDiscovery.ProgressChanged += new ProgressChangedEventHandler(LogProgressChanged);
            objWorkerDiscovery.RunWorkerAsync();
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
        public void FixedUpdate(){
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
        void Update(){
            while(cancellationsToSend.Count > 0){
                StartCoroutine(CompleteConnection(cancellationsToSend[0]));
                cancellationsToSend.RemoveAt(0);
            }
        }
        public void StartSender(){
            isHosting = true;
            Debug.Log("Starting Sender");
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
            objWorkerDiscovery.CancelAsync();
        }
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
    }
    public class AutoDiscoveryReceiver
    {
        public UDPAutoDiscovery hookedAutoDiscovery;
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

        public AutoDiscoveryReceiver(ref BackgroundWorker workerUDP, UDPAutoDiscovery hookedDiscovery)
        {
            this.workerUDP = workerUDP;
            this.BroadCastDaemonPort = AutoDiscoveryPort;
            this.addrDaemonListenIP = IPAddress.Parse("0.0.0.0");
            this.hookedAutoDiscovery = hookedDiscovery;
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

                            byte[] packetBytesAck = Encoding.Unicode.GetBytes("ACK*"+NetworkingUtils.GetLocalIPAddress()); // Acknowledged
                            newsock.Send(packetBytesAck, packetBytesAck.Length, RemoteEP);
                            this.workerUDP.ReportProgress(1, "Answering(ACK) " + packetBytesAck.Length + " bytes to " + IncomingIP);
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
            public UDPAutoDiscovery hookedAutoDiscovery;
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

            public AutoDiscoverySender(ref BackgroundWorker worker, UDPAutoDiscovery hookedDiscovery)
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
//                        hookedAutoDiscovery.StartCoroutine(hookedAutoDiscovery.CompleteConnection(returnDataSpl[1]));
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