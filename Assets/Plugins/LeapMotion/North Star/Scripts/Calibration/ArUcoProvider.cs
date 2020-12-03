using UnityEngine;
using LeapInternal;
using Leap.Unity;
using System;
using System.Collections.Generic;

public class ArUcoProvider : MonoBehaviour {
  public LeapServiceProvider provider;
  public Vector3[] points;// = new Vector3[4];
  public bool interpolation = true;

  long time, curTimestamp, prevTimestamp;
  Dictionary<uint, Vector3> curFrame, prevFrame;

  //RingBuffer<Dictionary<uint, Vector3>> frameHistory = new RingBuffer<Dictionary<uint, Vector3>>(3);
  //RingBuffer<long> frameHistoryTimestamps = new RingBuffer<long>(3);

  private void Update() {
    /*//Debug.Log(Time.deltaTime);
    var controller = provider.GetLeapController();
    LEAP_POINT_MAPPING mapping = new LEAP_POINT_MAPPING();
    controller.GetPointMapping(ref mapping);
    points = new Vector3[mapping.nPoints];
    frameHistory.Add(curFrame);
    frameHistoryTimestamps.Add(curTimestamp);
    //prevTimestamp = curTimestamp;
    //prevFrame = (curFrame != null) ? curFrame : new Dictionary<uint, Vector3>(points.Length);

    curTimestamp = controller.FrameTimestamp();
    curFrame = new Dictionary<uint, Vector3>(points.Length);
    for (int i = 0; i < mapping.nPoints; i++) {
      Vector3 t = mapping.points[i].ToVector3() * 0.001f;
      t = new Vector3(t.x, t.y, -t.z);
      t = transform.parent.TransformPoint(t);
      curFrame.Add(mapping.ids[i], t);
      //points[i] = t;
    }

    prevFrame = frameHistory.GetOldest(); prevTimestamp = frameHistoryTimestamps.GetOldest();
    long time = (controller.Now() - (long)provider.smoothedTrackingLatency) + 10000;
    float alpha = (float)(time - prevTimestamp) / (float)(curTimestamp - prevTimestamp);
    //Debug.Log(alpha);

    int index = 0;
    foreach (uint id in curFrame.Keys) {
      if (interpolation && prevFrame != null && prevFrame.ContainsKey(id) && curTimestamp != prevTimestamp) {
        if (prevFrame[id] != curFrame[id]) {
          points[index] = Vector3.LerpUnclamped(prevFrame[id], curFrame[id], alpha);
        } else {
          points[index] = Vector3.zero;
        }
      } else {
        points[index] = curFrame[id];
      }
      index++;
    }

    //if(arucoPoints.Length == 4) {
    //  Array.Copy(arucoPoints, points, 4);
    //}*/
  }
}