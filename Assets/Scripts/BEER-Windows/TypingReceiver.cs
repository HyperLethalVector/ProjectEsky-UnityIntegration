using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public struct TypingOnTextChangedEvent{
        public string newString;
        public TypingReceiver receiver;
    }
    public class TypingReceiver : MonoBehaviour
    {
        static Dictionary<string, TypingReceiver> receivers = new Dictionary<string, TypingReceiver>();
        // Start is called before the first frame update
        [SerializeField] string ReceiverID;
        [SerializeField] TMPro.TextMeshPro tmProReceiver;
        [SerializeField] TMPro.TextMeshProUGUI uGUITextReceiver;
        [SerializeField] TMPro.TMP_InputField myInputField;
        [SerializeField] string CurrentText;
        [SerializeField] int curIndex;
        public UnityEngine.Events.UnityEvent<TypingOnTextChangedEvent> OnUpdateTextEvent;
        void Awake()
        {
            if(!receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID] = this;
            }else{

                Debug.LogError("Typing receiver ID already exists: " + ReceiverID);
            }
        }
        void OnDestroy(){
            if(receivers.ContainsKey(ReceiverID)){
                receivers.Remove(ReceiverID);
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }
        }
        public static void TypeLetter(string ReceiverID,string newChar){
            if(receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID].TypeLetter(newChar);
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }
        }
        public static void ReplaceString(string ReceiverID, string newString){
            if(receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID].SetText(newString);
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }
        }
        public static void DeleteLetter(string ReceiverID){
            if(receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID].DeleteCharacter();
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }
        }
        public static void ClearLetters(string ReceiverID){
            if(receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID].DeleteText();
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }

        }
        
        public static void SelectIndex(string ReceiverID, int newIndex){
            if(receivers.ContainsKey(ReceiverID)){
                receivers[ReceiverID].SelectIndex(newIndex);
            }else{
                Debug.LogError("Window ID Doesn't Exist: " + ReceiverID);
            }
        }
        void TypeLetter(string newChar){
            //if(CurrentText.Length == 0){
                CurrentText += newChar;
               // Debug.Log(CurrentText + "," + CurrentText.Length);
             //   curIndex = 0;    
            //}else{
         //       Debug.Log(CurrentText + "," + CurrentText.Length);                
           //     CurrentText.Insert(curIndex,newChar);
       //         curIndex = CurrentText.Length-1;
         //   }

 //           if(curIndex > CurrentText.Length-1){
   //             curIndex = CurrentText.Length-1;
     //       } 
            UpdateUI();            
            FireEvents();            
        }
        void DeleteText(){
            CurrentText = "";
            curIndex = 0;
            UpdateUI();
        }
        void SetText(string newString){
            CurrentText = newString;
            curIndex = CurrentText.Length-1;
            UpdateUI();
        }
        void SelectIndex(int newIndex){
            curIndex = newIndex;
            UpdateUI();      
            FireEvents();            
        }
        void DeleteCharacter(){
            CurrentText.Remove(CurrentText.Length-1);
            curIndex = CurrentText.Length-1;
            UpdateUI();
            FireEvents();
        }
        void UpdateUI(){
            if(tmProReceiver != null)tmProReceiver.text = CurrentText;
            if(uGUITextReceiver != null)uGUITextReceiver.text = CurrentText;
            if(myInputField != null)myInputField.SetTextWithoutNotify(CurrentText);
        }
        void FireEvents(){
            TypingOnTextChangedEvent tote = new TypingOnTextChangedEvent();
            tote.newString = CurrentText;
            tote.receiver = this;
            OnUpdateTextEvent?.Invoke(tote);
        }
    }
}