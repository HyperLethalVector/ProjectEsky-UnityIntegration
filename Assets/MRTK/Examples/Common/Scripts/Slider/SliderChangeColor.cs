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
        bool updated = false;
        void Start(){

        }
        private void FixedUpdate() {
            if(updated){
                TargetRenderer = GetComponentInChildren<Renderer>();                
                updated = false;
                if ((TargetRenderer != null) && (TargetRenderer.material != null))
                {
                    TargetRenderer = GetComponentInChildren<Renderer>();                
                    TargetRenderer.material.color = new Color(red, green, blue);
                }          
            }  
        }

        public void OnSliderUpdateRedNetwork(float newVal){
            red = newVal;
            updated = true;
        }
        public void OnSliderUpdateGreenNetwork(float newVal){
            green = newVal;
            updated = true;            
        }
        public void OnSliderUpdateBlueNetwork(float newVal){
            blue = newVal;
            updated = true;            
        }        
        public void OnSliderUpdatedRed(SliderEventData eventData)
        {
            red = eventData.NewValue;
            updated = true;            
        }

        public void OnSliderUpdatedGreen(SliderEventData eventData)
        {
            green = eventData.NewValue;
            updated = true;            
        }

        public void OnSliderUpdateBlue(SliderEventData eventData)
        {
            blue = eventData.NewValue;
            updated = true;            
        }
    }
}
