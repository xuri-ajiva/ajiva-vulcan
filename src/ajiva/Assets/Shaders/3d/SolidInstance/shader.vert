#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
} viewProj;
layout(binding = 1) uniform UniformModel {
    mat4 model;
    uint fragtexSamplerId;
    int fragtexSamplerId2;
    int fragtexSamplerId3;
    int fragtexSamplerId4;
} model;

/*
layout(binding = 0) uniform UniformBufferObject {
  mat4 model;
  mat4 view;
  mat4 proj;
} ubo;
       */

// Vertex attributes
layout(location = 0) in vec3 inPos;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inUV;

/*
layout (location = 0) in vec3 inPos;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec3 inColor;*/

// Instanced attributes
layout (location = 3) in vec3 instancePos;
layout (location = 4) in vec3 instanceRot;
layout (location = 5) in vec3 instanceScale;
layout (location = 6) in int instanceTexIndex;
layout (location = 7) in vec2 padding;


layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outUV;

void main() {
    outColor = inColor;
    outUV = vec3(inUV, instanceTexIndex);
    
    vec4 pos = vec4((inPos.xyz * instanceScale) + instancePos, 1.0);
    
    gl_Position = viewProj.proj * viewProj.view * pos;
    //outNormal = mat3(modelview * gRotMat) * inverse(rotMat) * inNormal;

/*    gl_Position = viewProj.proj * viewProj.view * model.model * vec4(inPosition, 1.0);
    fragColor = inColor;
    fragTexCoord = inTexCoord;
    fragtexSamplerId = model.fragtexSamplerId;*/
}
