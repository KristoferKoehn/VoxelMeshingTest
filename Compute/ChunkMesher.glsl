#[compute]
#version 450



layout(local_size_x = 6, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) buffer vertices{
	vec4 data[];
} Vertices;

layout(set = 0, binding = 1, std430) buffer normals{
	vec4 data[];
} Normals;

layout(set = 0, binding = 2, std430) buffer uvs{
	vec4 data[];
} UVs;

layout(set = 0, binding = 3, std430) buffer count{
	uint data;
} Count;

layout(set = 0, binding = 4, std430) buffer chunkData{
	uint data[];
} ChunkData;

void main() {
	Vertices.data[0] = vec4(-0.5f, 0.5f, -0.5f, 0);
	Vertices.data[1] = vec4( 0.5f, 0.5f, -0.5f, 0);
	Vertices.data[2] = vec4( 0.5f, 0.5f,  0.5f, 0);
	Vertices.data[3] = vec4(-0.5f, 0.5f,  0.5f, 0);
	
	vec4 norms = vec4(0, 1, 0, 0);
	Normals.data[0] = norms;
	Normals.data[1] = norms;
	Normals.data[2] = norms;
	Normals.data[3] = norms;
	
	UVs.data[0] = vec4(1,1,0,0);
	UVs.data[1] = vec4(0,1,0,0);
	UVs.data[2] = vec4(0,0,0,0);
	UVs.data[3] = vec4(1,0,0,0);
	
	Count.data = 1;
}

