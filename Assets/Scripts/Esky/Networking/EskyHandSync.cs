using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectEsky.Networking{
   [System.Serializable]

    public class NetworkFinger{
        public Transform Knuckle;
        public Transform UpperJoint;
        public Transform MiddleJoint;
        public Transform Tip;
       

        public void UpdateFinger(FingerGraph fingerGraph, SceneGraphPoseSyncType syncType, float modifierMovement, float modifierRotation){
            fingerGraph.Knuckle.UpdateTransform(Knuckle,syncType,modifierMovement,modifierRotation);
            fingerGraph.UpperJoint.UpdateTransform(UpperJoint,syncType,modifierMovement,modifierRotation);
            fingerGraph.MiddleJoint.UpdateTransform(MiddleJoint,syncType,modifierMovement,modifierRotation);
            fingerGraph.Tip.UpdateTransform(Tip,syncType,modifierMovement,modifierRotation);
        }
        public void UpdateFinger(ref FingerGraph fg){
            
            fg.Knuckle.localPosition = Knuckle.localPosition;
            fg.Knuckle.localRotation = Knuckle.localRotation;
            fg.UpperJoint.localPosition = UpperJoint.localPosition;
            fg.UpperJoint.localRotation = UpperJoint.localRotation;
            fg.MiddleJoint.localPosition = MiddleJoint.localPosition;
            fg.MiddleJoint.localRotation = MiddleJoint.localRotation;
            fg.Tip.localPosition = Tip.localPosition;
            fg.Tip.localRotation = Tip.localRotation;

        }
    }
    [System.Serializable]
    public class NetworkHand{
        public NetworkFinger IndexFinger;
        public NetworkFinger MiddleFinger;
        public NetworkFinger RingFinger;
        public NetworkFinger PinkyFinger;
        public NetworkFinger Thumb;                                
        public Transform Palm;
        public void UpdateHand(HandGraph handGraph, SceneGraphPoseSyncType syncType, float speedModifier, float rotationModifier){
            IndexFinger.UpdateFinger(handGraph.IndexFinger,syncType,speedModifier,rotationModifier);
            MiddleFinger.UpdateFinger(handGraph.MiddleFinger,syncType,speedModifier,rotationModifier);
            RingFinger.UpdateFinger(handGraph.RingFinger,syncType,speedModifier,rotationModifier);
            PinkyFinger.UpdateFinger(handGraph.PinkyFinger,syncType,speedModifier,rotationModifier);
            Thumb.UpdateFinger(handGraph.Thumb,syncType,speedModifier,rotationModifier);

            handGraph.Palm.UpdateTransform(Palm,syncType,speedModifier,rotationModifier);
        }        
        public void UpdateHandGraph(ref HandGraph handGraph){
            handGraph.Palm.localPosition = Palm.localPosition;
            handGraph.Palm.localRotation = Palm.localRotation;

            IndexFinger.UpdateFinger(ref handGraph.IndexFinger);
            MiddleFinger.UpdateFinger(ref handGraph.MiddleFinger);
            RingFinger.UpdateFinger(ref handGraph.RingFinger);
            PinkyFinger.UpdateFinger(ref handGraph.PinkyFinger);
            Thumb.UpdateFinger(ref handGraph.Thumb);
        }
    }
    public class EskyHandSync : MonoBehaviour
    {
        
        public int HandID;

        [SerializeField] 
        public NetworkHand NetworkHandInfo;

        public SceneGraphPoseSyncType SynchronizeType;

        public float TranslationSmoothModifier;

        public float RotationSmoothModifier;

        public EskySceneGraphSync mySceneGraph;

        bool isNetworkSync = false;
        // Start is called before the first frame update
        public void Start(){
            if(mySceneGraph != null)
            mySceneGraph.AddHandIDToList(HandID,this);
        }
        public void Update(){
            if(mySceneGraph == null){
                if(EskyClient.myClient != null){
                    mySceneGraph = EskyClient.myClient.GetComponent<EskySceneGraphSync>();
                    mySceneGraph.AddHandIDToList(HandID,this);
                }
            }else{
                if(isNetworkSync){
                    NetworkHandInfo.UpdateHand(myHandGraph,SynchronizeType,TranslationSmoothModifier,RotationSmoothModifier);                    
                }
            }
        }
        [HideInInspector]
        public HandGraph myHandGraph = new HandGraph();
        // Update is called once per frame        
        public void UpdateHand(HandGraph info){
            myHandGraph = info;
            isNetworkSync = true;
            GetComponent<Leap.Unity.RiggedHand>().enabled = false;
        }
        public HandGraph GetHandInfo(){
            HandGraph handGraph = new HandGraph();
            NetworkHandInfo.UpdateHandGraph(ref handGraph);
            return handGraph;
        }
    }
}