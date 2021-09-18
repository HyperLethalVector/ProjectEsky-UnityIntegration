using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class TypeActionUGUI : MonoBehaviour
    {
        [SerializeField] string Target;
        [SerializeField] char CharToType;
        // Start is called before the first frame update
        void Awake()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(TypeText);
        }

        // Update is called once per frame
        void TypeText()
        {
            TypingReceiver.TypeLetter(Target,CharToType+"");
        }
    }
}