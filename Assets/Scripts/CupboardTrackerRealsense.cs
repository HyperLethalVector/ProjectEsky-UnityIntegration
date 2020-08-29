using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Intel.RealSense;
using UnityEngine;

using ProjectCupboard.Renderer;
namespace ProjectCupboard.Tracking{
    public class CupboardTrackerRealsense : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        public class RsPose
        {
            public Vector3 translation;
            public Vector3 velocity;
            public Vector3 acceleration;
            public Quaternion rotation;
            public Vector3 angular_velocity;
            public Vector3 angular_acceleration;
            public int tracker_confidence;
            public int mapper_confidence;
        }
        RsPose pose = new RsPose();
        public Transform RigCenter;
        public Matrix4x4 TransformFromTrackerToCenter;
        bool doTracking = false;

        public RsFrameProvider Source;

        FrameQueue q;

        void Start()
        {
            Source.OnStart += OnStartStreaming;
            Source.OnStop += OnStopStreaming;
        }

        private void OnStartStreaming(PipelineProfile profile)
        {
            q = new FrameQueue(1);
            Source.OnNewSample += OnNewSample;
        }


        private void OnStopStreaming()
        {
            Source.OnNewSample -= OnNewSample;

            if (q != null)
            {
                q.Dispose();
                q = null;
            }
        }


        private void OnNewSample(Frame f)
        {
            if (f.IsComposite)
            {
                using (var fs = f.As<FrameSet>())
                using (var poseFrame = fs.FirstOrDefault(Stream.Pose, Format.SixDOF))
                    if (poseFrame != null)
                        q.Enqueue(poseFrame);
            }
            else
            {
                using (var p = f.Profile)
                    if (p.Stream == Stream.Pose && p.Format == Format.SixDOF)
                        q.Enqueue(f);
            }
        }
        public float smoothTime = 0.3F;
        private Vector3 velocity = Vector3.zero;
        void Update()
        {
            if (q != null)
            {
                PoseFrame frame;
                if (q.PollForFrame<PoseFrame>(out frame))
                    using (frame)
                    {
                        frame.CopyTo(pose);

                        // Convert T265 coordinate system to Unity's
                        // see https://realsense.intel.com/how-to-getting-imu-data-from-d435i-and-t265/

                        var t = pose.translation;
                        t.Set(t.x, t.y, -t.z);

                        var e = pose.rotation.eulerAngles;
                        var r = Quaternion.Euler(-e.x, -e.y, e.z);

                        transform.localRotation = r;
                        transform.localPosition = Vector3.SmoothDamp(transform.localPosition,t,ref velocity, smoothTime);
                        Matrix4x4 m = Matrix4x4.TRS(transform.transform.position,transform.transform.rotation,Vector3.one);
                        m = m * TransformFromTrackerToCenter.inverse;
                        RigCenter.transform.position = m.MultiplyPoint3x4(Vector3.zero);
                        RigCenter.transform.rotation = m.rotation;
                    }

            }
        }
    }
}