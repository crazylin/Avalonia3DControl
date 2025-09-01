#version 300 es
precision highp float;
in vec3 Color;
in vec2 TexCoord;

uniform bool hasTexture;
uniform sampler2D texture0;
uniform float materialAlpha;

out vec4 fragColor;

void main()
{
    vec3 textureColor;
    
    if (hasTexture) {
        textureColor = texture(texture0, TexCoord).rgb;
    } else {
        float scale = 8.0;
        vec2 scaledCoord = TexCoord * scale;
        vec2 grid = floor(scaledCoord);
        float checker = mod(grid.x + grid.y, 2.0);
        textureColor = mix(vec3(0.8, 0.8, 0.8), vec3(0.2, 0.2, 0.2), checker);
    }
    
    fragColor = vec4(textureColor * Color, materialAlpha);
}