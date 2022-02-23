#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
    vec2 mousePos;
    vec2 vec;
} data;

// Vertex attributes
layout(location = 0) in vec2 inPos;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inUV;


// Instanced attributes
layout (location = 3) in vec2 instancePos;
layout (location = 4) in vec2 instanceRot;
layout (location = 5) in vec2 instanceScale;
layout (location = 6) in int instanceTexIndex;
layout (location = 7) in vec2 padding;

layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outUV;

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
    vec2 rotPos = inPos * rotationX(instanceRot.x) * rotationY(instanceRot.y);
    vec4 pos = vec4(rotPos * instanceScale + instancePos, 1.0, 1.0);
    
    gl_Position =  pos;
    outColor = inColor;
    outUV = vec3(inUV + data.mousePos, instanceTexIndex);
}
