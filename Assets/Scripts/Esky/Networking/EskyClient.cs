using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
namespace ProjectEsky.Networking{
    public class EskyClient : EskyNetworkEntity
    {
        // Start is called before the first frame update
        public static EskyClient myClient;
        [SyncVar]
        public bool isAR;
        public GameObject clientObjects;
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsLocal;
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsRemote;        
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsAll;
        public bool hasSetAR = false;
        public override void OnStartClient(){
            base.OnStartClient();
            if(isAR){
                if(isLocalPlayer){
                    myClient = this;
                    if(ClientARFlagTriggerEventsLocal != null){
                        ClientARFlagTriggerEventsLocal.Invoke();
                    }
                }else{
                    if(!isAR){
                        if(ClientARFlagTriggerEventsRemote != null){
                            ClientARFlagTriggerEventsRemote.Invoke();
                        }                        
                    }
                }
                if(isAR){
                    if(ClientARFlagTriggerEventsAll != null){
                        ClientARFlagTriggerEventsAll.Invoke();
                    }
                }
            }
        }
        public void TriggerClientARObjects(){
            if(!hasSetAR){
                hasSetAR = true;
                CmdTriggerServerIsClientFlag();
            }
        }
        [Command]
        public void CmdTriggerServerIsClientFlag(){
            isAR = true;
            RpcTriggerClientObjects();            
        }
        [ClientRpc]
        public void RpcTriggerClientObjects(){
            if(isLocalPlayer){
                if(ClientARFlagTriggerEventsLocal != null){
                    ClientARFlagTriggerEventsLocal.Invoke();
                }
            }else{
                    if(ClientARFlagTriggerEventsRemote != null){
                        ClientARFlagTriggerEventsRemote.Invoke();
                    }                        
                }
            if(ClientARFlagTriggerEventsAll != null){
                ClientARFlagTriggerEventsAll.Invoke(); 
            }
        }

    }
}