using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.Events;
namespace BEERLabs.ProjectEsky.Utilities{
    public enum PointerState{
        Entered,
        Stayed,
        Exit
    }
    public enum PointerClickState{
        Down,
        Up,
    }
    public class SinglePointerReceiver : MonoBehaviour, IMixedRealityPointerHandler
    {
        
        PointerState currentPointerState = PointerState.Exit;

        PointerClickState currentPointerClickState = PointerClickState.Up;

        PointerState nextPointerState = PointerState.Exit;
        PointerClickState nextPointerClickState = PointerClickState.Up;
        IMixedRealityPointer CurrentPointer;
        public void Awake(){
//            PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOff);
        }
        public void Start(){
            AfterStart();
        }
        public virtual void AfterStart(){

        }
        public virtual void PointerObjectEnters(IMixedRealityPointer p){
        }
        public virtual void PointerObjectLeaves(){
        }
        public virtual void PointerObjectStays(Vector3 startPoint, Vector3 endPoint){
        }
        public virtual void PointerDown(){

        }
        public virtual void PointerUp(){

        }
        // Update is called once per frame
        public void Update()
        {
            foreach(var source in CoreServices.InputSystem.DetectedInputSources)
            {
                // Ignore anything that is not a hand because we want articulated hands
                if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
                {
                    bool foundObject = false;
                    foreach (var p in source.Pointers)
                    {
                        if (p is IMixedRealityNearPointer)
                        {
                            // Ignore near pointers, we only want the rays
                            continue;
                        }
                        if(!p.PointerName.Contains("None"))
                        if (p.Result != null)
                        {
                            GameObject hitObject = p.Result.Details.Object;
                            if (hitObject)
                            {                                    
                                if(hitObject == this.gameObject){                                     
                                    foundObject = true;
                                    CurrentPointer = p;                                    
                                    if(currentPointerState == PointerState.Exit){
                                        nextPointerState = PointerState.Entered;
                                    }else{
                                        nextPointerState = PointerState.Stayed;
                                    }
                                }
                            }
                        }
                    }
                    if(!foundObject){
                        if(currentPointerState != PointerState.Exit){
                            nextPointerState = PointerState.Exit;
                        }
                    }
                }
            }
            if(CurrentPointer != null){
                if(currentPointerState != nextPointerState){
                    currentPointerState = nextPointerState;

                    switch(currentPointerState){
                        case PointerState.Entered:
                            PointerObjectEnters(CurrentPointer);
                        break;

                        case PointerState.Exit:
                            CurrentPointer = null;
                            PointerObjectLeaves();
                        break;
                    }
                }
                if(currentPointerState == PointerState.Stayed){PointerObjectStays(CurrentPointer.Result.StartPoint,CurrentPointer.Result.Details.Point);}
            }else{
                if(currentPointerState != PointerState.Exit){
                    nextPointerState = PointerState.Exit;
                    currentPointerState = nextPointerState;
                    PointerObjectLeaves();
                }
            }
            if(currentPointerClickState != nextPointerClickState){
                currentPointerClickState = nextPointerClickState;
                switch(currentPointerClickState){
                    case PointerClickState.Down:
                    PointerDown();
                    break;
                    case PointerClickState.Up:
                    PointerUp();
                    break;
                }
            }
            AfterUpdate();
        }
        public virtual void AfterUpdate(){

        }
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {                   
            
            bool isNear = eventData.Pointer is IMixedRealityNearPointer;
            if(!isNear){
                nextPointerClickState = PointerClickState.Down;
            }
            eventData.Use();
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            bool isNear = eventData.Pointer is IMixedRealityNearPointer;
            if(!isNear){
                nextPointerClickState = PointerClickState.Up;                
            }            
            eventData.Use();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
//            throw new System.NotImplementedException();
        }
    }
}
