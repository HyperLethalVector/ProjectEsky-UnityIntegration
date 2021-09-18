using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public class OpenWindowMRTKAction : MonoBehaviour
    {
        public string ManagerID;
        public string WindowID;
        // Start is called before the first frame update
        Interactable myInteractible;
        // Start is called before the first frame update
        void Start()
        {
            myInteractible = GetComponent<Interactable>();
            myInteractible.OnClick.AddListener(OnPress);
        }

        // Update is called once per frame
        void OnPress()
        {
            Debug.Log("Opening " + WindowID + " within manager ID: " + ManagerID);
            UIManager.OpenWindow(ManagerID,WindowID);
        }
    }
}