using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Calibrator
{
    public class CoordinateSpaceAlignerUI : MonoBehaviour
    {
        public enum InternalState{
            Idle_Invisible,
            Idle_Visible,
            Show_Message_Space,
            Show_Message_Space_Hold,
            Show_Message_Space_Hold_2,
            Show_Message_CompleteCapture0,
            Show_Message_CompleteCapture1,
            Show_Message_CalculatingAlignment0,
            Show_Message_CalculatingAlignment1,
            Show_Message_CalculatingAlignment2                        
        }
        public BEERLabs.ProjectEsky.Rendering.EskyNativeDxRenderer myrendererDriver;

        public CoordinateSpaceAligner myDriver;
        string HandInvisible = "Place your right hand in front of the screen, back facing you!";
        string HandVisible_HitSpace = "Great! Keep still, Hold <Space Bar> to start capturing!";
        string HandVisible_HoldingSpaceUpper = "Now, Keep <Space Bar> held until you finish!";
        string HandVisible_HoldingSpaceUpper2 = "Move your right hand finger tips towards wire spheres!";        
        string HandVisible_HoldingSpaceUpper3 = "Let go of <Space Bar> when you're close enough!";

        string HandVisible_CaptureComplete0 = "Awesome, You should do that 3 - 5 times";
        string HandVisible_CaptureComplete1 = "I think you have enough, Press <A> To Align";
        string CalculatingAlignment0 = "Aweseome, We're processing the calibration...";
        string CalculatingAlignment1 = "If happy, press <S>";
        string CalculatingAlignment2 = "If not, Use arrow keys to adjust, press <R> and try again";



        public UnityEngine.UI.Text UpperText;
        public UnityEngine.UI.Text LowerText;
        public UnityEngine.UI.Text PreviewText;
        // Start is called before the first frame update
        float BetweenTimer = 0f;
        public float TimeBetweenMessages = 4f;
        CoordinateAlignerStates myPrevState = CoordinateAlignerStates.Idle;
        public InternalState myInternalState = InternalState.Idle_Invisible;
        string CurString = "";
        // Update is called once per frame
        public float translationAdjustmentSpeed;        
        void Update()
        {
            if(Input.GetKey(KeyCode.UpArrow)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(0 ,1.0f*translationAdjustmentSpeed*Time.deltaTime , 0);
            }
            if(Input.GetKey(KeyCode.LeftArrow)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(-1.0f*translationAdjustmentSpeed*Time.deltaTime , 0 , 0);
            }            
            if(Input.GetKey(KeyCode.RightArrow)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(1.0f*translationAdjustmentSpeed*Time.deltaTime , 0, 0);
            }            
            if(Input.GetKey(KeyCode.DownArrow)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(0 ,-1.0f*translationAdjustmentSpeed*Time.deltaTime , 0);
            }
            if(Input.GetKey(KeyCode.M)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(0 ,0, 1.0f*translationAdjustmentSpeed*Time.deltaTime );
            }
            if(Input.GetKey(KeyCode.N)){
                myrendererDriver.LeapMotionCamera.transform.localPosition += new Vector3(0, 0 ,-1.0f*translationAdjustmentSpeed*Time.deltaTime);
            }            
            if(myPrevState != myDriver.myCurrentState){
                Debug.Log("Switching To: " + myDriver.myCurrentState);
                SwitchState(myDriver.myCurrentState);
            }
            ProcessInternal();
            UpperText.text = CurString;
        }
        void ProcessInternal(){
            switch(myInternalState){
                case InternalState.Idle_Invisible:
                    if(myDriver.handIsVisible){
                        myInternalState = InternalState.Idle_Visible;
                    }
                    CurString = HandInvisible;
                break;
                case InternalState.Idle_Visible:
                    if(!myDriver.handIsVisible){
                        myInternalState = InternalState.Idle_Invisible;
                    }
                    CurString = HandVisible_HitSpace;                    
                break;
                case InternalState.Show_Message_Space:
                    CurString = HandVisible_HoldingSpaceUpper;
                    if(processTimer()){myInternalState = InternalState.Show_Message_Space_Hold;return;}
                break;
                case InternalState.Show_Message_Space_Hold:
                    CurString = HandVisible_HoldingSpaceUpper2;                
                    if(processTimer()){myInternalState = InternalState.Show_Message_Space_Hold_2;return;}                    
                break;
                case InternalState.Show_Message_Space_Hold_2:
                    CurString = HandVisible_HoldingSpaceUpper3;                
                break;  
                case InternalState.Show_Message_CompleteCapture0:
                CurString = HandVisible_CaptureComplete0;
                if(processTimer()){myInternalState = InternalState.Idle_Invisible;return;}                
                break;
                case InternalState.Show_Message_CompleteCapture1:
                CurString = HandVisible_CaptureComplete1;                                
                break;
                case InternalState.Show_Message_CalculatingAlignment0:
                CurString = CalculatingAlignment0;            
                if(processTimer()){myInternalState = InternalState.Show_Message_CalculatingAlignment1;return;}                                
                break;
                case InternalState.Show_Message_CalculatingAlignment1:
                CurString = CalculatingAlignment1;            
                if(processTimer()){myInternalState = InternalState.Show_Message_CalculatingAlignment2;return;}                
                break;
                case InternalState.Show_Message_CalculatingAlignment2:
                CurString = CalculatingAlignment2;            
                break;                
                default:
                break;
            }
        }
        bool processTimer(){
            BetweenTimer += Time.deltaTime;
            if(BetweenTimer > TimeBetweenMessages){
                BetweenTimer = 0;
                return true;
            }else{
                return false;
            }
        }

        public void SwitchState(CoordinateAlignerStates newState){
            myPrevState = newState;
            switch(newState){
                case CoordinateAlignerStates.Idle:
                break;
                case CoordinateAlignerStates.CaptureInitialPair:
                    myInternalState = InternalState.Show_Message_Space;
                break;
                case CoordinateAlignerStates.CapturingFinalpair:                
                break;
                case CoordinateAlignerStates.CaptureFinalPair:
                    if(myDriver.initialPoints.Count > myDriver.minSamplePointsNeeded){
                        myInternalState = InternalState.Show_Message_CompleteCapture1;
                        myDriver.myCurrentState = CoordinateAlignerStates.PendingAlignment;                        
                    }else{
                        myInternalState = InternalState.Show_Message_CompleteCapture0;                        

                    }
                break;
                case CoordinateAlignerStates.Reset:
                break;
                case CoordinateAlignerStates.Calculating:
                    myInternalState = InternalState.Show_Message_CalculatingAlignment0;                
                break;
            }
        }
    }
}