#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 1) uniform sampler2D texSampler[TEXTURE_SAMPLER_COUNT];

layout(location = 0) in vec2 fragTexCoord;
layout(location = 1) in flat int index;


layout(location = 0) out vec4 outColor;

void main() {
    vec4 color = texture(texSampler[index], fragTexCoord);
    if (color.w < .1) {
        discard;
    } else {
        outColor = color;
    }
}
