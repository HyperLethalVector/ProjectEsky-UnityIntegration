
Texture2D txDiffuseLeft;
Texture2D txDiffuseRight;
SamplerState samLinear;
cbuffer ShaderVals : register(b0){	
    float4x4 leftUvToRectX;
	float4x4 leftUvToRectY;
	float4x4 rightUvToRectX;
	float4x4 rightUvToRectY;
    float4x4 cameraMatrixLeft;
    float4x4 cameraMatrixRight;
	float4 eyeBordersLeft;
	float4 eyeBordersRight;
	float4 offsets;
};
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
//note the left and right eyes are flipped due to the NorthStar rendering being upside down
float4 PShader(float4 position : SV_POSITION, float2 tex: TEXCOORD) : SV_TARGET
{
    float xSettled = 1.0-(tex.x);
    float ySettled = tex.y;
    float2 distorted_uv = tex;
    if(xSettled < 0.5){//we render the left eye
        float2 newTex = float2(xSettled*2,ySettled);
        float3 rectilinear_coordinate = float3(polyval2d(1.0-newTex.x, newTex.y, rightUvToRectX),polyval2d(1.0 - newTex.x, newTex.y, rightUvToRectY), 1.0);
        float2 distorted_uv = WorldToViewportInnerVec(cameraMatrixRight,rectilinear_coordinate);
        distorted_uv += float2(offsets.z,offsets.w);        
        if(distorted_uv.x < eyeBordersRight.x || distorted_uv.x > eyeBordersRight.y || distorted_uv.y < eyeBordersRight.z || distorted_uv.y > eyeBordersRight.w)
        return float4(0.0,0.0,0.0,1.0);
        else
        return txDiffuseLeft.Sample(samLinear, distorted_uv);
    }else{//we render the right eye
        float2 newTex = float2((xSettled-0.5)*2,ySettled);  
        float3 rectilinear_coordinate = float3(polyval2d(1.0-newTex.x, newTex.y, leftUvToRectX),polyval2d(1.0 - newTex.x, newTex.y, leftUvToRectY), 1.0);
        float2 distorted_uv = WorldToViewportInnerVec(cameraMatrixLeft,rectilinear_coordinate);
        distorted_uv += float2(offsets.x,offsets.y);
        if(distorted_uv.x < eyeBordersLeft.x || distorted_uv.x > eyeBordersLeft.y || distorted_uv.y < eyeBordersLeft.z || distorted_uv.y > eyeBordersLeft.w)
        return float4(0.0,0.0,0.0,1.0);
        else
        return txDiffuseRight.Sample(samLinear, distorted_uv);        
    }
    return float4(eyeBordersLeft[0],eyeBordersLeft[1],eyeBordersLeft[2],1.0);
}
