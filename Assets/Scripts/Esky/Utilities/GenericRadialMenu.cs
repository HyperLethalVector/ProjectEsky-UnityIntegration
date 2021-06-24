using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Desktop{
    public class GenericRadialMenu : MonoBehaviour
    {

        public ScaleAnimated myScaledAnimated;
        public List<GenericAction> clickActions = new List<GenericAction>();
        
        bool isDown = false;
        public PinchDetector myPinchDetector;

        GenericAction mcaCurrent = null;        
        public void OpenMenu(){
            myScaledAnimated.SetCanActivate(true);
            gameObject.SetActive(true);
        }
        public void CloseMenu(){
            myScaledAnimated.SetIn(false);
            myScaledAnimated.SetCanActivate(false);
            StartCoroutine(HideAfterSeconds(2));
        }
        IEnumerator HideAfterSeconds(float seconds){
            yield return new WaitForSeconds(seconds);
            gameObject.SetActive(false);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if(mcaCurrent == null){
                mcaCurrent = FindClosestAction();
            }else{
                GenericAction mcaNext = FindClosestAction();
                if(mcaCurrent != mcaNext){
                    mcaCurrent.SwitchState(ActionState.Idle);                    
                    mcaCurrent = mcaNext;
                }
                mcaCurrent.SwitchState(isDown? ActionState.Down : ActionState.Highlight);                
            }
        }
        public void ClickDown(){
            isDown = true;
        }
        public void ClickUp(){
            isDown = false;
        }
        GenericAction FindClosestAction(){

            float distanceCur = -1;
            GenericAction retAction = null;
            for(int i = 0; i < clickActions.Count; i++){
                GenericAction mcacur = clickActions[i];
                if(distanceCur == -1){retAction = mcacur; distanceCur = Vector3.Distance(myPinchDetector.transform.position,mcacur.transform.position);}else{
                    float dist = Vector3.Distance(myPinchDetector.transform.position,mcacur.transform.position);
                    if(dist < distanceCur){
                        distanceCur = dist;
                        retAction = mcacur;
                    }
                }
            }
            return retAction;
        }
    }
}