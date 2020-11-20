using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacingEventDriver : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent OnFacingCameraEvent;
    public UnityEngine.Events.UnityEvent OnAwayCameraEvent;
    Transform myCameraOrigin;
    [Range(0,1)]
    public float rangeFacing;
    bool isFacingCamera;
    [SerializeField]        
    public ProjectEsky.Tracking.FollowTarget targetType;
        // Start is called before the first frame update
    void Start()
        {
            try{
                switch(targetType){
                    case ProjectEsky.Tracking.FollowTarget.HMD:
                    myCameraOrigin = ProjectEsky.Tracking.EskyHMDOrigin.instance.transform;
                    break;
                    case ProjectEsky.Tracking.FollowTarget.LeftHand:
                    myCameraOrigin = ProjectEsky.Tracking.EskyHandOrigin.instanceLeft.transform;
                    break;
                    case ProjectEsky.Tracking.FollowTarget.RightHand:
                    myCameraOrigin = ProjectEsky.Tracking.EskyHandOrigin.instanceRight.transform;
                    break;
                }
            }catch(System.NullReferenceException e){
                Debug.LogError(e);                
                Debug.LogError("There was an issue getting the target, does the relavent eskyhandorigin script exist in scene?: " + gameObject.name);
                this.enabled = false;
            }
        }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = (myCameraOrigin.position - transform.position).normalized;
        if (Vector3.Dot(transform.forward, direction) < rangeFacing)
        {

            if(!isFacingCamera){
                isFacingCamera = true;
                if(OnFacingCameraEvent != null)
                    OnFacingCameraEvent.Invoke();
            }
        }else{
            if(isFacingCamera){
                isFacingCamera = false;
                if(OnAwayCameraEvent != null)
                    OnAwayCameraEvent.Invoke();
            }
        }   
    }
     
}
