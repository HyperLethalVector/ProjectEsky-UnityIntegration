using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeOtherInterestingScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void ReceiveManipulateEvent(){
        Debug.Log("Received Event");
    }
    public void UnReceiveManipulateEvent(){
        Debug.Log("UnreceivedEvent/Event Stopped");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
