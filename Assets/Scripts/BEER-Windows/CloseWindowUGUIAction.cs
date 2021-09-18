using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    public class CloseWindowUGUIAction : MonoBehaviour
    {
        public string ManagerID;
        // Start is called before the first frame update
        UnityEngine.UI.Button myInteractible;
        // Start is called before the first frame update
        void Start()
        {
            myInteractible = GetComponent<UnityEngine.UI.Button>();
            myInteractible.onClick.AddListener(OnPress);
        }

        // Update is called once per frame
        void OnPress()
        {
            UIManager.CloseCurrentWindow(ManagerID);
        }
    }
}