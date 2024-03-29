#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 2) uniform sampler2D texSampler[TEXTURE_SAMPLER_COUNT];

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec2 fragTexCoord;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4 (fragColor + texture(texSampler[0], fragTexCoord * 2.0f).rgb, 1.0f);
}
