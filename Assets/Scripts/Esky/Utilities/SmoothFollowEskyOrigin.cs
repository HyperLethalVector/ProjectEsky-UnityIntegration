using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
public enum SmoothingType{
    Lerp,
    Linear,
    None
}
public class SmoothFollowEskyOrigin : MonoBehaviour
{
        public bool FollowsTranslationAutomatically = true;
        public bool FollowsRotationAutomatically = true;
        [Range(0.0f, 5.0f)]
        public float DistanceBeforeMoving;
        [Range(0.0f, 5.0f)]        
        public float DistanceBeforeStopping;
        [Range(0.0f, 1.0f)]
        public float RotationRangeBeforeRotating;
        [Range(0.0f, 90.0f)]        
        public float RotationRangeBeforeStopping;  
        [Range(0.0f, 90.0f)]
        public float RotationSpeed;
        [Range(0.0f, 5.0f)]
        public float TranslationSpeed;
        Transform hmdOrigin;
        Transform myOrigin;
        [SerializeField]
        public FollowTarget  targetType;
        [SerializeField]        
        public FollowType followType;
        public Vector3 TranslationOffset;
        Vector3 TargetRotationForward;
        public Vector3 RotationOffset;
        public SmoothingType TranslationSmoothType;
        public SmoothingType RotationSmoothingType;
        bool rotationLock = false;        
        // Start is called before the first frame update
        public void SetPositionRotationLock(bool enabled){
            rotationLock = enabled;
        }
        void Start()
        {
            try{
            switch(targetType){
                case FollowTarget.HMD:
                myOrigin = EskyHMDOrigin.instance.transform;
                break;
                case FollowTarget.LeftHand:
                myOrigin = EskyHandOrigin.instanceLeft.transform;
                break;
                case FollowTarget.RightHand:
                myOrigin = EskyHandOrigin.instanceRight.transform;
                break;
            }

            }catch(System.NullReferenceException e){
                Debug.LogError("There was an issue getting the target, does the relavent eskyhandorigin script exist in scene?: " + gameObject.name + "\n" + e.Message);
                this.enabled = false;
            }
        }

        Vector3 targetPosition;
        Quaternion targetRotation;
        bool updatePosition = false;
        bool updateRotation = false;
        // Update is called once per frame
        void Update()
        {
            if(myOrigin != null){
                if(rotationLock)return;
                if(ApproximateAngle(myOrigin.forward,RotationRangeBeforeRotating) && followType != FollowType.ThreeDOF_Translation && followType != FollowType.ThreeDOF_Translation_FaceHMD){//Solve the rotation first so that local translation will work
                    switch(followType){
                        case FollowType.SixDOF:
                        case FollowType.ThreeDOF_Rotation:                        
                        targetRotation = myOrigin.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.FourDOF_Y_Rotation:          
                        targetRotation = Quaternion.Euler(new Vector3(0,myOrigin.transform.eulerAngles.y,0))* Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.ThreeDOF_Translation_FaceHMD:
                        transform.rotation = Quaternion.LookRotation(hmdOrigin.transform.position - transform.position,Vector3.up);
                        break;
                    }
                    if(FollowsRotationAutomatically){
                        TargetRotationForward = myOrigin.transform.forward;
                        updateRotation = true;
                    }                    

                }else if(ApproximateAngle(myOrigin.forward,RotationRangeBeforeRotating) && followType != FollowType.ThreeDOF_Translation){
                    switch(followType){
                        case FollowType.SixDOF:
                        case FollowType.ThreeDOF_Rotation:                        
                        targetRotation = myOrigin.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.FourDOF_Y_Rotation:          
                        targetRotation = Quaternion.Euler(new Vector3(0,myOrigin.transform.eulerAngles.y,0))* Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.ThreeDOF_Translation_FaceHMD:
                        transform.rotation = Quaternion.LookRotation(hmdOrigin.transform.position - transform.position,Vector3.up);
                        break;
                    }
                    if(FollowsRotationAutomatically){
                        TargetRotationForward = myOrigin.transform.forward;
                        updateRotation = true;
                    }                                        
                }
                if(Vector3.Distance(transform.position,myOrigin.position+ transform.rotation * TranslationOffset) > DistanceBeforeMoving && followType != FollowType.ThreeDOF_Rotation){//Solve the Position
                    switch(followType){
                        case FollowType.SixDOF:
                        case FollowType.ThreeDOF_Translation:                        
                        case FollowType.FourDOF_Y_Rotation:                        
                        targetPosition = myOrigin.transform.position + transform.rotation * TranslationOffset;
                        break;//will expand later into axis limited translation on the Y
                    }
                    if(FollowsTranslationAutomatically)                    
                    updatePosition = true;                    
                }
                if(updateRotation){
                    switch(RotationSmoothingType){
                        case SmoothingType.Lerp:
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);                        
                        break;
                        case SmoothingType.Linear:
                            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);
                        break;
                    }
                    if(!ApproximateAngle(TargetRotationForward,RotationRangeBeforeStopping)){     
                        updateRotation = false;
                    }
                }
                if(updatePosition){
                    switch(RotationSmoothingType){
                        case SmoothingType.Lerp:
                        transform.position = Vector3.Lerp(transform.position,targetPosition,Time.deltaTime * TranslationSpeed);                        
                        break;
                        case SmoothingType.Linear:
                        transform.position = Vector3.MoveTowards(transform.position,targetPosition,Time.deltaTime * TranslationSpeed);
                        break;
                        
                    }
                    if(Vector3.Distance(transform.position,targetPosition) < DistanceBeforeStopping){
                        updatePosition = false;
                    }
                }
                if(followType == FollowType.ThreeDOF_Translation_FaceHMD){
                    transform.rotation = Quaternion.LookRotation(hmdOrigin.transform.position - transform.position,Vector3.up);
                }
            }            
        }
        public void ManuallySetPositionRotationSnap(){
            transform.rotation = targetRotation;
            transform.position = targetPosition;
        }
        public void ManuallyTriggerRotation(){
            switch(followType){
                case FollowType.SixDOF:
                case FollowType.ThreeDOF_Rotation:                        
                targetRotation = myOrigin.transform.rotation * Quaternion.Euler(RotationOffset);
                break;
                case FollowType.FourDOF_Y_Rotation:          
                targetRotation = Quaternion.Euler(new Vector3(0,myOrigin.transform.eulerAngles.y,0))* Quaternion.Euler(RotationOffset);
                break;
                case FollowType.ThreeDOF_Translation_FaceHMD:
                transform.rotation = Quaternion.LookRotation(hmdOrigin.transform.position - transform.position,Vector3.up);
                break;                
            }
            TargetRotationForward = myOrigin.transform.forward;
            updateRotation = true;
        }
        public void ManuallyTriggerPosition(){
            switch(followType){
                case FollowType.SixDOF:
                case FollowType.ThreeDOF_Translation:                        
                case FollowType.FourDOF_Y_Rotation:                        
                targetPosition = myOrigin.transform.position + myOrigin.transform.rotation * TranslationOffset;
                break;//will expand later into axis limited translation on the Y
            }
            updatePosition = true;
        }
        public bool ApproximateAngle(Vector3 forwardDirection, float range) {
//            Debug.Log("Angle: " + Vector3.Dot(forwardDirection,transform.forward));
            return Vector3.Dot(forwardDirection,transform.forward) < range;
        }
    }
}
