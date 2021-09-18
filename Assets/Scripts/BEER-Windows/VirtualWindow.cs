using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public class VirtualWindow : MonoBehaviour
    {
        [SerializeField] string ManagerID;
        [SerializeField] string WindowID;
        [SerializeField] UnityEngine.Events.UnityEvent onCloseActions;
        [SerializeField] UnityEngine.Events.UnityEvent onOpenActions;

        [SerializeField] Animator myAnimator;
        [SerializeField] bool DisablesGameObject;
        public string GetManagerID(){

            return ManagerID;
        }
        public string GetWindowID(){
            return WindowID;
        }
        // Start is called before the first frame update
        protected virtual void Awake()
        {
            Close();
            UIManager.SubscribeWindow(ManagerID,WindowID,this);
        }
        void OnDestroy(){
            UIManager.RemoveWindow(ManagerID,WindowID); 
        }
        public virtual void Open(){
            Debug.Log("Opening window ID: " + WindowID);
            try{
                onOpenActions?.Invoke();
            }catch(System.Exception e){
                Debug.LogError(e);
            }
            if(myAnimator != null){
                myAnimator.SetTrigger("Open");
            }
            if(DisablesGameObject)
            gameObject.SetActive(true);
            
        }
        public virtual void Close(){
            Debug.Log("Closing window ID: " + WindowID);            
            try{
            onCloseActions?.Invoke();
            }catch(System.Exception e){
                Debug.LogError(e);
            }

            if(myAnimator != null){
                myAnimator.SetTrigger("Close");
            }else{
                FinishCloseAnim();
            }
        }
        public void FinishCloseAnim(){
            if(DisablesGameObject)
            gameObject.SetActive(false);
        }
    }
}