#version 330

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec2 texCoord;

smooth out vec3 theColor;
smooth out vec2 txCrd;
void main()
{
   gl_Position = vec4(inPosition, 1.0);
   theColor = inColor;
   txCrd = texCoord;
}