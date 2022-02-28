#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform UniformViewProj {
    mat4 view;
    mat4 proj;
    vec2 mousePos;
    vec2 vec;
} data;


// Instanced attributes
layout (location = 0) in vec4 instancePosCombine;
layout (location = 1) in vec2 instanceRot;
layout (location = 2) in uint instanceTexIndex;
layout (location = 3) in uint instanceDrawType;


layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outUV;
layout(location = 2) out vec2 outPos;

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
    vec2 inPos;
    vec2 inUV;
    if (gl_VertexIndex == 0){
        inPos = instancePosCombine.xy;
        inUV = vec2(0,0);
    } else if (gl_VertexIndex == 1){
        inPos = instancePosCombine.zy;
        inUV = vec2(1,0);
    } else if (gl_VertexIndex == 2){
        inPos = instancePosCombine.zw;
        inUV = vec2(1,1);
    } else if (gl_VertexIndex == 3){
        inPos = instancePosCombine.xy;
        inUV = vec2(0,0);
    } else if (gl_VertexIndex == 4){
        inPos = instancePosCombine.xw;
        inUV = vec2(0,1);
    } else if (gl_VertexIndex == 5){
        inPos = instancePosCombine.zw;
        inUV = vec2(1,1);
    } else {
        inPos = vec2(0,0);
        inUV = vec2(.5,.5);
    }
    
    outPos = inPos;
    //inPos += data.mousePos;
    vec2 rotPos = inPos * rotationX(instanceRot.x) * rotationY(instanceRot.y);
    vec4 pos = vec4(rotPos, 1.0, 1.0);

    gl_Position =  pos;
    outColor = vec3(1,0,1);
    outUV = vec3(inUV, instanceTexIndex);
}
