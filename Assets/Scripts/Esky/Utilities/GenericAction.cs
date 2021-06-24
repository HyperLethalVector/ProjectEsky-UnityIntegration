using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BEERLabs.ProjectEsky.Desktop{

    
    public enum ActionState{
        Idle,
        Down,
        Highlight
    }

    public enum PowerPointAction{

        PrevSlide,
        NextSlide,
        Exit
    }
    public class GenericAction : MonoBehaviour
    {
        ActionState curState = ActionState.Idle;
        ActionState nextState = ActionState.Idle;
        public GameObject visualizationClick;
        public GameObject visualizationHighlight;
        public UnityEvent OnHighlight;
        public UnityEvent OnClick;
        public UnityEvent OnRelease;
        public UnityEvent OnIdle;
        void ClickMe(){
            if(visualizationClick)visualizationClick.SetActive(true);
            OnClick.Invoke();
        }
        void SwitchToHighlight(){
            if(visualizationHighlight)visualizationHighlight.SetActive(true);            
            OnHighlight.Invoke();            
        }
        void SwitchToIdle(){
            if(visualizationHighlight)visualizationHighlight.SetActive(false);                  
            OnIdle.Invoke();
        }
        void ReleaseMe(){      
            if(visualizationClick)visualizationClick.SetActive(false);        
            OnRelease.Invoke();                
        }
        public void SwitchState(ActionState stateToSwitchTo){
            nextState = stateToSwitchTo;
        }
        private void FixedUpdate() {
            if(curState != nextState){
                switch(curState){//
                    case ActionState.Down:
                    ReleaseMe();
                    break;
                    case ActionState.Highlight:
                    SwitchToIdle();
                    break;
                }
                switch(nextState){
                    case ActionState.Highlight:
                    SwitchToHighlight();
                    break;
                    case ActionState.Down:
                    ClickMe();
                    break;
                }
                curState = nextState;
            }       
        }

    }
}