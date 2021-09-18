using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public class CloseWindowMRTKAction : MonoBehaviour
    {
        public string ManagerID;
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
            UIManager.CloseCurrentWindow(ManagerID);
        }
    }
}