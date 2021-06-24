using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleAnimated : MonoBehaviour
{
    public AnimationCurve SpringAnimationCurveX;
    public AnimationCurve SpringAnimationCurveY;
    public AnimationCurve SpringAnimationCurveZ;
    bool isIn = false;
    float timeAt = 0;
    public float TimeToAnimate = 1;//just assume 1
    Vector3 originalScale;
    public GameObject ChildContent;
    public bool CanActivate = true;
    // Start is called before the first frame update
    void Start()
    {
        originalScale = transform.localScale;
        if(isIn){
            timeAt = 1;
        }else{
            timeAt = 0;
            transform.localScale = new Vector3(0.0000001f,0.0000001f,0.0000001f);
        }

    }
    public void SetIn(bool value){
        if(CanActivate){
            isIn = value;
        }
    }
    public void SetCanActivate(bool value){
        CanActivate = value;
    }
    // Update is called once per frame
    void Update()
    {
        if(isIn){
            if(timeAt < 1){
                timeAt += Time.deltaTime / TimeToAnimate; 

                Vector3 nn = originalScale;
                nn.Scale(new Vector3(SpringAnimationCurveX.Evaluate(timeAt),SpringAnimationCurveY.Evaluate(timeAt),SpringAnimationCurveZ.Evaluate(timeAt)));
                transform.localScale = nn;                
            }else{
                transform.localScale = originalScale;
            }
                if(ChildContent)if(!ChildContent.activeSelf){ChildContent.SetActive(true);}             
        }else{
            if(timeAt > 0){
                timeAt -= Time.deltaTime / TimeToAnimate; 
                Vector3 nn = originalScale;
                nn.Scale(new Vector3(SpringAnimationCurveX.Evaluate(timeAt),SpringAnimationCurveY.Evaluate(timeAt),SpringAnimationCurveZ.Evaluate(timeAt)));
                transform.localScale = nn;
            }else{
                transform.localScale = new Vector3(0.0000001f,0.0000001f,0.0000001f);
                if(ChildContent)if(ChildContent.activeSelf){ChildContent.SetActive(false);}                
            }            
        }
    }
}
