#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 2) uniform sampler2D texSampler[128];


layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec2 fragTexCoord;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4 (/*fragColor.x +*/ fragTexCoord.x, /*fragColor.y + */fragTexCoord.y, 1.0, 1.0f);
    //outColor = vec4(fragTexCoord, 0.0, 1.0);
}