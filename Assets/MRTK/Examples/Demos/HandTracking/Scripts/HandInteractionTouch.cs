// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    [AddComponentMenu("Scripts/MRTK/Examples/HandInteractionTouch")]
    public class HandInteractionTouch : MonoBehaviour, IMixedRealityTouchHandler
    {
        [SerializeField]
        private TextMesh debugMessage = null;
        [SerializeField]
        private TextMesh debugMessage2 = null;

        #region Event handlers
        public TouchEvent OnTouchCompleted;
        public TouchEvent OnTouchStarted;
        public TouchEvent OnTouchUpdated;
        #endregion

        private Renderer TargetRenderer;
        private Color originalColor;
        private Color highlightedColor;

        protected float duration = 1.5f;
        protected float t = 0;

        bool isIn = false;
        public bool updateColor;
        public void SetHighlightedColor(float r, float g, float b){
            highlightedColor = new Color(r,g,b);
        }
        private void Start()
        {
            TargetRenderer = GetComponentInChildren<Renderer>();
            if ((TargetRenderer != null) && (TargetRenderer.sharedMaterial != null))
            {
                originalColor = TargetRenderer.sharedMaterial.color;
                highlightedColor = new Color(originalColor.r + 0.2f, originalColor.g + 0.2f, originalColor.b + 0.2f);
            }
        }
        private void FixedUpdate(){
                originalColor = TargetRenderer.sharedMaterial.color;            
                if(isIn){                                   
                    if ((TargetRenderer != null) && (TargetRenderer.material != null))
                    {
                        TargetRenderer.material.color = Color.Lerp(originalColor, Color.red, Time.deltaTime*4f);
                    }
                }else{                    
                    if (TargetRenderer != null)
                    {
                        TargetRenderer.sharedMaterial.color = Color.Lerp(originalColor, highlightedColor, Time.deltaTime*4f);
                    }
                }
        }
        public void RemoteTouchStart(){
            isIn = true;
        }
        public void RemoteTouchEnd(){
            isIn = false;
        }

        void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            OnTouchCompleted.Invoke(eventData);

            if (debugMessage != null)
            {
                debugMessage.text = "OnTouchCompleted: " + Time.unscaledTime.ToString();
            }
            isIn = false;
        }

        void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
        {
            OnTouchStarted.Invoke(eventData);

            isIn = true;
        }

        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            OnTouchUpdated.Invoke(eventData);

            if (debugMessage2 != null)
            {
                debugMessage2.text = "OnTouchUpdated: " + Time.unscaledTime.ToString();
            }

            
        }
    }
}