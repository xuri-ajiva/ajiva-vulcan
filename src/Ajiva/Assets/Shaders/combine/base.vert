#version 450
#extension GL_ARB_separate_shader_objects : enable

vec2 positions[6] = vec2[](
vec2(0.0, 0.0),
vec2(0.0, 1.0),
vec2(1.0, 0.0),
vec2(0.0, 1.0),
vec2(1.0, 0.0),
vec2(1.0, 1.0)
);

layout(location = 0) out vec2 fragTexCoord;
layout(location = 1) out int index;

void main() {
    fragTexCoord = positions[gl_VertexIndex];
    gl_Position = vec4(fragTexCoord.x*2.0-1.0, fragTexCoord.y*2.0-1.0, 1.0, 1.0);
    index = gl_InstanceIndex;
}
