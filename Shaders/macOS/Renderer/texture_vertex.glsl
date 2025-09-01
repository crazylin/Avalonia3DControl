#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform bool uPointMode;
uniform float uPointSize;

out vec3 Color;
out vec2 TexCoord;

void main()
{
    Color = aColor;
    TexCoord = (aPosition.xy + 1.0) * 0.5;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    if (uPointMode) { gl_PointSize = uPointSize; }
}