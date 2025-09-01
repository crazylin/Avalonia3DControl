#version 330 core
in vec3 vertexColor;

uniform float materialAlpha;

out vec4 fragColor;

void main()
{
    fragColor = vec4(vertexColor, materialAlpha);
}