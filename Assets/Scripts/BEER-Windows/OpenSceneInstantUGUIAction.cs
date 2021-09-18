using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Utilities{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class OpenSceneInstantUGUIAction : MonoBehaviour
    {
        public string SceneToLoad;
        // Start is called before the first frame update
        void Start()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OpenScene);
        }

        // Update is called once per frame
        void OpenScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneToLoad,UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}