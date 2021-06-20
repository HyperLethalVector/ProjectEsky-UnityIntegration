using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    public class EskyAnchorContent : MonoBehaviour
    {
        public string ContentID;
        public int AnchorIDToSubscribe;
        // Start is called before the first frame update
        void Start()
        {
            EskyAnchor.Subscribe(AnchorIDToSubscribe, this);

        }

        // Update is called once per frame
        void Update()
        {
            
        }
        public void OnLocalizedCallback(){
            //You can do fun stuff after localizing here
            Debug.Log(ContentID+ " was placed back in the scene!");
        }
        public void AfterSubscribeCallback(){

        }
    }
}