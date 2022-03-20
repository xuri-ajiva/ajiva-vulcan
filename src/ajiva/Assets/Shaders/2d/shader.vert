#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
    vec2 mousePos;
    vec2 vec;
} data;

// Vertext attributes
layout (location = 0) in vec2 inPos;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec2 inUV;

// Instanced attributes
layout (location = 3) in vec2 instanceOffset;
layout (location = 4) in vec2 instanceScale;
layout (location = 5) in vec2 instanceRot;
layout (location = 6) in uint instanceTexIndex;
layout (location = 7) in uint instanceDrawType;

// output attributes
layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outUV;
layout(location = 2) out vec2 outPos;

mat2 rotationX(in float angle) {
    return mat2(
    cos(angle), -sin(angle),
    sin(angle), cos(angle)
    );
}
mat2 rotationY(in float angle) {
    return mat2(
    cos(angle), sin(angle),
    -sin(angle), cos(angle)
    );
}

void main() {
    outPos = inPos;
    
    //inPos += data.mousePos;
    vec2 scaledPos = inPos * instanceScale;
    vec2 translatedPos = scaledPos + instanceOffset;
    vec2 rotPos = translatedPos * rotationX(instanceRot.x) * rotationY(instanceRot.y);
    vec4 pos = vec4(rotPos, 1.0, 1.0);

    gl_Position =  pos;
    //outColor = vec3(1, 0, 1);
    outUV = vec3(inUV, instanceTexIndex);
}
