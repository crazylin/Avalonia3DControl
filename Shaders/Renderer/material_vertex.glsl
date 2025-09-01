#version 300 es
precision highp float;
in vec3 aPosition;
in vec3 aColor;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform bool uPointMode;
uniform float uPointSize;
out vec3 vertexColor;
out vec3 worldPos;
out vec3 normal;
void main() {
    vec4 worldPosition = model * vec4(aPosition, 1.0);
    worldPos = worldPosition.xyz;
    normal = normalize(mat3(model) * vec3(0.0, 0.0, 1.0));
    gl_Position = projection * view * worldPosition;
    if (uPointMode) { gl_PointSize = uPointSize; }
    vertexColor = aColor;
}