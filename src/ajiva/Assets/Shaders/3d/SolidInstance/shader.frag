#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 2) uniform sampler2D texSampler[TEXTURE_SAMPLER_COUNT];


layout (location = 0) in vec3 inColor;
layout (location = 1) in vec3 inUV;

layout(location = 0) out vec4 outColor;

void main() {

    /*    vec4 color = texture(samplerArray, inUV) * vec4(inColor, 1.0);
        vec3 N = normalize(inNormal);
        vec3 L = normalize(inLightVec);
        vec3 V = normalize(inViewVec);
        vec3 R = reflect(-L, N);
        vec3 diffuse = max(dot(N, L), 0.1) * inColor;
        vec3 specular = (dot(N,L) > 0.0) ? pow(max(dot(R, V), 0.0), 16.0) * vec3(0.75) * color.r : vec3(0.0);
        outFragColor = vec4(diffuse * color.rgb + specular, 1.0);      */

    outColor =  texture(texSampler[int(inUV.z)], inUV.xy) * vec4 (inColor, 1.0f);
    //outColor = vec4(fragTexCoord, 0.0, 1.0);
}
