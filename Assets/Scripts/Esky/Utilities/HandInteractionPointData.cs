using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class HandInteractionPointData : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void InteractHandStart(HandPanEventData data){
        Debug.Log("Started");
    }
    public void InteractHandStop(HandPanEventData data){
        Debug.Log("Stopped");
    }
}
