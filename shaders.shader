
Texture2D txDiffuseLeft: register(t0);
Texture2D txDiffuseRight: register(t1);
Texture2D txDiffuseLeftLuT: register(t2);
Texture2D txDiffuseRightLuT: register(t3); 
SamplerState samLinear;

cbuffer ShaderVals : register(b0){	
    float4x4 leftUvToRectX;
	float4x4 leftUvToRectY;
	float4x4 rightUvToRectX;
	float4x4 rightUvToRectY;
    float4x4 cameraMatrixLeft;
    float4x4 cameraMatrixRight;
    float4x4 invCameraMatrixLeft;
    float4x4 invCameraMatrixRight;    
	float4 eyeBordersLeft;
	float4 eyeBordersRight;
	float4 offsets;
};
cbuffer ShaderVals2 : register(b1){
    float4x4 DeltaPoseLeft;  
    float4x4 DeltaPoseRight;  
    float4 toggleConfigs;
}
struct VOut
{
    float4 position : SV_POSITION;
	float2 tex : TEXCOORD;
};

VOut VShader(float4 position : POSITION, float2 tex : TEXCOORD)
{
    VOut output;

    output.position = position;
    output.tex = tex;

    return output;
}

float2 WorldToViewportInnerVec(float4x4 inputPerspective, float3 worldPoint) {
      float3 result;
      result.x = inputPerspective[0][0] * worldPoint.x + inputPerspective[0][1] * worldPoint.y + inputPerspective[0][2] * worldPoint.z + inputPerspective[0][3];
      result.y = inputPerspective[1][0] * worldPoint.x + inputPerspective[1][1] * worldPoint.y + inputPerspective[1][2] * worldPoint.z + inputPerspective[1][3];
      result.z = inputPerspective[2][0] * worldPoint.x + inputPerspective[2][1] * worldPoint.y + inputPerspective[2][2] * worldPoint.z + inputPerspective[2][3];
      float  w = inputPerspective[3][0] * worldPoint.x + inputPerspective[3][1] * worldPoint.y + inputPerspective[3][2] * worldPoint.z + inputPerspective[3][3];
      result.x /= w; result.y /= w;
      result.x = (result.x * 0.5 + 0.5);
      result.y = (result.y * 0.5 + 0.5);
      return result.xy;
}

float polyval2d(float X, float Y, float4x4 C) {
  float X2 = X * X; float X3 = X2 * X;
  float Y2 = Y * Y; float Y3 = Y2 * Y;
  return (
          ((C[0][ 0]     ) + (C[0][1]      * Y) + (C[0][ 2]      * Y2) + (C[0][ 3]      * Y3)) +
          ((C[1][ 0] * X ) + (C[1][ 1] * X  * Y) + (C[1][ 2] * X  * Y2) + (C[1][ 3] * X  * Y3)) +
          ((C[2][ 0] * X2) + (C[2][ 1] * X2 * Y) + (C[2][2] * X2 * Y2) + (C[2][3] * X2 * Y3)) +
          ((C[3][0] * X3) + (C[3][1] * X3 * Y) + (C[3][2] * X3 * Y2) + (C[3][3] * X3 * Y3))
          );
}
float2 resolveTemporalWarping(float2 inputUV, float4x4 DeltaPose){
        float4x4 hardCodedBias = {2,0,0,-1.0,0,2,0,-1.0,0,0,2,0,0,0,0,1};  
        float4x4 hardCodedInverseBias = {0.5,0,0, 0.5,0,0.5,0,0.5,0,0,0.5,0,0,0,0,1};          
        float planeDepth = 0.05;     
        float4 depthProbe = float4(0.0,0.0,planeDepth,1.0); // Point in initial screen space 1, depth 1
        float4 viewRay = float4(inputUV.x,inputUV.y,planeDepth,1.0); // point in final screen space
        
        float4 d = mul(cameraMatrixLeft,depthProbe); // Point in initial world space      
        float4 O = mul(DeltaPose,float4(0.0,0.0,0.0,1.0)); //Origin of future pose        
        float4 P = mul(hardCodedBias,viewRay); P = mul(invCameraMatrixLeft,P);  // point in final world space
        P = mul(DeltaPose,P); //point in final world space
        float4 V = normalize(P - O);
        float4 unitPlaneNormal = float4(0.0,0.0,1.0,1.0);
        float s = dot(unitPlaneNormal,V);
        float t = (O - dot(unitPlaneNormal,V))/s;
        float4 OX = V * t; //get the vector to the plane reprojected
        float4 retd = mul(cameraMatrixLeft,OX);
        retd = mul(hardCodedInverseBias,retd);
        float3 retdd = float3(retd.x/retd.w,retd.y/retd.w,retd.z/retd.w);
        return float2( (retdd.x),(retdd.y));
}
float4 resolveWithoutDistortion(float xSettled, float ySettled){            
    if(xSettled < 0.5){//we render the left eye
        float2 newTex = float2(ySettled,xSettled*2);// input quad UV in world space (should be between 0-1)                
        float2 distorted_uv = resolveTemporalWarping(newTex,DeltaPoseLeft); // perform the temporal warping
        if(toggleConfigs.x != 0.0){
            distorted_uv = newTex;
        }
        if(distorted_uv.x < eyeBordersRight.x || distorted_uv.x > eyeBordersRight.y || distorted_uv.y < eyeBordersRight.z || distorted_uv.y > eyeBordersRight.w){//ensure the UVS are within the set bounds for the eye
            return float4(0.0,0.0,0.0,1.0);//if outside, return black (prevent)
        }else{
            return txDiffuseLeft.Sample(samLinear, distorted_uv)* toggleConfigs.y;
        }
    }else{//we render the right eye        
        float2 newTex = float2(ySettled,(xSettled-0.5)*2); //input quad UV in world space (should be between 0-1)          
        float2 distorted_uv = resolveTemporalWarping(newTex,DeltaPoseRight); // perform the temporal warping

        if(distorted_uv.x < eyeBordersLeft.x || distorted_uv.x > eyeBordersLeft.y || distorted_uv.y < eyeBordersLeft.z || distorted_uv.y > eyeBordersLeft.w){
            return float4(0.0,0.0,0.0,1.0);
        }else{
            return txDiffuseRight.Sample(samLinear, distorted_uv) * toggleConfigs.y;        
        }
    }
    return float4(0.0,0.0,0.0,1.0);
}
float4 resolveWithDistortion(float xSettled, float ySettled){
    if(xSettled < 0.5){//we render the left eye
        float2 newTex = float2(xSettled*2,ySettled);// input quad UV in world space (should be between 0-1)
        float3 rectilinear_coordinate = float3(polyval2d(1.0-newTex.x, newTex.y, rightUvToRectX),polyval2d(1.0 - newTex.x, newTex.y, rightUvToRectY), 1.0); //resolve the 2D polynomial to get a modified world space UV
        float2 distorted_uv = WorldToViewportInnerVec(cameraMatrixRight,rectilinear_coordinate); //project back into screen space
        distorted_uv += float2(offsets.z,offsets.w); //apply a screen space UV offset
        if(toggleConfigs.x == 0.0){
            distorted_uv = resolveTemporalWarping(distorted_uv,DeltaPoseLeft);        //Should do things here for reprojection ....
        }
        if(distorted_uv.x < eyeBordersRight.x || distorted_uv.x > eyeBordersRight.y || distorted_uv.y < eyeBordersRight.z || distorted_uv.y > eyeBordersRight.w)//ensure the UVS are within the set bounds for the eye
        return float4(0.0,0.0,0.0,1.0);//if outside, return black (prevent)
        else
        return txDiffuseLeft.Sample(samLinear, distorted_uv)* toggleConfigs.y;
    }else{//we render the right eye
        float2 newTex = float2((xSettled-0.5)*2,ySettled);  
        float3 rectilinear_coordinate = float3(polyval2d(1.0-newTex.x, newTex.y, leftUvToRectX),polyval2d(1.0 - newTex.x, newTex.y, leftUvToRectY), 1.0);
        float2 distorted_uv = WorldToViewportInnerVec(cameraMatrixLeft,rectilinear_coordinate);
        distorted_uv += float2(offsets.x,offsets.y);
        if(toggleConfigs.x == 0.0){
            distorted_uv = resolveTemporalWarping(distorted_uv,DeltaPoseRight);        
        }
        if(distorted_uv.x < eyeBordersLeft.x || distorted_uv.x > eyeBordersLeft.y || distorted_uv.y < eyeBordersLeft.z || distorted_uv.y > eyeBordersLeft.w)
        return float4(0.0,0.0,0.0,1.0);
        else
        return txDiffuseRight.Sample(samLinear, distorted_uv)* toggleConfigs.y;        
    }
    return float4(0.0,0.0,0.0,1.0);
}
float4 resolveWithLuT(float xSettled, float ySettled){            
    if(xSettled < 0.5){//we render the left eye
        float2 newTex = float2(ySettled,xSettled*2);// input quad UV in world space (should be between 0-1)                
        float2 distorted_uv = resolveTemporalWarping(txDiffuseLeftLuT.Sample(samLinear, newTex).xz,DeltaPoseLeft); // perform the temporal warping
        if(toggleConfigs.x != 0.0){
            distorted_uv = newTex;
        }
        if(distorted_uv.x < eyeBordersRight.x || distorted_uv.x > eyeBordersRight.y || distorted_uv.y < eyeBordersRight.z || distorted_uv.y > eyeBordersRight.w){//ensure the UVS are within the set bounds for the eye
            return float4(0.0,0.0,0.0,1.0);//if outside, return black (prevent)
        }else{
            return txDiffuseLeft.Sample(samLinear, distorted_uv)* toggleConfigs.y;
        }
    }else{//we render the right eye        
        float2 newTex = float2(ySettled,(xSettled-0.5)*2); //input quad UV in world space (should be between 0-1)          
        float2 distorted_uv = resolveTemporalWarping(txDiffuseRightLuT.Sample(samLinear, newTex).xz,DeltaPoseRight); // perform the temporal warping

        if(distorted_uv.x < eyeBordersLeft.x || distorted_uv.x > eyeBordersLeft.y || distorted_uv.y < eyeBordersLeft.z || distorted_uv.y > eyeBordersLeft.w){
            return float4(0.0,0.0,0.0,1.0);
        }else{
            return txDiffuseRight.Sample(samLinear, distorted_uv) * toggleConfigs.y;        
        }
    }
    return float4(0.0,0.0,0.0,1.0);
}
//note the left and right eyes are flipped due to the NorthStar rendering being upside down
float4 PShader(float4 position : SV_POSITION, float2 tex: TEXCOORD) : SV_TARGET
{
    float xSettled = 1.0-(tex.x); // flip the X axis since the screen is upside down
    float ySettled = tex.y; //we can use the raw Y
    return resolveWithDistortion(xSettled,ySettled);
}
