using UnityEngine;
using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using Leap;


public class CameraProcessing : MonoBehaviour {
  /*
  public Leap.Unity.LeapServiceProvider leap_provider;
  Controller leap_controller_;

  //LIST OF ALL TRACKED 3D POINTS
  public List<Vector3> Blobs = new List<Vector3>();
  //THRESHOLD FOR BLOB DETECTION
  public byte brightnessThreshold = 136;
  //DIAGNOSE TRACKING
  public bool DrawDebug;

  int rcount = 0;
  int lcount = 0;
  float[] RBlobs = new float[765];
  float[] LBlobs = new float[765];
  int[] MatchBlobs = new int[255];
  public Vector3 CameraOffset = new Vector3(0.02f, 0f, 0f);
  int PixelLimit = 1000;
  int PixelsFound = 0;
  public float BlobSizeCutoff = 500000f;

  byte[] r_image_data;
  byte[] l_image_data;
  int Width = 640;
  int Height = 240;
  protected long ImageTimeout = 9000; //microseconds

  void Start() {
    //SET UP CONTROLLER
    leap_controller_ = leap_provider.GetLeapController();
    //TELL IT WE'RE ASKING FOR IMAGES
    //leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);
    //SET BYTE ARRAYS
    SetDimensions(Width, Height);
  }

  void Update() {
    //RESET EVERYTHING FOR THIS FRAME
    Blobs.Clear();
    PixelsFound = 0;
    for (int i = 0; i < 765; i++) {
      RBlobs[i] = 0;
    }
    for (int i = 0; i < 765; i++) {
      LBlobs[i] = 0;
    }
    for (int i = 0; i < 255; i++) {
      MatchBlobs[i] = 256;
    }

    //GET THE LEAP
    Image _requestedImage = leap_controller_.RequestImages(leap_provider.CurrentFrame.Id, Image.ImageType.DEFAULT);
    /*
    //IF THERE ARE NO IMAGES TO ANALYZE, CALL IT A DAY
    if (!_requestedImage.IsValid) {
      Debug.Log("No Images.");
      return;
    }
    */
    /*
    //IF THERE IS NO DATA **IN** THE IMAGES TO ANALYZE, CALL IT A DAY
    if (_requestedImage.Width == 0 || _requestedImage.Height == 0) {
      Debug.Log("No data in the images.");
      return;
    }

    //IF IMAGE DIMENSIONS HAVE CHANGED, CHANGE BYTE ARRAY SIZE
    if (_requestedImage.Width != Width || _requestedImage.Height != Height) {
      Width = _requestedImage.Width;
      Height = _requestedImage.Height;
      SetDimensions(Width, Height);
    }

    //PUT IMAGES INTO BYTE ARRAYS
    long start = leap_controller_.Now();
    while (!_requestedImage.IsComplete) {
      if (leap_controller_.Now() - start > ImageTimeout)
        break;
    }
    byte[] packedImages = _requestedImage.Data;
    if (_requestedImage.IsComplete) {
      System.Array.Copy(packedImages, 0, r_image_data, 0, r_image_data.Length);
      System.Array.Copy(packedImages, _requestedImage.Width * _requestedImage.Height, l_image_data, 0, l_image_data.Length);
    }

    //FIND AND ADD BLOBS TO RIGHT ARRAY
    rcount = 0;
    int rbiggest = 0;
    for (int i = (Width * Height) - 1; i > 0; i--) {
      if ((PixelsFound < PixelLimit) && (r_image_data[i] > 240)){
        Vector3 Blob = BlobFind(i, r_image_data, 0, 0);
        if (Blob.z > 10) {
          RBlobs[rcount * 3] = Blob.x;
          RBlobs[(rcount * 3) + 1] = Blob.y;
          RBlobs[(rcount * 3) + 2] = Blob.z;
          if (Blob.z > RBlobs[rbiggest + 2]) {
            rbiggest = rcount * 3;
          }
          rcount++;
        }
      }
    }

    //FIND AND ADD BLOBS TO LEFT ARRAY
    lcount = 0;
    int lbiggest = 0;
    for (int i = (Width * Height) - 1; i > 0; i--) {
      if ((PixelsFound < PixelLimit) && (l_image_data[i] > 240)){
        Vector3 Blob = BlobFind(i, l_image_data, 0, 1);
        if (Blob.z > 10) {
          LBlobs[lcount * 3] = Blob.x;
          LBlobs[(lcount * 3) + 1] = Blob.y;
          LBlobs[(lcount * 3) + 2] = Blob.z;
          if (Blob.z > LBlobs[lbiggest + 2]) {
            lbiggest = lcount * 3;
          }
          lcount++;
        }
      }
    }

    if (PixelLimit == PixelsFound) {
      //Debug.Log("TOO MANY BRIGHT PIXELS, DATA UNRELIABLE");
    }

    {

      //CONVERT RAYS TO LEAP SPACE
      for (int i = 0; i < rcount * 3; i = i + 3) {
        //Debug.Log ("Blob coords:"+RBlobs[i]/RBlobs[i+2]+", "+RBlobs[i+1]/RBlobs[i+2]);
        //Vector fBlob = _requestedImage.Rectify (new Vector (((RBlobs [i] / RBlobs [i + 2])-320)/240f, ((RBlobs [i + 1] / RBlobs [i + 2])-120f)/120f, 0), Image.PerspectiveType.STEREO_RIGHT);
        Vector fBlob = _requestedImage.PixelToRectilinear(Image.PerspectiveType.STEREO_LEFT, new Vector(((RBlobs[i] / RBlobs[i + 2])), ((RBlobs[i + 1] / RBlobs[i + 2])), 0));
        //Debug.Log("BlobX: "+_requestedImage.Warp(new Vector(1000f,0f,1f),false));
        RBlobs[i] = -fBlob.x * RBlobs[i + 2];
        RBlobs[i + 1] = -fBlob.y * RBlobs[i + 2];
      }

      //CONVERT RAYS TO LEAP SPACE
      for (int i = 0; i < lcount * 3; i = i + 3) {
        //Vector fBlob = _requestedImage.Rectify (new Vector (((LBlobs [i] / LBlobs [i + 2])-320)/240f, ((LBlobs [i + 1] / LBlobs [i + 2])-120f)/120f, 0), Image.PerspectiveType.STEREO_LEFT);
        Vector fBlob = _requestedImage.PixelToRectilinear(Image.PerspectiveType.STEREO_RIGHT, new Vector(((LBlobs[i] / LBlobs[i + 2])), ((LBlobs[i + 1] / LBlobs[i + 2])), 0));
        //Debug.Log("BlobX: "+LBlobs [i] / LBlobs [i + 2] +" BloBY: "+LBlobs [i + 1] / LBlobs [i + 2]);
        LBlobs[i] = -fBlob.x * LBlobs[i + 2];
        LBlobs[i + 1] = -fBlob.y * LBlobs[i + 2];
      }

      //MATCH THE LEFT RAYS TO THE RIGHT RAYS
      //MAKE SURE RAYS ARE AT LEAST WITHIN 0.05 OF EACHOTHER
      //PICK PAIR THAT RESULTS IN LONGER DISTANCE TO AVOID ENTANGLEMENT
      float farthest;
      for (int i = 0; i < rcount * 3; i = i + 3) {
        if (RBlobs[i + 2] < BlobSizeCutoff) {
          Vector3 RDir = new Vector3((RBlobs[i] / RBlobs[i + 2]), 1f, RBlobs[i + 1] / RBlobs[i + 2]);
          if (DrawDebug) {
            Debug.DrawRay(transform.TransformPoint(CameraOffset), transform.TransformPoint(new Vector3(10f * RDir.x, 10f * RDir.y, 10f * RDir.z)), Color.red);
          }
          farthest = 0.02f; //MUST BE FARTHER THAN THIS TO COUNT AT ALL
          for (int j = 0; j < lcount * 3; j = j + 3) {
            if (LBlobs[j + 2] < BlobSizeCutoff) {
              Vector3 LDir = new Vector3(LBlobs[j] / LBlobs[j + 2], 1f, LBlobs[j + 1] / LBlobs[j + 2]);
              if (DrawDebug) {
                Debug.DrawRay(transform.TransformPoint(-CameraOffset), transform.TransformPoint(new Vector3(10f * LDir.x, 10f * LDir.y, 10f * LDir.z)), Color.cyan);
              }
              //TEST ALL FOR FARTHEST WORKING BLOB
              float intersectiondistance = ClosestDistOfApproach(CameraOffset, RDir, -CameraOffset, LDir);

              float distancefromleap = ClosestTimeOfApproach(CameraOffset, RDir, -CameraOffset, LDir);
              if (DrawDebug) {
                Debug.DrawLine(transform.TransformPoint(ClosestPointOfApproach(CameraOffset, RDir, -CameraOffset, LDir)), transform.TransformPoint(ClosestPointOfApproach(-CameraOffset, LDir, CameraOffset, RDir)));
              }
              if (distancefromleap > farthest) {
                if (intersectiondistance < 0.015f) {
                  farthest = distancefromleap;
                  MatchBlobs[i / 3] = j;
                }
              }
            }
          }
        }
      }

      //ADD BLOB POSITIONS (RELATIVE TO PARENT) ONLY AT THE INTERSECTION OF THE MATCHED RAYS
      for (int i = 0; i < rcount; i++) {
        if ((MatchBlobs[i] != 256)) {
          Vector3 RDir = new Vector3((RBlobs[(i * 3)] / RBlobs[(i * 3) + 2]), 1f, RBlobs[(i * 3) + 1] / RBlobs[(i * 3) + 2]);
          Vector3 LDir = new Vector3(LBlobs[MatchBlobs[i]] / LBlobs[MatchBlobs[i] + 2], 1f, LBlobs[MatchBlobs[i] + 1] / LBlobs[MatchBlobs[i] + 2]);
          Blobs.Add(transform.TransformPoint(ClosestPointOfApproach(CameraOffset, RDir, -CameraOffset, LDir)));
        }
      }

      //FINITO!
    }
  }

  //DRAW SPHERES IN EDITOR AT THE INTERSECTIONS OF THE MATCHED RAYS
  void OnDrawGizmos() {
    if (DrawDebug) {
      for (int i = 0; i < Blobs.Count; i++) {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere((Vector3)Blobs[i], 0.03f);
      }
    }
  }

  //RECURSIVE FOUR-CONNECTED FLOOD-FILL; WEIGHTS EXPOENENTIALLY BY BRIGHTNESS
  Vector3 BlobFind(int Start, byte[] image, int Stack, int Cam) {
    if (PixelsFound < PixelLimit) {
      //ADD THE WEIGHTED RAY DIRECTION TO THE TOTAL
      //Vector Dir = frame.Images[Cam].Rectify(new Vector(Start%Width,Start/Width,0f));
      Vector Dir = new Vector((float)(Start % Width), (float)(Start / Width), 0f);
      float Weight = Mathf.Pow(((float)image[Start] - (float)brightnessThreshold), 2) + 1f;
      //float Weight = 1f;
      Vector2 Pos = new Vector2(Dir.x, Dir.y) * Weight;
      float Sum = Weight;

      //MARK THIS PIXEL AS DONE SO WE DON'T REDO IT
      image[Start] = 0;
      PixelsFound++;

      //ASSIMILATE THE SURROUNDING PIXELS
      if (Stack < 5000) {//PREVENTS STACK OVERFLOW
        if ((Mathf.Floor(Start % Width) != 0) && (image[Start - 1] > brightnessThreshold)) {
          Vector3 nPos = BlobFind(Start - 1, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start % Width) != Width - 1) && (image[Start + 1] > brightnessThreshold)) {
          Vector3 nPos = BlobFind(Start + 1, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start / Width) != 0) && (image[Start - Width] > brightnessThreshold)) {
          Vector3 nPos = BlobFind(Start - Width, image, Stack + 1, Cam);
          Pos = new Vector2(Pos.x + nPos.x, Pos.y + nPos.y);
          Sum += nPos.z;
        }
        if ((Mathf.Floor(Start / Width) != Height - 1) && (image[Start + Width] > brightnessThreshold)) {
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

  //SETS DIMENSIONS OF BYTE ARRAY
  private void SetDimensions(int width, int height) {
    Debug.Log("New dimensions: " + width + " x " + height);
    int num_pixels = width * height;
    r_image_data = new byte[num_pixels];
    l_image_data = new byte[num_pixels];
  }

  //Methods below taken from http://answers.unity3d.com/questions/192261/finding-shortest-line-segment-between-two-rays-clo.html Credits to molokh
  //FIND DISTANCE ALONG LINE THAT IS CLOSEST TO OTHER LINE
  public static float ClosestTimeOfApproach(Vector3 pos1, Vector3 vel1, Vector3 pos2, Vector3 vel2) {
    //float t = 0;
    Vector3 dv = vel1 - vel2;
    float dv2 = Vector3.Dot(dv, dv);
    if (dv2 < 0.0000001f) {      // the tracks are almost parallel
      return 0.0f; // any time is ok.  Use time 0.
    }

    Vector3 w0 = pos1 - pos2;
    return (-Vector3.Dot(w0, dv) / dv2);
  }

  //FINDS DISTANCE WHERE LINES ARE CLOSEST
  public static float ClosestDistOfApproach(Vector3 pos1, Vector3 vel1, Vector3 pos2, Vector3 vel2) {
    float t = ClosestTimeOfApproach(pos1, vel1, pos2, vel2);
    Vector3 p1 = pos1 + (t * vel1);
    Vector3 p2 = pos2 + (t * vel2);

    return Vector3.Distance(p1, p2);           // distance at CPA
  }

  //FINDS POINT ON LINE ONE CLOSEST TO LINE TWO
  public static Vector3 ClosestPointOfApproach(Vector3 pos1, Vector3 vel1, Vector3 pos2, Vector3 vel2) {
    float t = ClosestTimeOfApproach(pos1, vel1, pos2, vel2);
    if (t < 0) { // don't detect approach points in the past, only in the future;
      return (pos1);
    }
    return (pos1 + (t * vel1));
  }*/
}