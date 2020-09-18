using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineSetter : MonoBehaviour
{
    public GameObject StartPoint;
    public GameObject EndPoint;
    public LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0,StartPoint.transform.position);
        lineRenderer.SetPosition(1,EndPoint.transform.position);
    }
}
