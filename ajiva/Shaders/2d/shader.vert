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

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragTexCoord;
layout(location = 2) out uint fragtexSamplerId;

void main() {
    gl_Position = viewProj.view * model.model  * vec4(inPosition, 0.0f, 1.0);
    fragColor = inColor;
    fragTexCoord = inTexCoord;
    fragtexSamplerId = model.fragtexSamplerId;
}
