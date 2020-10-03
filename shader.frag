#version 330

smooth in vec3 theColor;
smooth in vec2 txCrd;
out vec4 outputColor;
uniform sampler2D gSamplerLeft;
uniform sampler2D gSamplerRight;
uniform float[16] leftUvToRectX;
uniform float[16] leftUvToRectY;
uniform float[16] rightUvToRectX;
uniform float[16] rightUvToRectY;
uniform float[16] CameraMatrixLeft;
uniform float[16] CameraMatrixRight;
uniform float[2] leftOffset;
uniform float[2] rightOffset;
uniform float[4] eyeBordersLeft;
uniform float[4] eyeBordersRight;
// Evaluate a 2D polynomial from its coefficients

float polyval2d(float X, float Y, float[16] C) {
  float X2 = X * X; float X3 = X2 * X;
  float Y2 = Y * Y; float Y3 = Y2 * Y;
  return (((C[ 0]     ) + (C[ 1]      * Y) + (C[ 2]      * Y2) + (C[ 3]      * Y3)) +
          ((C[ 4] * X ) + (C[ 5] * X  * Y) + (C[ 6] * X  * Y2) + (C[ 7] * X  * Y3)) +
          ((C[ 8] * X2) + (C[ 9] * X2 * Y) + (C[10] * X2 * Y2) + (C[11] * X2 * Y3)) +
          ((C[12] * X3) + (C[13] * X3 * Y) + (C[14] * X3 * Y2) + (C[15] * X3 * Y3)));
}
/*
vec2 WorldToViewport(vec3 worldPoint) {
      vec3 result;
      result.x = leftCameraProjection[0] * worldPoint.x + leftCameraProjection[1] * worldPoint.y + leftCameraProjection[2] * worldPoint.z + leftCameraProjection[3];
      result.y = leftCameraProjection[4] * worldPoint.x + leftCameraProjection[5] * worldPoint.y + leftCameraProjection[6] * worldPoint.z + leftCameraProjection[7];
      result.z = leftCameraProjection[8] * worldPoint.x + leftCameraProjection[9] * worldPoint.y + leftCameraProjection[10] * worldPoint.z + leftCameraProjection[11];
      float  w = leftCameraProjection[12] * worldPoint.x + leftCameraProjection[13] * worldPoint.y + leftCameraProjection[14] * worldPoint.z + leftCameraProjection[15];
      result.x /= w; result.y /= w;
      result.x = (result.x * 0.5 + 0.5);
      result.y = (result.y * 0.5 + 0.5);
      return result.xy;
}*/
vec2 WorldToViewportInnerVec(float[16] inputPerspective, vec3 worldPoint) {
      vec3 result;
      result.x = inputPerspective[0] * worldPoint.x + inputPerspective[1] * worldPoint.y + inputPerspective[2] * worldPoint.z + inputPerspective[3];
      result.y = inputPerspective[4] * worldPoint.x + inputPerspective[5] * worldPoint.y + inputPerspective[6] * worldPoint.z + inputPerspective[7];
      result.z = inputPerspective[8] * worldPoint.x + inputPerspective[9] * worldPoint.y + inputPerspective[10] * worldPoint.z + inputPerspective[11];
      float  w = inputPerspective[12] * worldPoint.x + inputPerspective[13] * worldPoint.y + inputPerspective[14] * worldPoint.z + inputPerspective[15];
      result.x /= w; result.y /= w;
      result.x = (result.x * 0.5 + 0.5);
      result.y = (result.y * 0.5 + 0.5);
      return result.xy;
}
void main()
{
   float xSettled = 1.0-(txCrd.x);
   float ySettled = txCrd.y;
   vec2 distorted_uv = txCrd;
   if(xSettled <= 0.5){ //we're rendering the left eye (As it's flipped upside down)
      xSettled = (xSettled*2) ;     
      vec2 uv = vec2(xSettled,ySettled);      
      vec3 rectilinear_coordinate = vec3(polyval2d(1.0 - uv.x, uv.y, rightUvToRectX),polyval2d(1.0 - uv.x, uv.y, rightUvToRectY), 1.5);
      vec2 distorted_uv = WorldToViewportInnerVec(CameraMatrixRight,rectilinear_coordinate);       
      distorted_uv += vec2(rightOffset[0],rightOffset[1]);      
      if(distorted_uv.x < eyeBordersRight[0] || distorted_uv.x > eyeBordersRight[1] || distorted_uv.y < eyeBordersRight[2] || distorted_uv.y > eyeBordersRight[3])
         outputColor = vec4(0.0,0.0,0.0,1.0);
      else
         outputColor = texture2D(gSamplerRight, distorted_uv);      
      
   }else{ // we're rendering the left eye      
      xSettled = (xSettled-0.5)*2;      
      vec2 uv = vec2(xSettled,ySettled);
      vec3 rectilinear_coordinate = vec3(polyval2d(1.0 - uv.x, uv.y, leftUvToRectX), polyval2d(1.0 - uv.x, uv.y, leftUvToRectY), 1.5);               
      vec2 distorted_uv = WorldToViewportInnerVec(CameraMatrixLeft,rectilinear_coordinate); 
      distorted_uv += vec2(leftOffset[0],leftOffset[1]);
      if(distorted_uv.x < eyeBordersLeft[0] || distorted_uv.x > eyeBordersLeft[1] || distorted_uv.y < eyeBordersLeft[2] || distorted_uv.y > eyeBordersLeft[3])
         outputColor = vec4(0.0,0.0,0.0,1.0);
      else     
         outputColor = texture2D(gSamplerLeft, distorted_uv);                   
   }

}