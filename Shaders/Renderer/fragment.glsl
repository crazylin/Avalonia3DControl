#version 300 es
precision highp float;
in vec3 vertexColor;

uniform float materialAlpha;

out vec4 fragColor;

void main()
{
    fragColor = vec4(vertexColor, materialAlpha);
}