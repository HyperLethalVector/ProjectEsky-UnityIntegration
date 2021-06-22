using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using BEERLabs.ProjectEsky.Networking.WebAPI;
namespace BEERLabs.ProjectEsky.Networking{

    #region Events
    [System.Serializable]
    public class StringEvent: UnityEvent<string>{
        
    }
    [System.Serializable]
    public class IntEvent: UnityEvent<int>{
        
    }
    [System.Serializable]
    public class FloatEvent: UnityEvent<float>{
        
    }
    [System.Serializable]
    public class ByteEvent: UnityEvent<byte[]>{
        
    }  
    [System.Serializable]
    public class BaseWebEvent{
        [SerializeField]
        public string ID;
    } 
    [System.Serializable]
    public class BasicWebEvent:BaseWebEvent{
        [SerializeField]
        public UnityEvent TriggeredEvents = new UnityEvent();
    }
    
    [System.Serializable]
    public class StringWebEvent:BaseWebEvent{
        [SerializeField]
        public StringEvent TriggeredEvents = new StringEvent();
    }
    [System.Serializable]
    public class IntWebEvent:BaseWebEvent{
        [SerializeField]
        public IntEvent TriggeredEvents = new IntEvent();
    }
    [System.Serializable]
    public class FloatWebEvent:BaseWebEvent{
        [SerializeField]
        public FloatEvent TriggeredEvents = new FloatEvent();
    }
    [System.Serializable]
    public class ByteWebEvent:BaseWebEvent{
        [SerializeField]
        public ByteEvent TriggeredEvents = new ByteEvent();
    }            
    #endregion


    public class WebAPIInterface : MonoBehaviour
    {
        public delegate bool HandleRequestExternalHook(Request request, Response response);
        public List<HandleRequestExternalHook> externalHooks = new List<HandleRequestExternalHook>();
        public static WebAPIInterface instance;
        public UnityEvent HeartBeatTimeoutEvent;
        public List<BasicWebEvent> BaseWebEvents;
        public List<StringWebEvent> StringWebEvents;
        public List<IntWebEvent> IntWebEvents;
        public List<FloatWebEvent> FloatWebEvents;
        public List<ByteWebEvent> ByteWebEvents;
        
        public bool startOnAwake = true;
        public int port = 8079;
        public int workerThreads = 2;
        public bool processRequestsInMainThread = true;
        public bool logRequests = true;
        public JSONRequest requestToSerialize;
        WebServer server;
        Dictionary<string, IWebResource> resources = new Dictionary<string, IWebResource> ();
        void Awake(){
            instance = this;
        }
        void Start ()
        {
            if (processRequestsInMainThread)
                Application.runInBackground = true;
            server = new WebServer (port, workerThreads, processRequestsInMainThread);
            server.logRequests = logRequests;
            server.HandleRequest += HandleRequest;
            server.Start();
        }

        void OnApplicationQuit ()
        {
            server.Dispose ();
        }

        void Update ()
        {
            if (server.processRequestsInMainThread) {
                server.ProcessRequests ();    
            }
        }


        void HandleRequest(Request request, Response response)
        {
            Debug.Log("Handling Request");
            if(request.formData.ContainsKey("APIType")){
                try{
                string s = request.formData["APIType"].Value.Trim();
                string key = request.formData["EventID"].Value.Trim();
                Debug.Log("Checking: " + s + "," + key);
                switch(s){
                    case "Base":
                    Debug.Log("Received Base Event");
                    foreach(BasicWebEvent bwe in BaseWebEvents){
                        if(bwe.ID == key){
                            Debug.Log("Invoking: " + key);
                            bwe.TriggeredEvents.Invoke();
                        }
                    }
                    break;
                    case "String":
                    string data = request.formData["EventData"].Value;
                    foreach(StringWebEvent bwe in StringWebEvents){
                        if(bwe.ID == key){
                            bwe.TriggeredEvents.Invoke(data);
                        }
                    }                    
                    break;
                    case "Int":
                    int dataI = int.Parse(request.formData["EventData"].Value);
                    foreach(IntWebEvent bwe in IntWebEvents){
                        if(bwe.ID == key){
                            bwe.TriggeredEvents.Invoke(dataI);
                        }
                    }                         
                    break;
                    case "Float":
                    int dataF = int.Parse(request.formData["EventData"].Value);
                    foreach(FloatWebEvent bwe in FloatWebEvents){
                        if(bwe.ID == key){
                            bwe.TriggeredEvents.Invoke(dataF);
                        }
                    }                                             
                    break;
                    case "Bytes":
                    byte[] dataB = System.Text.Encoding.UTF32.GetBytes(request.formData["EventData"].Value);
                    foreach(ByteWebEvent bwe in ByteWebEvents){
                        if(bwe.ID == key){
                            bwe.TriggeredEvents.Invoke(dataB);
                        }
                    }                                        
                    break;
                }
                response.statusCode = 200;
                response.message = "OK";
                response.Write(request.uri.LocalPath + " OK");
                }catch(System.Exception  e){
                    Debug.LogError(e);
                response.statusCode = 500;
                response.message = "Issue occured.";
                response.Write(request.uri.LocalPath + " Issue occured.");                    
                }
            }else{
                bool responded = false;
                if(externalHooks.Count > 0){
                    foreach(HandleRequestExternalHook hre in externalHooks){
                        if(hre.Invoke(request, response))responded = true;
                    }
                }
                if(!responded){
                    response.statusCode = 404;
                    response.message = "Not Found.";
                    response.Write(request.uri.LocalPath + " not found.");
                }
            }
        }
        public void SubscribeEvent(HandleRequestExternalHook hookToSubscribe){
            externalHooks.Add(hookToSubscribe);
        }
        public void UnSubscribeEvent(HandleRequestExternalHook hookToSubscribe){
            externalHooks.Remove(hookToSubscribe);
        }

        public void AddResource(string path, IWebResource resource)
        {
            resources[path] = resource;
        }
        BaseWebEvent checkWebEventExists(string ID,List<BaseWebEvent> webEventsToCheck){
            foreach(BaseWebEvent b in webEventsToCheck){
                if(b.ID == ID){
                    return b;
                }
            }
            return null;
        }
        public void SubscribeWebEvent(string ID, UnityAction eventToSubscribe){
            BasicWebEvent existingEvent = (BasicWebEvent)checkWebEventExists(ID,BaseWebEvents.Cast<BaseWebEvent>().ToList());
            if(existingEvent != null){
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);
            }else{
                existingEvent = new BasicWebEvent();
                existingEvent.ID = ID;
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);    
                BaseWebEvents.Add(existingEvent);            
            }
        }
        public void SubscribeWebEvent(string ID, UnityAction<int> eventToSubscribe){
            IntWebEvent existingEvent = (IntWebEvent)checkWebEventExists(ID,IntWebEvents.Cast<BaseWebEvent>().ToList());
            if(existingEvent != null){
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);
            }else{
                existingEvent = new IntWebEvent();
                existingEvent.ID = ID;
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);                
                IntWebEvents.Add(existingEvent);
            }
        }
        public void SubscribeWebEvent(string ID, UnityAction<string> eventToSubscribe){
            StringWebEvent existingEvent = (StringWebEvent)checkWebEventExists(ID,StringWebEvents.Cast<BaseWebEvent>().ToList());
            if(existingEvent != null){
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);
            }else{
                existingEvent = new StringWebEvent();
                existingEvent.ID = ID;
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);                
                StringWebEvents.Add(existingEvent);                
            }
        }
        public void SubscribeWebEvent(string ID, UnityAction<float> eventToSubscribe){
            FloatWebEvent existingEvent = (FloatWebEvent)checkWebEventExists(ID,FloatWebEvents.Cast<BaseWebEvent>().ToList());
            if(existingEvent != null){
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);
            }else{
                existingEvent = new FloatWebEvent();
                existingEvent.ID = ID;
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);  
                FloatWebEvents.Add(existingEvent);             
            }
        }        
        public void SubscribeWebEvent(string ID, UnityAction<byte[]> eventToSubscribe){
            ByteWebEvent existingEvent = (ByteWebEvent)checkWebEventExists(ID,BaseWebEvents.Cast<BaseWebEvent>().ToList());
            if(existingEvent != null){
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);
            }else{
                existingEvent = new ByteWebEvent();
                existingEvent.ID = ID;
                existingEvent.TriggeredEvents.AddListener(eventToSubscribe);                
                ByteWebEvents.Add(existingEvent);
            }
        }
        public void RemoveWebEvent(string ID){//needs to be completed
        
        }

    }
}
