#[compute]
#version 450

layout(local_size_x = 256, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) buffer Uints{
	uint data[];
} uints;
	

void main() {
	uints.data[gl_LocalInvocationID.x] *= 2;
}