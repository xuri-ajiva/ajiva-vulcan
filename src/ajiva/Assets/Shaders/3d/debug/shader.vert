#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
} viewProj;

// Vertex attributes
layout(location = 0) in vec3 inPos;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inUV;


// Instanced attributes
layout (location = 3) in vec3 instancePos;
layout (location = 4) in vec3 instanceRot;
layout (location = 5) in vec3 instanceScale;
layout (location = 6) in int instanceTexIndex;
layout (location = 7) in vec2 padding;

layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outUV;

mat3 rotationX(in float angle) {
    return mat3(1.0, 0, 0,
    0, cos(angle), -sin(angle),
    0, sin(angle), cos(angle));
}

mat3 rotationY(in float angle) {
    return mat3(cos(angle), 0, sin(angle),
    0, 1.0, 0,
    -sin(angle), 0, cos(angle));
}

mat3 rotationZ(in float angle) {
    return mat3(cos(angle), -sin(angle), 0,
    sin(angle), cos(angle), 0,
    0, 0, 1);
}

void main() {
    outColor = inColor;
    outUV = vec3(inUV, instanceTexIndex);
    vec3 rotPos = inPos * rotationX(instanceRot.x) * rotationY(instanceRot.y) * rotationZ(instanceRot.z);
    vec4 pos = vec4(rotPos * instanceScale + instancePos, 1.0);

    gl_Position = viewProj.proj * viewProj.view * pos;
}
