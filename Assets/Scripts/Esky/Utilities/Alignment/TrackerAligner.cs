using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace BEERLabs.ProjectEsky.Calibrator
{
    public class TrackerAligner : MonoBehaviour
    {
        public int HookedTrackerID;
        public CoordinateAlignerStates myCurrentState;
        public bool useThumbtip = false;
        public bool useIndex = false;
        public bool useMiddle = false;
        public bool useRing = false;
        public bool usePinky = false;
        public bool usePalm = true;
        

        Vector3 initialPosition;
        Quaternion initialRotation;
        public GameObject[] startTipSpheres;
        public GameObject[] endTipSpheres;
        
        public KeyCode takeCalibrationSampleKey;
        public KeyCode solveForRelativeTransformKey;

        public BEERLabs.ProjectEsky.Tracking.EskyTracker Tracker; // for when the leap is inside HMD, and you want to align entire HMD to reference Tracker space
        public bool inverseSolve = false;
        public int minSamplePointsNeeded = 3;
        public Transform[] referenceTracker; //e.g. positions to store and compare aginst leap data each sample. MUST MATCH ORDER AND QTY OF HAND REFERNECE POINTS

        public Transform ignoreChild;
        public GameObject rightHand;

        Vector3[] referenceInitialPoints = new Vector3[5]{Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero};
        Vector3[] referenceFinalPoints = new Vector3[5]{Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero};
        public LineRenderer[] lineRenderers;
        public List<Vector3> initialPoints = new List<Vector3>();
        public List<Vector3> finalPoints = new List<Vector3>();
        public Transform LeapMotionTransform;
        static Material lineMaterial;
        static void CreateLineMaterial()
        {
            if (!lineMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                lineMaterial.SetInt("_ZWrite", 0);
            }
        }
        // Use this for initialization
        void Start() {
            initialPoints = new List<Vector3>();
            finalPoints = new List<Vector3>();
            if (Tracker == null)
            {
                Tracker = BEERLabs.ProjectEsky.Tracking.EskyTracker.instances[HookedTrackerID];
            }
            CreateLineMaterial();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        void AddAllEnabledHandReferencePoints(Vector3[] initialPoints, Vector3[] finalPoints)
        {

            if (useThumbtip)
            {
                this.initialPoints.Add(initialPoints[0]);
                this.finalPoints.Add(finalPoints[0]);                
            }
            if (useIndex)
            {
                this.initialPoints.Add(initialPoints[1]);
                this.finalPoints.Add(finalPoints[1]);
            }
            if (useMiddle)
            {
                this.initialPoints.Add(initialPoints[2]);
                this.finalPoints.Add(finalPoints[2]);
            }
            if (useRing)
            {
                this.initialPoints.Add(initialPoints[3]);
                this.finalPoints.Add(finalPoints[3]);
            }
            if (usePinky)
            {
                this.initialPoints.Add(initialPoints[4]);
                this.finalPoints.Add(finalPoints[4]);
            }
            if (usePalm)
            {
                this.initialPoints.Add(initialPoints[5]);
                this.finalPoints.Add(finalPoints[5]);
            }
        }
        void SetAllEnabledHandReferencePointsInitial()
        {
 
            if (useThumbtip)
            {
                referenceInitialPoints[0] = referenceTracker[0].position;
            }
            if (useIndex)
            {
                referenceInitialPoints[1] = referenceTracker[1].position;
            }
            if (useMiddle)
            {
                referenceInitialPoints[2] = referenceTracker[2].position;
            }
            if (useRing)
            {
                referenceInitialPoints[3] = referenceTracker[3].position;
            }
            if (usePinky)
            {
                referenceInitialPoints[4] = referenceTracker[4].position;
            }
            if (usePalm)
            {
                referenceInitialPoints[5] = referenceTracker[5].position;
            }
        }
        void SetAllEnabledHandReferencePointsFinal()
        {

            if (useThumbtip)
            {
                referenceFinalPoints[0] = referenceTracker[0].position;
            }
            if (useIndex)
            {
                referenceFinalPoints[1] = referenceTracker[1].position;
            }
            if (useMiddle)
            {
                referenceFinalPoints[2] = referenceTracker[2].position;
            }
            if (useRing)
            {
                referenceFinalPoints[3] = referenceTracker[3].position;
            }
            if (usePinky)
            {
                referenceFinalPoints[4] = referenceTracker[4].position;
            }
            if (usePalm)
            {
                referenceFinalPoints[5] = referenceTracker[5].position;
            }
        }
        void OnPostRender2()
        {
            if(myCurrentState == CoordinateAlignerStates.CapturingFinalpair){
                Debug.Log("Should be rendering the visualization");
                // Set your materials
                GL.PushMatrix();
                // yourMaterial.SetPass( );
                // Draw your stuff
                lineMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                if (useThumbtip)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[0]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[0]);                
                }
                if (useIndex)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[1]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[1]);                
                }
                if (useMiddle)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[2]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[2]);                
                }
                if (useRing)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[3]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[3]);                
                }
                if (usePinky)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[4]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[4]);                
                }
                if (usePalm)
                {
                    GL.Color(new Color(1.0f,1.0f,1.0f,1.0f));
                    GL.Vertex(referenceInitialPoints[5]);
                    GL.Color(new Color(1.0f,0.0f,0.0f,1.0f));
                    GL.Vertex(referenceFinalPoints[5]);                
                }
                GL.End();
                GL.PopMatrix();
            }
        }
        public SkinnedMeshRenderer meshToBakeFrom;
        public MeshFilter meshToBakeTo;
        
        Mesh s;
        void EnableLines(){
            Mesh s = new Mesh();
            meshToBakeFrom.BakeMesh(s);
            meshToBakeTo.mesh = s;
            meshToBakeTo.sharedMesh = s;
            rightHand.SetActive(false);
            meshToBakeTo.gameObject.SetActive(true);
            /*
            if (useThumbtip)
            {
                lineRenderers[0].gameObject.SetActive(true);
            }
            if (useIndex)
            {
                lineRenderers[1].gameObject.SetActive(true);
            }
            if (useMiddle)
            {
                lineRenderers[2].gameObject.SetActive(true);
            }
            if (useRing)
            {
                lineRenderers[3].gameObject.SetActive(true);
            }
            if (usePinky)
            {
                lineRenderers[4].gameObject.SetActive(true);
            }
            if (usePalm)
            {
                lineRenderers[5].gameObject.SetActive(true);
            }*/
        }
        void DisableLines(){
            meshToBakeTo.gameObject.SetActive(false);
                        rightHand.SetActive(true);
                        /*
            if (useThumbtip)
            {
                lineRenderers[0].gameObject.SetActive(false);
            }
            if (useIndex)
            {
                lineRenderers[1].gameObject.SetActive(false);
            }
            if (useMiddle)
            {
                lineRenderers[2].gameObject.SetActive(false);
            }
            if (useRing)
            {
                lineRenderers[3].gameObject.SetActive(false);
            }
            if (usePinky)
            {
                lineRenderers[4].gameObject.SetActive(false);
            }
            if (usePalm)
            {
                lineRenderers[5].gameObject.SetActive(false);
            }*/
        }
        void SetLineRenderers()
        {
            if(myCurrentState == CoordinateAlignerStates.CapturingFinalpair){
                // Set your materials
                if (useThumbtip)
                {
                    startTipSpheres[0].transform.position = referenceInitialPoints[0];
                    endTipSpheres[0].transform.position = referenceFinalPoints[0];

                    lineRenderers[0].SetPosition(0,referenceInitialPoints[0]);
                    lineRenderers[0].SetPosition(1,referenceFinalPoints[0]);                
                }
                if (useIndex)
                {
                    startTipSpheres[1].transform.position = referenceInitialPoints[1];
                    endTipSpheres[1].transform.position = referenceFinalPoints[1];

                    lineRenderers[1].SetPosition(0,referenceInitialPoints[1]);
                    lineRenderers[1].SetPosition(1,referenceFinalPoints[1]);                
                }
                if (useMiddle)
                {
                    startTipSpheres[2].transform.position = referenceInitialPoints[2];
                    endTipSpheres[2].transform.position = referenceFinalPoints[2];

                    lineRenderers[2].SetPosition(0,referenceInitialPoints[2]);
                    lineRenderers[2].SetPosition(1,referenceFinalPoints[2]);                
                }
                if (useRing)
                {
                    startTipSpheres[3].transform.position = referenceInitialPoints[3];
                    endTipSpheres[3].transform.position = referenceFinalPoints[3];

                    lineRenderers[3].SetPosition(0,referenceInitialPoints[3]);
                    lineRenderers[3].SetPosition(1,referenceFinalPoints[3]);                
                }
                if (usePinky)
                {
                    startTipSpheres[4].transform.position = referenceInitialPoints[4];
                    endTipSpheres[4].transform.position = referenceFinalPoints[4];

                    lineRenderers[4].SetPosition(0,referenceInitialPoints[4]);
                    lineRenderers[4].SetPosition(1,referenceFinalPoints[4]);                
                }
                if (usePalm)
                {
                    startTipSpheres[5].transform.position = referenceInitialPoints[5];
                    endTipSpheres[5].transform.position = referenceFinalPoints[5];                    

                    lineRenderers[5].SetPosition(0,referenceInitialPoints[5]);
                    lineRenderers[5].SetPosition(1,referenceFinalPoints[5]);                
                }
            }
        }
        public bool handIsVisible;
        public GameObject handRig;
        GameObject CopiedRig;
        // Update is called once per frame
        void Update()
        {
            bool handInAllFrames = rightHand.activeSelf;
            handIsVisible = handInAllFrames;
            switch(myCurrentState){
                case CoordinateAlignerStates.PostAlignment:
                    if(Input.GetKeyDown(KeyCode.R)){
                        myCurrentState = CoordinateAlignerStates.Reset;
                        break;
                    }
                    break;
                case CoordinateAlignerStates.PendingAlignment:
                    if(Input.GetKeyDown(solveForRelativeTransformKey)){
                        myCurrentState = CoordinateAlignerStates.Calculating;
                        break;
                    }
                break;
                case CoordinateAlignerStates.Idle:
                    if(handInAllFrames){
                        if(Input.GetKeyDown(takeCalibrationSampleKey)){
                            myCurrentState = CoordinateAlignerStates.CaptureInitialPair;
                        }
                    }

                break;
                case CoordinateAlignerStates.CaptureInitialPair:
                    Debug.Log("Capturing the initial pose");
                    if (handInAllFrames)
                    {
                        SetAllEnabledHandReferencePointsInitial();
                        Debug.Log("Saved Initial Pair, continuing to capture final pair");
                        myCurrentState = CoordinateAlignerStates.CapturingFinalpair;                            
                        EnableLines();
                    }else{
                        Debug.Log("Couldn't Capture initial Pose");
                        myCurrentState = CoordinateAlignerStates.Idle;
                    }

                break;
                case CoordinateAlignerStates.CapturingFinalpair:
                    if (handInAllFrames)
                    {
                        SetAllEnabledHandReferencePointsFinal();       
                    }
                    SetLineRenderers();
                    if (Input.GetKeyUp(takeCalibrationSampleKey))
                    {
                        myCurrentState = CoordinateAlignerStates.CaptureFinalPair;
                        Debug.Log("Attempting to capture final pose");
                        DisableLines();
                    }
                break;
                case CoordinateAlignerStates.CaptureFinalPair:
                    if (handInAllFrames)
                    {
                        Debug.Log("Capturing the shifted Pose");                            
                        SetAllEnabledHandReferencePointsFinal();
                        AddAllEnabledHandReferencePoints(referenceInitialPoints,referenceFinalPoints);
                        SendMessage("StoredSample",SendMessageOptions.DontRequireReceiver);
                        Debug.Log("saved new relative points, total sets:"+ initialPoints.Count + "," + finalPoints.Count);
                    }else{
                        Debug.Log("FailedToCapture");
                    }
                    myCurrentState = CoordinateAlignerStates.Idle;
                break;                
                case CoordinateAlignerStates.Calculating:
                    SolveKabschAndAlign();
                break;
                case CoordinateAlignerStates.Reset:
                    initialPoints.Clear();
                    finalPoints.Clear();
                    Debug.Log("Resetting the state");
                    myCurrentState = CoordinateAlignerStates.Idle;
                    transform.position = initialPosition;
                    transform.rotation = initialRotation;
                break;
            }            
        }

        public void SolveKabschAndAlign()
        {
            Debug.Log("Trying solve...");
            if (finalPoints.Count >= minSamplePointsNeeded)
            {
#if UNITY_EDITOR && !UNITY_ANDROID                
                    KabschSolver solver = new KabschSolver();
                    Matrix4x4 deviceToOriginDeviceMatrix;
                    if (!inverseSolve)
                    {
                        deviceToOriginDeviceMatrix =
                        solver.SolveKabsch(finalPoints, initialPoints, 200);
                    }
                    else{ 
                        deviceToOriginDeviceMatrix =
                        solver.SolveKabsch(initialPoints, finalPoints, 200);
                    }
                    //If child set, remove from hierarchy first
                    //device to origin matrix is from the LM -> the 6DOF tracker, we need to solve it 
                Matrix4x4 WorldToLM = LeapMotionTransform.worldToLocalMatrix; 
                Matrix4x4 DOFTrackerToOrigin = (WorldToLM * deviceToOriginDeviceMatrix).inverse;
                Tracker.myOffsets.LocalRigTranslation = DOFTrackerToOrigin.MultiplyPoint(Vector3.zero);
                Tracker.myOffsets.LocalRigRotation = DOFTrackerToOrigin.rotation;
                Tracker.RigCenter.localPosition = Tracker.myOffsets.LocalRigTranslation;
                Tracker.RigCenter.localRotation = Tracker.myOffsets.LocalRigRotation;
                SendMessage("StoredSample", 0, SendMessageOptions.DontRequireReceiver);
                myCurrentState = CoordinateAlignerStates.PostAlignment;                
#endif                
            }
            else { 
                Debug.Log("FAIL: Not enough samples, need at least "+ minSamplePointsNeeded.ToString()); 
                myCurrentState = CoordinateAlignerStates.Reset;
            }
        }

        private void OnDrawGizmos()
        {
            for (int i = 0; i < initialPoints.Count; i++)
            {
                if(i < finalPoints.Count){
                        Vector3 tempHandPoint = initialPoints[i];
                        Vector3 tempRefPoint = finalPoints[i];
                        Gizmos.DrawSphere(tempHandPoint, 0.01f);
                        Gizmos.DrawSphere(tempRefPoint, 0.01f);
                        Gizmos.DrawLine(tempRefPoint, tempHandPoint);
                }
            }
        }

    }
}
