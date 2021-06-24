using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    [System.Serializable]
    public enum FollowType{
        SixDOF = 0,
        FourDOF_Y_Rotation = 1,
        ThreeDOF_Translation = 2,
        ThreeDOF_Rotation = 3,
        Floor_FourDOF_Y_ROTATION = 4,
        ThreeDOF_Translation_FaceHMD = 5
    }
    [System.Serializable]
    public enum FollowTarget{
        HMD,
        LeftHand,
        RightHand
    }
    public class FollowEskyOrigin : MonoBehaviour
    {
        Transform myOrigin;
        Transform hmdOrigin;
        [SerializeField]
        public FollowTarget  targetType;
        [SerializeField]        
        public FollowType followType;
        public bool ApplyPoses = true;//this will be enabled by the server on local clients
        public Vector3 TranslationOffset;
        public Vector3 RotationOffset;
        // Start is called before the first frame update
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
                Debug.LogError("There was an issue getting the target, does the relavent eskyhandorigin script exist in scene?: " + e);
            }
            hmdOrigin = EskyHMDOrigin.instance.transform;
        }

        // Update is called once per frame
        void Update()
        {
            if(myOrigin != null){
                if(ApplyPoses){

                    switch(followType){
                        case FollowType.SixDOF:
                        transform.position = myOrigin.transform.position + ( myOrigin.transform.rotation * TranslationOffset);
                        transform.rotation = myOrigin.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.ThreeDOF_Translation:
                        transform.position = myOrigin.transform.position + ( myOrigin.transform.rotation * TranslationOffset);
                        break;
                        case FollowType.ThreeDOF_Rotation:
                        transform.rotation = myOrigin.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.FourDOF_Y_Rotation:
                        transform.position = myOrigin.transform.position + ( myOrigin.transform.rotation * TranslationOffset);
                        transform.rotation = Quaternion.Euler(new Vector3(0,myOrigin.transform.eulerAngles.y,0))* Quaternion.Euler(RotationOffset);
                        break;
                        case FollowType.ThreeDOF_Translation_FaceHMD:
                        transform.position = myOrigin.transform.position + ( myOrigin.transform.rotation * TranslationOffset);                        
                        transform.rotation = Quaternion.LookRotation(myOrigin.transform.position - hmdOrigin.transform.position,hmdOrigin.transform.up) * Quaternion.Euler(RotationOffset);
                        break;
                    }
                }
            }else{
                Start();
            }
        }
    }
}
