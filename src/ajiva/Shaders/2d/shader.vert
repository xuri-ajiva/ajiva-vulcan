#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
    vec2 mousePos;
    vec2 vec;
} data;
layout(binding = 1) uniform UniformModel {
    mat4 model;
} model;

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragTexCoord;

void main() {
    vec4 translated = model.model * vec4(inPosition,1,1);
    vec4 pos = data.proj * data.view * (translated + vec4(data.mousePos, 1.0f, 1.0f));
    gl_Position =  vec4(vec2(pos), 0, 1);
    fragColor = inColor;
    fragTexCoord = inTexCoord + data.mousePos;
}
