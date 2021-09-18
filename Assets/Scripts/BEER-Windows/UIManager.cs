using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public class UIManager : MonoBehaviour
    {
        static Dictionary<string,UIManager> _instances = new Dictionary<string, UIManager>();
        [SerializeField] string ManagerID;
        Dictionary<string,VirtualWindow> windows =  new Dictionary<string, VirtualWindow>();
        [SerializeField] Stack<VirtualWindow> stack = new Stack<VirtualWindow>();
        [SerializeField] VirtualWindow currentWindow;
        [SerializeField] VirtualWindow mainMenu;
        void Awake(){
            if(_instances.ContainsKey(ManagerID)){
                Debug.Log("Manager for ID already exists, destroying: " + ManagerID);
                DestroyImmediate(this.gameObject);
                return;
            }
            _instances.Add(ManagerID,this);
        }
        public void Start(){
            OpenMainMenu();
        }
        public static void OpenWindow(string ManagerID, string WindowID){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].OpenWindow(WindowID);
            }else{
                Debug.LogError("Manager ID Doesn't Exist: ");
            }
        }
        public static void CloseCurrentWindow(string ManagerID){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].CloseCurrentWindow();
            }else{
                Debug.LogError("Manager ID Doesn't Exist: " + ManagerID);
            }
        }        
        public static void CloseAllWindows(string ManagerID){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].CloseAllWindows();
            }else{
                Debug.LogError("Manager ID Doesn't Exist: " + ManagerID);
            }
        }   
        public static void OpenMainMenu(string ManagerID){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].OpenMainMenu();
            }else{
                Debug.LogError("Manager ID Doesn't Exist: " + ManagerID);
            }
        } 
        public static void SubscribeWindow(string ManagerID,string WindowID, VirtualWindow windowToSubscribe){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].windows[WindowID] = windowToSubscribe;
            }else{
                Debug.LogError("Manager ID Doesn't Exist: " + ManagerID);
            }
        }
        public static void RemoveWindow(string ManagerID,string WindowID){
            if(_instances.ContainsKey(ManagerID)){
                _instances[ManagerID].windows.Remove(WindowID);
            }else{
                Debug.LogError("Manager ID Doesn't Exist: " + ManagerID);
            }
        }
        void OpenWindow(string ID){
            Debug.Log("Opening window: " + ID);
            VirtualWindow window = null;
            if(windows.ContainsKey(ID)){
                Debug.Log("Finding ID");
                window = windows[ID];
            }
            if(window != null){
                if(currentWindow != null){
                    stack.Push(currentWindow);
                    currentWindow.Close();
                }
                Debug.Log("assigning current window: " + window.GetWindowID());
                currentWindow = null;
                currentWindow = window;
                window.Open();
            }            
        }
        void CloseCurrentWindow(){
            if(currentWindow !=  null){
                currentWindow.Close();         
                if(stack.Count > 0){
                    currentWindow =  stack.Pop();
                    currentWindow.Open(); 
                }else{
                    currentWindow = null;
                    Debug.Log("Closed all windows");
                }
            }
        }
        public void OpenMainMenu(){
            Debug.Log("Closing all windows and opening main menu");
            CloseAllWindows();
            if(mainMenu != null)
            OpenWindow(mainMenu.GetWindowID());
        }
        void CloseAllWindows(){
            while(stack.Count > 0){
                VirtualWindow w = stack.Pop();
                w.Close();
            }
        }
    }
}