using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;
using OpenCvSharp;
using Leap.Unity.Interaction;

namespace Leap.Unity.AR.Testing {
  public class WebcamScreenCalibration : MonoBehaviour, IRuntimeGizmoComponent {
    public float standardDelay = 0.1f;
    public KeyCode alignScreenKey = KeyCode.Space;
    public KeyCode createReflectorMaskKey = KeyCode.LeftControl;

    //THRESHOLD FOR BLOB DETECTION
    public int blobThreshold = 10;
    public byte monitorMaskThreshold = 85;
    public byte headsetMaskThreshold = 95;
    public float BlobSizeCutoff = 50000f;

    public GameObject monitorWhiteness;
    public GameObject headsetWhiteness, whiteCircle, monitorPattern, headsetPattern;

    public Transform calibrationDotsParent;
    public Transform CalibrationMonitor;
    public Camera calibrationMonitorCamera;
    public Transform calibrationBars;

    public CalibrationDevice[] calibrationDevices;
    public bool calculateSumOfDeviation = true;
    public DeviceCalibrations deviceCalibrations;

    private List<Vector3> _realDots = new List<Vector3>(5);
    private Renderer monitorPatternRenderer, headsetPatternRenderer;
    private DenseOptimizer[] optimizers;

    [System.Serializable]
    public struct CalibrationDevice {
      public string name;
      public OpenCVStereoWebcam webcam;
      public Transform LeftCamera, RightCamera;

      public struct ImageMetrics {
        public double average;
        public double sum, numPixels;
        public float totalMaskDeviation;
      }

      [NonSerialized]
      public ImageMetrics leftImageMetrics, rightImageMetrics;
      [NonSerialized]
      public List<Vector3> triangulatedDots;
      [NonSerialized]
      public List<Vector3> rightBlobs;
      [NonSerialized]
      public int biggestRightBlobIndex;
      [NonSerialized]
      public List<Vector3> leftBlobs;
      [NonSerialized]
      public int biggestLeftBlobIndex;
      [NonSerialized]
      public int pixelsFound;
      [NonSerialized]
      public Mat[] subtractionImage, maskImage, undistortMaps;
      [NonSerialized]
      public bool undistortMapsInitialized;
      [NonSerialized]
      public bool undistortImage;
      [NonSerialized]
      public DeviceCalibrations.DeviceCalibration calibration;
      public bool isConnected {
        get {
          if (webcam != null && webcam.cap != null) {
            return webcam.cap.IsOpened() && webcam.leftImage != null;
          }
          return false;
        }
      }
      [NonSerialized]
      public int deviceID;

      public void initializeFields(int imageWidth, int imageHeight) {
        triangulatedDots = new List<Vector3>(10);
        rightBlobs = new List<Vector3>(10);
        leftBlobs = new List<Vector3>(10);
        biggestRightBlobIndex = -1;
        biggestLeftBlobIndex = -1;
        subtractionImage = new Mat[2];
        subtractionImage[0] = new Mat(imageHeight, imageWidth, MatType.CV_8UC1, 0);
        subtractionImage[1] = new Mat(imageHeight, imageWidth, MatType.CV_8UC1, 0);
        maskImage = new Mat[2];
        maskImage[0] = new Mat(imageHeight, imageWidth, MatType.CV_8UC1, 255);
        maskImage[1] = new Mat(imageHeight, imageWidth, MatType.CV_8UC1, 255);
        undistortMaps = new Mat[4];
        undistortMaps[0] = new Mat(imageHeight, imageWidth, MatType.CV_32FC1, 0);
        undistortMaps[1] = new Mat(imageHeight, imageWidth, MatType.CV_32FC1, 0);
        undistortMaps[2] = new Mat(imageHeight, imageWidth, MatType.CV_32FC1, 0);
        undistortMaps[3] = new Mat(imageHeight, imageWidth, MatType.CV_32FC1, 0);
        undistortMapsInitialized = false;
      }

      public void resetMasks() {
        int width = subtractionImage[0].Width;
        int height = subtractionImage[0].Height;
        subtractionImage[0].Release();
        subtractionImage[1].Release();
        subtractionImage[0] = new Mat(height, width, MatType.CV_8UC1, 0);
        subtractionImage[1] = new Mat(height, width, MatType.CV_8UC1, 0);
        maskImage[0].Release();
        maskImage[1].Release();
        maskImage[0] = new Mat(height, width, MatType.CV_8UC1, 255);
        maskImage[1] = new Mat(height, width, MatType.CV_8UC1, 255);
      }

      public void resetSubtraction() {
        int width = subtractionImage[0].Width;
        int height = subtractionImage[0].Height;
        subtractionImage[0].Release();
        subtractionImage[1].Release();
        subtractionImage[0] = new Mat(height, width, MatType.CV_8UC1, 0);
        subtractionImage[1] = new Mat(height, width, MatType.CV_8UC1, 0);
      }

      public void calculateUndistortMaps(DeviceCalibrations.DeviceCalibration calibration) {
        Cv2.InitUndistortRectifyMap(calibration.cameras[0].cameraMatrixMat, 
                                    calibration.cameras[0].distCoeffsMat, 
                                    calibration.cameras[0].rectificationMatrixMat, 
                                    calibration.cameras[0].newCameraMatrixMat,
          subtractionImage[0].Size(), MatType.CV_32FC1, undistortMaps[0], undistortMaps[1]);

        Cv2.InitUndistortRectifyMap(calibration.cameras[1].cameraMatrixMat,
                                    calibration.cameras[1].distCoeffsMat,
                                    calibration.cameras[1].rectificationMatrixMat,
                                    calibration.cameras[1].newCameraMatrixMat,
          subtractionImage[0].Size(), MatType.CV_32FC1, undistortMaps[2], undistortMaps[3]);
        undistortMapsInitialized = true;
        this.calibration = calibration;
      }

      public void Dispose() {
        subtractionImage[0].Release();
        subtractionImage[1].Release();
        maskImage[0].Release();
        maskImage[1].Release();
        foreach (Mat map in undistortMaps) map.Release();
      }
    }

    //On Start
    private void OnEnable() {
      optimizers = GetComponents<DenseOptimizer>();
      for (int i = 0; i < calibrationDevices.Length; i++) {
        calibrationDevices[i].initializeFields(640, 480);
      }

      for (int i = 0; i < calibrationDotsParent.childCount; i++) {
        _realDots.Add(calibrationDotsParent.GetChild(i).localPosition);
        for (int j = 0; j < calibrationDevices.Length; j++) {
          calibrationDevices[j].triangulatedDots.Add(Vector3.zero);
        }
      }

      string path = Application.dataPath + "/../cameraCalibration.json";
      if (File.Exists(path)) {
        deviceCalibrations = JsonUtility.FromJson<DeviceCalibrations>(File.ReadAllText(path));
        deviceCalibrations = deviceCalibrations.processToMat();
        Debug.Log(JsonUtility.ToJson(deviceCalibrations, true));

        for (int i = 0; i < calibrationDevices.Length; i++) {
          calibrationDevices[i].calculateUndistortMaps(deviceCalibrations.deviceCalibrations[i]);
          calibrationDevices[i].undistortImage = true; // Set true initially for verification, but then turned off later
        }

      } else {
        Debug.Log("Could not find camera calibrations at:" + path);
      }

      Vector2 monitorDimensions = new Vector2(monitorPattern.transform.localScale.x, monitorPattern.transform.localScale.y);
      if (Config.TryRead("CalibrationMonitorActiveAreaMeters", ref monitorDimensions)) {
        setScreenDimensions(monitorDimensions.x, monitorDimensions.y);
        Debug.Log("Loaded Monitor Dimensions from Config: " + monitorDimensions);
      } else {
        Config.Write("CalibrationMonitorActiveAreaMeters", monitorDimensions);
      }

      float simplexSize = 0.004f;
      if (Config.TryRead("CalibrationSimplexSize", ref simplexSize)) {
        Debug.Log("Loaded Simplex Size from Config: " + simplexSize);
        foreach (DenseOptimizer optimizer in optimizers) optimizer.simplexSize = simplexSize;
      } else {
        Config.Write("CalibrationSimplexSize", simplexSize);
      }

      float rotationToMetersRatio = 750f;
      if (Config.TryRead("CalibrationRotationRatio", ref simplexSize)) {
        Debug.Log("Loaded RotationRatio from Config: " + simplexSize);
        foreach (DenseOptimizer optimizer in optimizers) optimizer.rotationUnitRatio = rotationToMetersRatio;
      } else {
        Config.Write("CalibrationRotationRatio", rotationToMetersRatio);
      }

      if (Config.TryRead("CalibrationCalculateDeviationFromMean", ref calculateSumOfDeviation)) {
        Debug.Log("Loaded CalculateDeviationFromMean from Config: " + calculateSumOfDeviation);
      } else {
        Config.Write("CalibrationCalculateDeviationFromMean", calculateSumOfDeviation);
      }

      if (Config.TryRead("CalibrationImageDelay", ref standardDelay)) {
        Debug.Log("Loaded CalibrationImageDelay from Config: " + standardDelay);
      } else {
        Config.Write("CalibrationImageDelay", standardDelay);
      }
    }

    protected void Update() {
      HyperMegaStuff.HyperMegaLines drawer = HyperMegaStuff.HyperMegaLines.drawer;
      for (int j = 0; j < calibrationDevices.Length; j++) {
        if (!calibrationDevices[j].isConnected) continue;
        for (int i = 0; i < _realDots.Count; i++) {
          drawer.color = Color.red;
          _realDots[i] = calibrationDotsParent.GetChild(i).position;
          //drawer.DrawSphere(_realDots[i], 0.01f);
          if (calibrationDevices[j].triangulatedDots[i] != Vector3.zero) {
            drawer.color = Color.green;
            //drawer.DrawSphere(calibrationDevices[j].triangulatedDots[i], 0.01f);
            drawer.color = Color.white;
            drawer.DrawLine(_realDots[i], calibrationDevices[j].triangulatedDots[i]);
          }
        }

        Mat workingImage = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                   calibrationDevices[j].webcam.leftImage.Width,
                                   calibrationDevices[j].webcam.leftImage.Type(), 0);
        Mat workingImage2 = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                    calibrationDevices[j].webcam.leftImage.Width,
                                    calibrationDevices[j].webcam.leftImage.Type(), 0);
        for (int i = 0; i < 2; i++) {
          workingImage2.SetTo(0);
          Mat curMat = i == 0 ? calibrationDevices[j].webcam.leftImage :
                                calibrationDevices[j].webcam.rightImage;

          // Undistort the image if the calibrations are available!
          if (calibrationDevices[j].undistortMapsInitialized && calibrationDevices[j].undistortImage) {
            Cv2.Remap(curMat, workingImage2, calibrationDevices[j].undistortMaps[(i * 2)],
                                             calibrationDevices[j].undistortMaps[(i * 2) + 1]);
          } else if (calibrationDevices[j].subtractionImage[i] != null) {
            // Subtract the background from the curMat
            Cv2.Subtract(curMat, calibrationDevices[j].subtractionImage[i], workingImage);
            workingImage.CopyTo(workingImage2, calibrationDevices[j].maskImage[i]);
          }

          int optimizerIndex = ((i == 0 && j == 0) || (i == 1 && j == 1)) ? 0 : 1;
          bool isBottom = j == 1; bool isLeft = optimizerIndex == 0;
          if (optimizers[optimizerIndex].solver == null ||
              (optimizers[optimizerIndex].solver.isBottomRigel == isBottom &&
               optimizers[optimizerIndex].solver.isLeft == isLeft)) {
            calibrationDevices[j].webcam.updateScreen(workingImage2, isLeft);
          }
        }
        workingImage.Release();
        workingImage2.Release();

        calculateImageMetrics(j);
      }
    }

    void OnGUI() {
      if (GUI.Button(new UnityEngine.Rect(400, 20, 200, 30), "1) Align Monitor Transform")){
        foreach (DenseOptimizer optimizer in optimizers) optimizer.StopAllCoroutines();
        StopAllCoroutines();
        calibrateScreenLocation();
      }

      if (GUI.Button(new UnityEngine.Rect(625, 20, 200, 30), "2) Create Reflector Mask")) {
        foreach (DenseOptimizer optimizer in optimizers) optimizer.StopAllCoroutines();
        StopAllCoroutines();
        createReflectorMasks();
      }

      if (GUI.Button(new UnityEngine.Rect(850, 20, 200, 30), "3) Toggle Optimization")) {
        foreach (DenseOptimizer optimizer in optimizers) optimizer.ToggleSolve();
      }

      if (GUI.Button(new UnityEngine.Rect(1075, 20, 250, 30), "4) Adjust Ergonomics w/ Arrow Keys")) {
        foreach (DenseOptimizer optimizer in optimizers) optimizer.StopAllCoroutines();
        if (!calibrationBars.GetChild(0).gameObject.activeSelf) FindObjectOfType<IPDAdjuster>().resetEyes();
        HyperMegaStuff.HyperMegaLines.drawer.dontClear = false;
        monitorPattern.SetActive(false);
        headsetPattern.SetActive(false);
        headsetWhiteness.SetActive(false);
        whiteCircle.gameObject.SetActive(false);
        foreach (Transform bar in calibrationBars) bar.gameObject.SetActive(!bar.gameObject.activeSelf);
        FindObjectOfType<LeapXRServiceProvider>().enabled = true;
      }

      if (GUI.Button(new UnityEngine.Rect(1350, 20, 200, 30), "5) Save Calibration")) {
        FindObjectOfType<OpticalCalibrationManager>().SaveCurrentCalibration();
      }

      Vector2 sliderPosition = new Vector2(185, 0);
      Vector2 sliderScale = new Vector2(185, 20);

      // Change this until the monitor is perfectly masked when pressing space
      GUI.Label(new UnityEngine.Rect(sliderPosition, sliderScale), "MonitorMaskThreshold: " + monitorMaskThreshold);
      monitorMaskThreshold = (byte)GUI.HorizontalSlider(new UnityEngine.Rect(sliderPosition - (Vector2.right * sliderScale.x), sliderScale), monitorMaskThreshold, 0, 255);

      // Change this until the reflectors are adequately masked when masking reflectors
      sliderPosition += (Vector2.up * 25);
      GUI.Label(new UnityEngine.Rect(sliderPosition, sliderScale), "HeadsetMaskThreshold: " + headsetMaskThreshold);
      headsetMaskThreshold = (byte)GUI.HorizontalSlider(new UnityEngine.Rect(sliderPosition - (Vector2.right * sliderScale.x), sliderScale), headsetMaskThreshold, 0, 255);

      // Change this until the the blobs detect properly when locating the screen
      sliderPosition += (Vector2.up * 25);
      GUI.Label(new UnityEngine.Rect(sliderPosition, sliderScale), "BlobDetectionThreshold: " + blobThreshold);
      blobThreshold = (byte)GUI.HorizontalSlider(new UnityEngine.Rect(sliderPosition - (Vector2.right * sliderScale.x), sliderScale), headsetMaskThreshold, 0, 50);

      if (monitorPatternRenderer == null) monitorPatternRenderer = monitorPattern.GetComponent<Renderer>();
      if (headsetPatternRenderer == null) headsetPatternRenderer = headsetPattern.GetComponent<Renderer>();

      // Change these two until the screen and monitor cancel out to form the same color

      sliderPosition += (Vector2.up * 25);
      GUI.Label(new UnityEngine.Rect(sliderPosition, sliderScale), "MonitorBrightness: " + monitorPatternRenderer.material.color.r);
      float newColor = GUI.HorizontalSlider(new UnityEngine.Rect(sliderPosition - (Vector2.right * sliderScale.x), sliderScale), monitorPatternRenderer.material.color.r, 0.0f, 1.0f);
      monitorPatternRenderer.material.color = new Color(newColor, newColor, newColor);

      sliderPosition += (Vector2.up * 25);
      GUI.Label(new UnityEngine.Rect(sliderPosition, sliderScale), "HeadsetBrightness: " + headsetPatternRenderer.material.color.r);
      newColor = GUI.HorizontalSlider(new UnityEngine.Rect(sliderPosition - (Vector2.right * sliderScale.x), sliderScale), headsetPatternRenderer.material.color.r, 0.0f, 1.0f);
      headsetPatternRenderer.material.color = new Color(newColor, newColor, newColor);
    }

    void setScreenDimensions(float xDim, float yDim) {
      headsetPattern.transform.localScale = new Vector3(xDim, yDim, 1f);
      monitorPattern.transform.localScale = new Vector3(xDim, yDim, 1f);
      monitorWhiteness.transform.localScale = new Vector3(xDim, yDim, 1f);
      calibrationMonitorCamera.orthographicSize = yDim * 0.5f;
      calibrationMonitorCamera.aspect = xDim/yDim;
      calibrationMonitorCamera.GetComponent<ForceAspectRatio>().aspect = xDim / yDim;
    }

    void calibrateScreenLocation() {
      StartCoroutine("CalibrateScreenLocation");
    }

    IEnumerator CalibrateScreenLocation() {
      //Reset some stuff
      HyperMegaStuff.HyperMegaLines.drawer.dontClear = false;
      monitorPattern.SetActive(false);
      headsetPattern.SetActive(false);
      for (int i = 0; i < calibrationDevices.Length; i++) {
        if (calibrationDevices[i].isConnected) calibrationDevices[i].resetMasks();
        calibrationDevices[i].undistortImage = false; // Stop undistorting the image because it's slow
      }
      whiteCircle.gameObject.SetActive(false);

      //1) Take a background subtraction shot of a black screen
      yield return new WaitForSeconds(standardDelay);
      float startTime = Time.unscaledTime;
      while (Time.unscaledTime < startTime + standardDelay * 10f) {
        //Take the max of the current image against the current subtraction image
        updateSubtractionBackgrounds();
        yield return null;
      }
      yield return new WaitForSeconds(standardDelay);

      //2) Unhide a white object
      monitorWhiteness.SetActive(true);
      yield return new WaitForSeconds(standardDelay);

      //3) Hit "Create Mask"
      // Create the binary masks from the subtracted images
      createBinaryMasks(monitorMaskThreshold);
      yield return new WaitForSeconds(standardDelay);
      monitorWhiteness.SetActive(false);

      //4) Start Moving the Circle Around
      whiteCircle.SetActive(true);
      yield return new WaitForSeconds(standardDelay / 2f);
      HyperMegaStuff.HyperMegaLines drawer = HyperMegaStuff.HyperMegaLines.drawer;
      drawer.dontClear = true;
      for (int i = 0; i < _realDots.Count; i++) {
        whiteCircle.transform.position = calibrationDotsParent.GetChild(i).position;
        int foundDots = 0;
        while (foundDots == 0) {
          yield return new WaitForSeconds(standardDelay);
          foundDots = 0;
          for (int j = 0; j < calibrationDevices.Length; j++) {
            Vector3 triangulatedDot = triangulate(j, drawer);
            if (triangulatedDot != Vector3.zero) {
              calibrationDevices[j].triangulatedDots[i] = triangulatedDot;
              foundDots++;
            }
          }
        }
      }
      whiteCircle.SetActive(false);
      drawer.dontClear = false;

      //Kabsch the Dots, move the screen!
      KabschSolver solver = new KabschSolver();
      //Place the monitor based off of one of the webcams
      List<Vector3> _triangulatedDots = new List<Vector3>(), _monitorDots = new List<Vector3>();
      for (int j = 0; j < _realDots.Count; j++) {
        if (calibrationDevices[0].triangulatedDots[j] != Vector3.zero) {
          _monitorDots.Add(calibrationDotsParent.GetChild(j).position);
          _triangulatedDots.Add(calibrationDevices[0].triangulatedDots[j]);
        }
        if (calibrationDevices[1].triangulatedDots[j] != Vector3.zero) {
          _monitorDots.Add(calibrationDotsParent.GetChild(j).position);
          _triangulatedDots.Add(calibrationDevices[1].triangulatedDots[j]);
        }
      }

      CalibrationMonitor.Transform(solver.SolveKabsch(_monitorDots, _triangulatedDots, 200));

      for (int j = 0; j < _realDots.Count; j++) {
        _realDots[j] = calibrationDotsParent.GetChild(j).position;
      }

      monitorPattern.SetActive(true);
      headsetPattern.SetActive(true);
    }

    void createReflectorMasks() {
      StartCoroutine("CreateReflectorMasks");
    }

    IEnumerator CreateReflectorMasks() {
      monitorPattern.SetActive(false);
      headsetPattern.SetActive(false);
      monitorWhiteness.SetActive(false);
      headsetWhiteness.SetActive(false);

      //1) Take a background subtraction shot of a black screen
      yield return new WaitForSeconds(standardDelay);
      float startTime = Time.unscaledTime;
      while (Time.unscaledTime < startTime + standardDelay * 2f) {
        //Take the max of the current image against the current subtraction image
        updateSubtractionBackgrounds();
        yield return null;
      }
      yield return new WaitForSeconds(standardDelay);

      //2) Then create a mask of the monitor viewing area
      monitorWhiteness.SetActive(true);
      yield return new WaitForSeconds(standardDelay);
      createBinaryMasks(monitorMaskThreshold);
      yield return new WaitForSeconds(standardDelay);
      monitorWhiteness.SetActive(false);

      //3) AND the Monitor Mask with the Headset Reflector Mask to create a mask of the overlap!
      headsetWhiteness.SetActive(true);
      yield return new WaitForSeconds(standardDelay);
      createBinaryMasks(headsetMaskThreshold, true);
      yield return new WaitForSeconds(standardDelay);
      headsetWhiteness.SetActive(false);

      //And lastly clean everything up
      for (int i = 0; i < calibrationDevices.Length; i++) {
        if (calibrationDevices[i].isConnected) calibrationDevices[i].resetSubtraction();
      }
      monitorPattern.SetActive(true);
      headsetPattern.SetActive(true);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      //DRAW SPHERES IN EDITOR AT THE INTERSECTIONS OF THE MATCHED RAYS
      for (int j = 0; j < calibrationDevices.Length; j++) {
        if (!calibrationDevices[j].isConnected) continue;
        for (int i = 0; i < _realDots.Count; i++) {
          drawer.color = Color.red;
          _realDots[i] = calibrationDotsParent.GetChild(i).position;
          drawer.DrawSphere(_realDots[i], 0.01f);
          if (calibrationDevices[j].triangulatedDots[i] != Vector3.zero) {
            drawer.color = j == 1 ? Color.green : Color.blue;
            drawer.DrawSphere(calibrationDevices[j].triangulatedDots[i], 0.01f);
            drawer.color = Color.white;
            drawer.DrawLine(_realDots[i], calibrationDevices[j].triangulatedDots[i]);
          }
        }
      }
    }

    public static float ClosestAlphaOnSegmentToLine(Vector3 segA, Vector3 segB, Vector3 lineA, Vector3 lineB) {
      Vector3 lineBA = lineB - lineA; float lineDirSqrMag = Vector3.Dot(lineBA, lineBA);
      Vector3 inPlaneA = segA - ((Vector3.Dot(segA - lineA, lineBA) / lineDirSqrMag) * lineBA),
              inPlaneB = segB - ((Vector3.Dot(segB - lineA, lineBA) / lineDirSqrMag) * lineBA);
      Vector3 inPlaneBA = inPlaneB - inPlaneA;
      return (lineDirSqrMag != 0f && inPlaneA != inPlaneB) ? Vector3.Dot(lineA - inPlaneA, inPlaneBA) / Vector3.Dot(inPlaneBA, inPlaneBA) : 0f;
    }

    public static Vector3 RayRayIntersection(Ray rayA, Ray rayB) {
      float alpha = ClosestAlphaOnSegmentToLine(rayA.origin, rayA.origin + rayA.direction,
                                                rayB.origin, rayB.origin + rayB.direction);

      if (alpha > 0f) {
        return Vector3.LerpUnclamped(rayA.origin, rayA.origin + rayA.direction, alpha);
      } else { return Vector3.zero; }
    }

    void updateSubtractionBackgrounds() {
      for (int i = 0; i < 2; i++) {
        for (int j = 0; j < calibrationDevices.Length; j++) {
          if (calibrationDevices[j].isConnected) {
            Cv2.Max(
              calibrationDevices[j].subtractionImage[i],
              i == 0 ? calibrationDevices[j].webcam.leftImage :
                       calibrationDevices[j].webcam.rightImage,
              calibrationDevices[j].subtractionImage[i]);
          }
        }
      }
    }

    void createBinaryMasks(byte maskThreshold, bool ANDwithExistingMask = false) {
      for (int i = 0; i < 2; i++) {
        for (int j = 0; j < calibrationDevices.Length; j++) {
          if (calibrationDevices[j].isConnected) {
            Mat workingImage = new Mat(calibrationDevices[j].subtractionImage[i].Height,
                                       calibrationDevices[j].subtractionImage[i].Width,
                                       calibrationDevices[j].subtractionImage[i].Type(), 0);
            Cv2.Subtract(i == 0 ? calibrationDevices[j].webcam.leftImage :
                                  calibrationDevices[j].webcam.rightImage,
                         calibrationDevices[j].subtractionImage[i],
                         workingImage);

            if (!ANDwithExistingMask) {
              Cv2.Threshold(workingImage, calibrationDevices[j].maskImage[i],
                            maskThreshold, 255, ThresholdTypes.Binary);
            } else {
              Mat tempMask = new Mat(calibrationDevices[j].subtractionImage[i].Height,
                                     calibrationDevices[j].subtractionImage[i].Width,
                                     calibrationDevices[j].subtractionImage[i].Type(), 0);
              Cv2.Threshold(workingImage, tempMask, maskThreshold, 255, ThresholdTypes.Binary);
              Cv2.BitwiseAnd(tempMask, calibrationDevices[j].maskImage[i], calibrationDevices[j].maskImage[i]);
              tempMask.Release();
            }
            workingImage.Release();
          }
        }
      }
    }

    void OnDestroy() {
      for (int j = 0; j < calibrationDevices.Length; j++) {
        calibrationDevices[j].Dispose();
      }
    }

    Vector3 triangulate(int j, HyperMegaStuff.HyperMegaLines drawer = null) {
      Ray[] rays = new Ray[2];
      Mat workingImage = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                 calibrationDevices[j].webcam.leftImage.Width,
                                 calibrationDevices[j].webcam.leftImage.Type(), 0);
      for (int i = 0; i < 2; i++) {
        Mat curMat = i == 0 ? calibrationDevices[j].webcam.leftImage :
                              calibrationDevices[j].webcam.rightImage;

        if (calibrationDevices[j].subtractionImage[i] != null) {
          // Subtract the background from the curMat
          Cv2.Subtract(curMat, calibrationDevices[j].subtractionImage[i], workingImage);

          // Threshold the image to separate black and white
          Cv2.Threshold(workingImage, workingImage, blobThreshold, 255, ThresholdTypes.BinaryInv); // TODO MAKE THRESHOLD TUNABLE

          // Detect Blobs using the Mask
          var settings = new SimpleBlobDetector.Params();
          settings.FilterByArea = false;
          settings.FilterByColor = false;
          settings.FilterByInertia = true;
          settings.FilterByConvexity = true;
          settings.FilterByCircularity = false;
          SimpleBlobDetector detector = SimpleBlobDetector.Create();
          KeyPoint[] blobs = detector.Detect(workingImage, calibrationDevices[j].maskImage[i]);
          Cv2.DrawKeypoints(workingImage, blobs, workingImage, 255);
          int biggest = -1; float size = 0;
          for(int k = 0; k < blobs.Length; k++) {
            if(blobs[k].Size > size) {
              biggest = k;
              size = blobs[k].Size;
            }
          }

          // If there's only one blob in this image, assume it's the white circle
          if (blobs.Length > 0) {
            float[] pointArr = { blobs[biggest].Pt.X, blobs[biggest].Pt.Y };
            Mat point = new Mat(1, 1, MatType.CV_32FC2, pointArr);
            Mat undistortedPoint = new Mat(1, 1, MatType.CV_32FC2, 0);
            Cv2.UndistortPoints(point, undistortedPoint, calibrationDevices[j].calibration.cameras[i].cameraMatrixMat,
                                                         calibrationDevices[j].calibration.cameras[i].distCoeffsMat,
                                                         calibrationDevices[j].calibration.cameras[i].rectificationMatrixMat);
            Point2f[] rectilinear = new Point2f[1];
            undistortedPoint.GetArray(0, 0, rectilinear);
            Transform camera = i == 0 ? calibrationDevices[j].LeftCamera : calibrationDevices[j].RightCamera;
            rays[i] = new Ray(camera.position, camera.TransformDirection(
                             new Vector3(-rectilinear[0].X, rectilinear[0].Y, 1f)));
            if (drawer != null) {
              drawer.color = ((j == 0) != (i == 0)) ? Color.cyan : Color.red;
              drawer.DrawRay(rays[i].origin, rays[i].direction);
            }
          }
        }
      }
      workingImage.Release();

      // Only accept the triangulated point if the rays match up closely enough
      if (rays[0].origin != Vector3.zero &&
          rays[1].origin != Vector3.zero) {
        Vector3 point1 = RayRayIntersection(rays[0], rays[1]);
        Vector3 point2 = RayRayIntersection(rays[1], rays[0]);

        if (Vector3.Distance(point1, point2) < 0.005f) {
          return (point1 + point2) * 0.5f;
        } else {
          return Vector3.zero;
        }
      } else {
        return Vector3.zero;
      }
    }


    void calculateImageMetrics(int j) {
      int calculated = 0;
      // Reset Image Metrics
      calibrationDevices[j].leftImageMetrics.sum = 0;
      calibrationDevices[j].leftImageMetrics.numPixels = 0;
      calibrationDevices[j].leftImageMetrics.average = 0;
      calibrationDevices[j].leftImageMetrics.totalMaskDeviation = 0;
      calibrationDevices[j].rightImageMetrics.sum = 0;
      calibrationDevices[j].rightImageMetrics.numPixels = 0;
      calibrationDevices[j].rightImageMetrics.average = 0;
      calibrationDevices[j].rightImageMetrics.totalMaskDeviation = 0;

      Mat workingImage = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                 calibrationDevices[j].webcam.leftImage.Width,
                                 calibrationDevices[j].webcam.leftImage.Type(), 0);
      Mat workingImage2 = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                  calibrationDevices[j].webcam.leftImage.Width,
                                  MatType.CV_64FC1, 0);
      Mat workingImage3 = new Mat(calibrationDevices[j].webcam.leftImage.Height,
                                  calibrationDevices[j].webcam.leftImage.Width,
                                  MatType.CV_64FC1, 0);
      for (int i = 0; i < 2; i++) {
        workingImage.SetTo(0);
        workingImage2.SetTo(0);
        workingImage3.SetTo(0);
        Mat curMat = i == 0 ? calibrationDevices[j].webcam.leftImage :
                              calibrationDevices[j].webcam.rightImage;

        if (calibrationDevices[j].maskImage[i] != null) {
          // 1) Mask out the non-lens portions of the image
          curMat.CopyTo(workingImage, calibrationDevices[j].maskImage[i]);

          // 2) Sum the pixel values here
          double pixelSum = workingImage.Sum().Val0;

          // 3) Sum the pixel values of the mask
          double maskSum = calibrationDevices[j].maskImage[i].Sum().Val0 / 255.0;

          // 4) Calculate the average as image / mask
          double average = pixelSum / maskSum;

          workingImage2.SetTo(0);

          // 5) Convert both to signed double values
          workingImage.ConvertTo(workingImage2, MatType.CV_64FC1);

          // 6) Subtract the average from the original masked image
          Cv2.Subtract(workingImage2, average, workingImage2, calibrationDevices[j].maskImage[i]);

          // 7) Take the Absolute Value - Unnecessary, the squaring later implies an absolute value
          //Cv2.Absdiff(workingImage2, averageImage, averageImage);
          workingImage3.SetTo(0);

          // 8) Multiply the image by itself
          Cv2.AccumulateSquare(workingImage2, workingImage3, calibrationDevices[j].maskImage[i]);

          // 9) Take the Sum - this is the total mask deviation!!!
          double totalAbsDiff = Cv2.Sum(workingImage3).Val0;

          // 10) Set the Image Metrics
          //   -Make sure to compensate for the left/ right switch on the bottom
          if (i == 0) {
            calibrationDevices[j].leftImageMetrics.sum = pixelSum;
            calibrationDevices[j].leftImageMetrics.numPixels = maskSum;
            calibrationDevices[j].leftImageMetrics.average = pixelSum / maskSum;
            calibrationDevices[j].leftImageMetrics.totalMaskDeviation = (float)totalAbsDiff;
          } else {
            calibrationDevices[j].rightImageMetrics.sum = pixelSum;
            calibrationDevices[j].rightImageMetrics.numPixels = maskSum;
            calibrationDevices[j].rightImageMetrics.average = pixelSum / maskSum;
            calibrationDevices[j].rightImageMetrics.totalMaskDeviation = (float)totalAbsDiff;
          }
          calculated++;
        }
      }
      /*if (calculated == 2) {
        Debug.Log("Left Cost: " + calibrationDevices[j].leftImageMetrics.totalMaskDeviation + "\n" +
                  "RightCost: " + calibrationDevices[j].rightImageMetrics.totalMaskDeviation);
      }*/
      workingImage.Release();
      workingImage2.Release();
      workingImage3.Release();
    }

    [Serializable]
    public struct DeviceCalibrations {
      public bool matsProcessed;
      [Serializable]
      public struct DeviceCalibration {
        public int source;
        public string date;
        public float baseline;
        public List<CameraCalibration> cameras;

        [Serializable]
        public struct CameraCalibration {
          public List<float> distCoeffs;
          public List<float> cameraMatrix;
          public List<float> rectificationMatrix;
          public List<float> newCameraMatrix;

          [NonSerialized]
          public Mat distCoeffsMat, cameraMatrixMat, 
                     rectificationMatrixMat, newCameraMatrixMat;

          public CameraCalibration processToMat() {
            distCoeffsMat =          new Mat(8, 1, MatType.CV_32FC1, distCoeffs.ToArray());
            cameraMatrixMat =        new Mat(3, 3, MatType.CV_32FC1, cameraMatrix.ToArray());
            rectificationMatrixMat = new Mat(3, 3, MatType.CV_32FC1, rectificationMatrix.ToArray());
            newCameraMatrixMat =     new Mat(3, 4, MatType.CV_32FC1, newCameraMatrix.ToArray());
            return this;
          }

          public void Dispose() {
            distCoeffsMat.Release();
            cameraMatrixMat.Release();
            rectificationMatrixMat.Release();
            newCameraMatrixMat.Release();
          }
        }

        public DeviceCalibration(bool dummy = true) {
          source = 0;
          date = "1/17/2019; 2:24PM";
          baseline = 0.059f;
          cameras = new List<CameraCalibration>();
          for(int i = 0; i < 2; i++) {
            CameraCalibration calib = new CameraCalibration();
            calib.distCoeffs = new List<float>();
            calib.cameraMatrix = new List<float>();
            calib.rectificationMatrix = new List<float>();
            calib.newCameraMatrix = new List<float>();

            for (int j = 0; j < 12; j++) {
              if (j < 8) calib.distCoeffs.Add(j);
              if (j < 9) calib.rectificationMatrix.Add(j);
              if (j < 9) calib.cameraMatrix.Add(j);
              calib.newCameraMatrix.Add(j);
            }
            cameras.Add(calib);
          }
        }
        public DeviceCalibration processToMat() {
          for(int i = 0; i < cameras.Count; i++) {
            cameras[i] = cameras[i].processToMat();
          }
          return this;
        }
        public void Dispose() {
          for (int i = 0; i < cameras.Count; i++) {
            cameras[i].Dispose();
          }
        }
      }
      public List<DeviceCalibration> deviceCalibrations;
      public DeviceCalibrations(bool dummy = true) {
        deviceCalibrations = new List<DeviceCalibration>();
        for (int i = 0; i < 2; i++) {
          deviceCalibrations.Add(new DeviceCalibration(true));
        }
        matsProcessed = false;
      }
      public DeviceCalibrations processToMat() {
        for (int i = 0; i < deviceCalibrations.Count; i++) {
          deviceCalibrations[i].processToMat();
        }
        matsProcessed = true;
        return this;
      }
      public void Dispose() {
        for (int i = 0; i < deviceCalibrations.Count; i++) {
          deviceCalibrations[i].Dispose();
        }
      }
    }
  }
}
