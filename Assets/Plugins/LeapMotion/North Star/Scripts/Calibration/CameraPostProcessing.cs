using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;
using Leap;
using UnityEngine.Serialization;

public class CameraPostProcessing : MonoBehaviour {
  
  //public LeapImageRetriever retriever;
  //THRESHOLD FOR BLOB DETECTION
  public Vector2Int brightnessThreshold = new Vector2Int(50, 75);
  public byte maskThreshold = 90;

  [QuickButton("Create Mask", "createMask")]
  public GameObject whiteness;

  [QuickButton("Acquire White Dots", "acquireWhiteDots")]
  public GameObject whiteDots;

  [QuickButton("Prepare For Black Dots", "acquireBlackDots")]
  public bool invertImage = false;
  //DIAGNOSE TRACKING
  [QuickButton("Save Black Dots", "saveBlackDots")]
  public bool DrawDebug = true;
  public float maxMatchDistance = 0.1f;

  //int rcount = 0, lcount = 0;
  [NonSerialized]
  public List<Vector3> RightBlobs = new List<Vector3>();
  [NonSerialized]
  public int biggestRightBlobIndex = 0;
  [NonSerialized]
  public List<Vector3> LeftBlobs = new List<Vector3>();
  [NonSerialized]
  public int biggestLeftBlobIndex = 0;
  int PixelLimit = 4000;
  int PixelsFound = 0;
  public float BlobSizeCutoff = 500000f;
  int Width = 384;
  int Height = 384;

  bool runOnce = false, runMaskProcess = false;
  byte[] imageData, subtractionImage, maskImage;

  public Image combinedImage;
  public ManualCalibrationPlacement calib;

  [Serializable]
  public struct BlackDots {
    public List<Vector3> BlackRightBlobs;
    public int biggestBlackRightBlobIndex;
    public List<Vector3> BlackLeftBlobs;
    public int biggestBlackLeftBlobIndex;
  }
  [NonSerialized]
  public BlackDots blackDots = new BlackDots();
  [QuickButton("Sort Black Dots", "sortBlackDotsByDistance")]
  public string blackDotsData;

  private void Start() {
    //DontDestroyOnLoad(this.gameObject);
    if(blackDotsData.Length > 0) {
      blackDots = JsonUtility.FromJson<BlackDots>(blackDotsData);
    }
  }

  private void Update() {
    runOnce = false;
  }

  void createMask() {
    StartCoroutine("CreateMaskRoutine");
  }
  void acquireBlackDots() {
    StartCoroutine("AcquireBlackDots");
  }
  void acquireWhiteDots() {
    StartCoroutine("AcquireWhiteDots");
  }
  void saveBlackDots() {
    blackDots.BlackRightBlobs = new List<Vector3>(RightBlobs);//RightBlobs.ConvertAll(v => v);
    blackDots.BlackLeftBlobs = new List<Vector3>(LeftBlobs);//LeftBlobs.ConvertAll(v => v);
    blackDots.biggestBlackLeftBlobIndex = biggestLeftBlobIndex;
    blackDots.biggestBlackRightBlobIndex = biggestRightBlobIndex;

    sortBlackDotsByDistance();
  }

  void sortBlackDotsByDistance() {
    //Sort Lists by distance to central dot
    Vector3 BiggestLeft = blackDots.BlackLeftBlobs[blackDots.biggestBlackLeftBlobIndex] / blackDots.BlackLeftBlobs[blackDots.biggestBlackLeftBlobIndex].z;
    Vector3 BiggestRight = blackDots.BlackRightBlobs[blackDots.biggestBlackRightBlobIndex] / blackDots.BlackRightBlobs[blackDots.biggestBlackRightBlobIndex].z;
    blackDots.BlackLeftBlobs.Sort((x, y) => ((BiggestLeft - (x / x.z)).sqrMagnitude).CompareTo(((BiggestLeft - (y / y.z)).sqrMagnitude)));
    blackDots.BlackRightBlobs.Sort((x, y) => ((BiggestRight - (x / x.z)).sqrMagnitude).CompareTo(((BiggestRight - (y / y.z)).sqrMagnitude)));

    blackDots.biggestBlackLeftBlobIndex = 0;
    blackDots.biggestBlackRightBlobIndex = 0;
    blackDotsData = JsonUtility.ToJson(blackDots);
    Debug.Log(blackDotsData);
  }

  bool captureBGSubtraction;
  IEnumerator CreateMaskRoutine() {
    //Reset some stuff
    whiteDots.SetActive(false);
    brightnessThreshold = new Vector2Int(50, 75);
    invertImage = false;
    subtractionImage = new byte[subtractionImage.Length];
    maskImage = new byte[subtractionImage.Length];

    //1) Take a background subtraction shot of a white screen
    yield return new WaitForSeconds(0.1f);
    captureBGSubtraction = true;
    yield return new WaitForSeconds(0.1f);
    captureBGSubtraction = false;
    yield return new WaitForSeconds(0.1f);

    //2) Unhide a white object
    whiteness.SetActive(true);
    yield return new WaitForSeconds(0.1f);

    //3) Hit "Create Mask"
    runMaskProcess = true;
    yield return new WaitForSeconds(0.1f);
    whiteness.SetActive(false);
  }

  //4) Hit the "Invert Image" Bool
  IEnumerator AcquireBlackDots() {
    whiteDots.SetActive(false);
    invertImage = true;

    //5) Take a new background subtraction with the folder in place
    for (int i = 0; i < subtractionImage.Length; i++) {
      subtractionImage[i] = 0;
    }
    captureBGSubtraction = true;
    yield return new WaitForSeconds(0.5f);
    captureBGSubtraction = false;
    yield return new WaitForSeconds(0.25f);

    //6) Remove the folder

    //7) Adjust the point detection thresholds to work with this super low SNR
    brightnessThreshold = new Vector2Int(2, 4);

    print("Remove Folder and Adjust");
  }

  IEnumerator AcquireWhiteDots() {
    //Reset some stuff
    whiteDots.SetActive(false);
    brightnessThreshold = new Vector2Int(50, 75);
    invertImage = false;
    subtractionImage = new byte[subtractionImage.Length];

    //1) Take a background subtraction shot of a white screen
    yield return new WaitForSeconds(0.1f);
    captureBGSubtraction = true;
    yield return new WaitForSeconds(2f);
    captureBGSubtraction = false;
    yield return new WaitForSeconds(0.1f);

    //2) Unhide a white object
    whiteDots.SetActive(true);
    yield return new WaitForSeconds(0.1f);
  }

  public void UpdateImage(Image image, byte[] imageData) {
    if (!runOnce) {
      runOnce = true;
      Width = image.Width;
      Height = image.Height;

      combinedImage = image;

      PixelsFound = 0;
      RightBlobs.Clear();
      LeftBlobs.Clear();

      //Handle Subtraction Image Stuff
      if (subtractionImage == null || subtractionImage.Length != imageData.Length) {
        subtractionImage = new byte[imageData.Length]; maskImage = new byte[imageData.Length];
      }

      //Reset Subtraction Image
      if (Input.GetKeyDown(KeyCode.Space)) {
        Array.Copy(imageData, 0, subtractionImage, 0, subtractionImage.Length);
      }
      for (int i = 0; i < imageData.Length; i++) {
        //Invert Image if trying to capture black dots
        if (invertImage) {
          imageData[i] = (byte)(255 - imageData[i]);
        }
        //Take the max of the current image against the current subtraction image
        if (Input.GetKey(KeyCode.Space) || captureBGSubtraction) {
          subtractionImage[i] = Math.Max(subtractionImage[i], imageData[i]);
        }

        if (runMaskProcess) {
          maskImage[i] = (byte)((imageData[i] - subtractionImage[i] > maskThreshold) ? 0 : 255);
          subtractionImage[i] = 0;
        }

        //Apply the subtraction image to the current image
        imageData[i] = (byte)(maskImage[i] == 0 ? Mathf.Clamp(imageData[i] - subtractionImage[i], 0, 255) : 0f);

        /*Vector2 coord = new Vector2Int(i % Width, i / Width);
        Vector2 cursor = ManualCalibrationPlacement.localSpaceToPixel(calib.selectionCursor.localPosition, image);
        imageData[i] = (byte)(Vector2.SqrMagnitude(coord - cursor) < 15f ? 255 : imageData[i]);*/
      }

      //Find blobs
      if (!captureBGSubtraction && !runMaskProcess) {
        biggestLeftBlobIndex = FindBiggestBlob(LeftBlobs, 0, Width * Height, imageData);
        biggestRightBlobIndex = FindBiggestBlob(RightBlobs, Width * Height, Width * Height, imageData);
      }
      runMaskProcess = false;
    }
  }

  int FindBiggestBlob(List<Vector3> blobs, int start, int length, byte[] image) {
    int biggest = -1;
    for (int i = start; i < start+length; i++) {
      if ((PixelsFound < PixelLimit) && (image[i] > brightnessThreshold.y)) {
        Vector3 Blob = BlobFind(i, image, 0, 0);
        if (Blob.z > 3) {
          blobs.Add(Blob/* / Blob.z*/);
          if (biggest == -1 || Blob.z > blobs[biggest].z) {
            biggest = blobs.Count - 1;
          }
        }
      }
    }
    return biggest;
  }


  //DRAW SPHERES IN EDITOR AT THE INTERSECTIONS OF THE MATCHED RAYS
  void OnDrawGizmos() {
    if (DrawDebug && LeftBlobs != null && RightBlobs != null) {
      DrawBlobs(LeftBlobs, Color.cyan, 0.0f, biggestLeftBlobIndex);
      DrawBlobs(RightBlobs, Color.red, 0.0f, biggestRightBlobIndex);

      if (blackDots.BlackLeftBlobs != null && blackDots.BlackRightBlobs != null) {
        DrawBlobs(blackDots.BlackLeftBlobs, Color.cyan, 0f, blackDots.biggestBlackLeftBlobIndex);
        DrawBlobs(blackDots.BlackRightBlobs, Color.red, 0f, blackDots.biggestBlackRightBlobIndex);

        if (biggestLeftBlobIndex != 0 || biggestLeftBlobIndex != -1) {
          DrawConnections(blackDots.BlackLeftBlobs, LeftBlobs, biggestLeftBlobIndex, 0f);
        }
        if (biggestRightBlobIndex != 0 || biggestRightBlobIndex != -1) {
          DrawConnections(blackDots.BlackRightBlobs, RightBlobs, biggestRightBlobIndex, 0f);
        }
      }
    }
  }

  void DrawBlobs(List<Vector3> blobs, Color color, float offset, int biggest) {
    Gizmos.color = color;
    for (int i = 0; i < blobs.Count; i++) {
      Gizmos.DrawSphere(blobToWorld(blobs[i], offset), biggest == i ? 0.003f : 0.001f);
    }
  }

  void DrawConnections(List<Vector3> blackBlobs, List<Vector3> whiteBlobs, int biggestWhite, float blackOffset) {
    if(blackBlobs.Count <= 1 || whiteBlobs.Count <= 1 || biggestWhite <= 0) { return; }
    Gizmos.color = Color.white;
    List<Vector3> tempBlack = new List<Vector3>(blackBlobs), tempWhite = new List<Vector3>(whiteBlobs);

    //Black to world
    for (int i = 0; i < tempBlack.Count; i++) {
      tempBlack[i] = blobToWorld(tempBlack[i], 0f);
    }

    //White to translated world
    Vector3 biggestOffset = tempBlack[0] - blobToWorld(tempWhite[biggestWhite], 0f);
    for (int i = 0; i < tempWhite.Count; i++) {
      Vector3 worldWhite = blobToWorld(tempWhite[i], 0f);
      tempWhite[i] = worldWhite + biggestOffset;
    }

    float error = 0f; int numMatches = 0;
    //Draw Matches between Black and White (note, the Match Vector2's are invalid right now)
    //(need to sort/iterate through both lists backwards I think to keep them valid?)
    for (int i = 0; i < tempBlack.Count; i++) {
      Vector2Int match = new Vector2Int(i, -1);
      float minDistance = maxMatchDistance;
      for (int j = 0; j < tempWhite.Count; j++) {
        float curSqrDist = (tempBlack[i] - tempWhite[j]).magnitude;
        if (curSqrDist < minDistance) {
          minDistance = curSqrDist;
          match.y = j;
        }
      }

      if (match.y != -1) {
        Gizmos.DrawLine(tempBlack[match.x] + (Vector3.forward * blackOffset), tempWhite[match.y] - biggestOffset);
        error += (tempBlack[match.x] - tempWhite[match.y]).sqrMagnitude;
        numMatches++;
        tempWhite.RemoveAt(match.y);
      }
    }
    //Debug.Log("Matching Error: " + (error / numMatches));
  }

  Vector3 blobToWorld(Vector3 blob, float offset) {
    blob = blob / blob.z;
    blob = (blob / Width) - (Vector3.one * 0.5f);
    blob.y /= 2f;
    blob.y -= 0.25f;
    blob.z = offset;
    return transform.TransformPoint(blob);
  }

  //RECURSIVE FOUR-CONNECTED FLOOD-FILL; WEIGHTS EXPOENENTIALLY BY BRIGHTNESS
  Vector3 BlobFind(int Start, byte[] image, int Stack, int Cam) {
    if (PixelsFound < PixelLimit) {
      //ADD THE WEIGHTED RAY DIRECTION TO THE TOTAL
      //Vector Dir = frame.Images[Cam].Rectify(new Vector(Start%Width,Start/Width,0f));
      Vector Dir = new Vector((float)(Start % Width), (float)(Start / Width), 0f);
      //float Weight = Mathf.Pow(((float)image[Start] - (float)brightnessThreshold.x), 2) + 1f;
      float Weight = 1f;
      Vector2 Pos = new Vector2(Dir.x, Dir.y) * Weight;
      float Sum = Weight;

      //MARK THIS PIXEL AS DONE SO WE DON'T REDO IT
      PixelsFound++;

      //ASSIMILATE THE SURROUNDING PIXELS
      if (Stack < 5000) {//PREVENTS STACK OVERFLOW
        if ((Mathf.Floor(Start % Width) != 0) && (image[Start - 1] > brightnessThreshold.x)) {
          image[Start - 1] = 0;
          Vector3 nPos = BlobFind(Start - 1, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start % Width) != Width - 1) && (image[Start + 1] > brightnessThreshold.x)) {
          image[Start + 1] = 0;
          Vector3 nPos = BlobFind(Start + 1, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start / Width) != 0) && (image[Start - Width] > brightnessThreshold.x)) {
          image[Start - Width] = 0;
          Vector3 nPos = BlobFind(Start - Width, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start / Width) != Height - 1) && (image[Start + Width] > brightnessThreshold.x)) {
          image[Start + Width] = 0;
          Vector3 nPos = BlobFind(Start + Width, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
      }

      //COME HOME
      return new Vector3(Pos.x, Pos.y, Sum);
    } else {
      //SEND BACK THEIR HEAD IN A BOX
      return new Vector3(1f, 1f, 1f);
    }
  }
}
