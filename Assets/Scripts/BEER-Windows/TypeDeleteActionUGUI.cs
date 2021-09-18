using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities.UI{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class TypeDeleteActionUGUI : MonoBehaviour
    {
        [SerializeField] string Target;
        [SerializeField] bool clearAll;
        // Start is called before the first frame update
        void Awake()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(TypeText);
        }

        // Update is called once per frame
        void TypeText()
        { 

            if(!clearAll)
            TypingReceiver.DeleteLetter(Target);
            else
            TypingReceiver.ClearLetters(Target);
        }
    }
}