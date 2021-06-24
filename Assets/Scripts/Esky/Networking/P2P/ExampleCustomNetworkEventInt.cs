using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using ProtoBuf;
namespace BEERLabs.ProjectEsky.Networking{
    public class ExampleCustomNetworkEventInt : NetworkEvent
    {
        public UnityEvent<float> floatEvents;
        public Microsoft.MixedReality.Toolkit.UI.PinchSlider mySlider;
        void Start(){
            mySlider = GetComponent<Microsoft.MixedReality.Toolkit.UI.PinchSlider>();
        }
        // Start is called before the first frame update
        float f;
        public void TriggerCustomEvent(){
            f = mySlider.SliderValue;
            TriggerEventTransmission();
        }
        public override void TriggerEventTransmission()
        {
            NetworkEventPacket p = new NetworkEventPacket();
            p.TriggerID = ReceiverID;            
            p.data = System.BitConverter.GetBytes(f);
            SendPacket(p);
        }
        protected override void ProcessEvent(NetworkEventPacket p)
        {
            base.ProcessEvent(p);
            try{
                floatEvents.Invoke(System.BitConverter.ToSingle(p.data,0));
            }catch(System.Exception e){
                Debug.LogError("Something went wrong: " + e.Message);
            }
        }
    }
}