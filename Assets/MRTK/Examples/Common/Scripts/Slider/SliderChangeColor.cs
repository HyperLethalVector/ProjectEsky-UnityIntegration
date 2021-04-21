//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    [AddComponentMenu("Scripts/MRTK/Examples/SliderChangeColor")]
    public class SliderChangeColor : MonoBehaviour
    {
        [SerializeField]
        private Renderer TargetRenderer;
        public float red;
        public float green;

        public float blue;
        void Start(){
            TargetRenderer = GetComponentInChildren<Renderer>();
        }
        private void FixedUpdate() {
            if ((TargetRenderer != null) && (TargetRenderer.material != null))
            {
                TargetRenderer.material.color = new Color(red, green, blue);
            }            
        }

        public void OnSliderUpdateRedNetwork(float newVal){
            red = newVal;
        }
        public void OnSliderUpdateGreenNetwork(float newVal){
            green = newVal;
        }
        public void OnSliderUpdateBlueNetwork(float newVal){
            blue = newVal;
        }        
        public void OnSliderUpdatedRed(SliderEventData eventData)
        {
            red = eventData.NewValue;
        }

        public void OnSliderUpdatedGreen(SliderEventData eventData)
        {
            green = eventData.NewValue;
        }

        public void OnSliderUpdateBlue(SliderEventData eventData)
        {
            blue = eventData.NewValue;
        }
    }
}
