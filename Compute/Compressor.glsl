#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER_LENGTH = 786432;

const int VERTEX_SIZE = 12;  // 3 vec4 (xyz + w) per vertex
const int NORMAL_SIZE = 12;  // 3 vec4 normals
const int COLOR_SIZE = 16;   // 4 vec4 color values
const int UV_SIZE = 8;       // 2 vec4 UV coordinates
const int COLLISION_SIZE = 18; // 6 vec3 collision vertices



//256 bytes
struct QuadIn {
	//48bytes
	vec4 vertices[3];
	//48bytes
	vec4 color[3];
	//16bytes
	vec4 custom0; // uv, metallicity, emissiveness, transparency
	//12bytes
	float normX;
	float normY;
	float normZ;
	//8 bytes
	bool greedy;
	int next;
	//124 bytes padding
	int padding[31];
};

struct Face {
	int UpQuadIndex;
	int NorthQuadIndex;
	int EastQuadIndex;
	int SouthQuadIndex;
	int WestQuadIndex;
	int DownQuadIndex;
	int transparent;
	int padding;
};

layout(set = 0, binding = 0, std430) buffer vertexbuffer {
	float vertices[MAX_BUFFER_LENGTH];			//0
	float normals[MAX_BUFFER_LENGTH];			//1
	float UV[MAX_BUFFER_LENGTH];				//2
	float colors[MAX_BUFFER_LENGTH];			//3
	float custom0[MAX_BUFFER_LENGTH];			//4
	float CollisionVertices[MAX_BUFFER_LENGTH];	//5
	float EmissiveColor[MAX_BUFFER_LENGTH];		//6
	int indices[MAX_BUFFER_LENGTH];				//7
	//expansion testing
	float data1[MAX_BUFFER_LENGTH];				//8
	float data2[MAX_BUFFER_LENGTH];				//9
	float data3[MAX_BUFFER_LENGTH];				//10
	float data4[MAX_BUFFER_LENGTH];				//11
	
} VertexBuffer;

layout(set = 0, binding = 5, std430) buffer greedybuffer {
	int data[CHUNK_SIZE * 6][CHUNK_SIZE][CHUNK_SIZE];
	
	float vertices[MAX_BUFFER_LENGTH];			//0
	float normals[MAX_BUFFER_LENGTH];			//1
	float UV[MAX_BUFFER_LENGTH];				//2
	float colors[MAX_BUFFER_LENGTH];			//3
	float custom0[MAX_BUFFER_LENGTH];			//4
	float CollisionVertices[MAX_BUFFER_LENGTH];	//5
	float EmissiveColor[MAX_BUFFER_LENGTH];		//6
	int indices[MAX_BUFFER_LENGTH];				//7
	//expansion testing
	float data1[MAX_BUFFER_LENGTH];				//8
	float data2[MAX_BUFFER_LENGTH];				//9
	float data3[MAX_BUFFER_LENGTH];				//10
	float data4[MAX_BUFFER_LENGTH];				//11
	
} GreedyBuffer;

layout(set = 0, binding = 1, std430) buffer quadcount {
	int count;
	int greedycount;
	int test;
	int padding;
} QuadCount;

layout(set = 0, binding = 2, std430) buffer chunkdata{
	int data[CHUNK_SIZE + 2][CHUNK_SIZE + 2][CHUNK_SIZE + 2];
} ChunkData;

layout(set = 0, binding = 3, std430) buffer chunkdimensions{
	int ChunkSize;
	int WorkGroupSide;
} ChunkDimensions;

layout(set = 0, binding = 4, std430) buffer voxeldata{
	QuadIn QuadInput[4000];
	Face FaceData[4000];
} VoxelData;

layout(set = 0, binding = 6, std430) buffer finalbuffer {
	float data[MAX_BUFFER_LENGTH *4];
} FinalBuffer;

void Transfer(int index_to, int index_from) {
	int vertices_offset   = 0;
	int normals_offset    = vertices_offset + VERTEX_SIZE * QuadCount.count;
	int colors_offset     = normals_offset + NORMAL_SIZE * QuadCount.count;
	int uv_offset        = colors_offset + COLOR_SIZE * QuadCount.count;
	int collision_offset = uv_offset + UV_SIZE * QuadCount.count;
	
	FinalBuffer.data[vertices_offset + index_to * 12 + 0] = VertexBuffer.vertices[index_from * 12 + 0]; //0 x
	FinalBuffer.data[vertices_offset + index_to * 12 + 1] = VertexBuffer.vertices[index_from * 12 + 1]; //0 y
	FinalBuffer.data[vertices_offset + index_to * 12 + 2] = VertexBuffer.vertices[index_from * 12 + 2]; //0 z
	FinalBuffer.data[vertices_offset + index_to * 12 + 3] = VertexBuffer.vertices[index_from * 12 + 3]; //0 w
	FinalBuffer.data[vertices_offset + index_to * 12 + 4] = VertexBuffer.vertices[index_from * 12 + 4]; //1 x
	FinalBuffer.data[vertices_offset + index_to * 12 + 5] = VertexBuffer.vertices[index_from * 12 + 5]; //1 y
	FinalBuffer.data[vertices_offset + index_to * 12 + 6] = VertexBuffer.vertices[index_from * 12 + 6]; //1 z
	FinalBuffer.data[vertices_offset + index_to * 12 + 7] = VertexBuffer.vertices[index_from * 12 + 7]; //1 w
	FinalBuffer.data[vertices_offset + index_to * 12 + 8] = VertexBuffer.vertices[index_from * 12 + 8]; //2 x
	FinalBuffer.data[vertices_offset + index_to * 12 + 9] = VertexBuffer.vertices[index_from * 12 + 9]; //2 y
	FinalBuffer.data[vertices_offset + index_to * 12 + 10] = VertexBuffer.vertices[index_from * 12 + 10];//2 z
	FinalBuffer.data[vertices_offset + index_to * 12 + 11] = VertexBuffer.vertices[index_from * 12 + 11];//2 w
	
	/*
	FinalBuffer.data[collision_offset + index_to * 18 + 0] = VertexBuffer.CollisionVertices[index_from * 18 + 0]; 
	FinalBuffer.data[collision_offset + index_to * 18 + 1] = VertexBuffer.CollisionVertices[index_from * 18 + 1];
	FinalBuffer.data[collision_offset + index_to * 18 + 2] = VertexBuffer.CollisionVertices[index_from * 18 + 2];
	FinalBuffer.data[collision_offset + index_to * 18 + 3] = VertexBuffer.CollisionVertices[index_from * 18 + 3];
	FinalBuffer.data[collision_offset + index_to * 18 + 4] = VertexBuffer.CollisionVertices[index_from * 18 + 4];
	FinalBuffer.data[collision_offset + index_to * 18 + 5] = VertexBuffer.CollisionVertices[index_from * 18 + 5];
	FinalBuffer.data[collision_offset + index_to * 18 + 6] = VertexBuffer.CollisionVertices[index_from * 18 + 6];
	FinalBuffer.data[collision_offset + index_to * 18 + 7] = VertexBuffer.CollisionVertices[index_from * 18 + 7];
	FinalBuffer.data[collision_offset + index_to * 18 + 8] = VertexBuffer.CollisionVertices[index_from * 18 + 8];
	FinalBuffer.data[collision_offset + index_to * 18 + 9] = VertexBuffer.CollisionVertices[index_from * 18 + 9];
	FinalBuffer.data[collision_offset + index_to * 18 + 10] = VertexBuffer.CollisionVertices[index_from * 18 + 10];
	FinalBuffer.data[collision_offset + index_to * 18 + 11] = VertexBuffer.CollisionVertices[index_from * 18 + 11];
	FinalBuffer.data[collision_offset + index_to * 18 + 12] = VertexBuffer.CollisionVertices[index_from * 18 + 12];
	FinalBuffer.data[collision_offset + index_to * 18 + 13] = VertexBuffer.CollisionVertices[index_from * 18 + 13];
	FinalBuffer.data[collision_offset + index_to * 18 + 14] = VertexBuffer.CollisionVertices[index_from * 18 + 14];
	FinalBuffer.data[collision_offset + index_to * 18 + 15] = VertexBuffer.CollisionVertices[index_from * 18 + 15]; 
	FinalBuffer.data[collision_offset + index_to * 18 + 16] = VertexBuffer.CollisionVertices[index_from * 18 + 16];
	FinalBuffer.data[collision_offset + index_to * 18 + 17] = VertexBuffer.CollisionVertices[index_from * 18 + 17];

	FinalBuffer.data[uv_offset + index_to * 8 + 0] = VertexBuffer.UV[index_from * 8 + 0];
	FinalBuffer.data[uv_offset + index_to * 8 + 1] = VertexBuffer.UV[index_from * 8 + 1];
	FinalBuffer.data[uv_offset + index_to * 8 + 2] = VertexBuffer.UV[index_from * 8 + 2];
	FinalBuffer.data[uv_offset + index_to * 8 + 3] = VertexBuffer.UV[index_from * 8 + 3];
	FinalBuffer.data[uv_offset + index_to * 8 + 4] = VertexBuffer.UV[index_from * 8 + 4];
	FinalBuffer.data[uv_offset + index_to * 8 + 5] = VertexBuffer.UV[index_from * 8 + 5];
	FinalBuffer.data[uv_offset + index_to * 8 + 6] = VertexBuffer.UV[index_from * 8 + 6];
	FinalBuffer.data[uv_offset + index_to * 8 + 7] = VertexBuffer.UV[index_from * 8 + 7];
	
	FinalBuffer.data[normals_offset + index_to * 12 + 0] = VertexBuffer.normals[index_from * 12 + 0];
	FinalBuffer.data[normals_offset + index_to * 12 + 1] = VertexBuffer.normals[index_from * 12 + 1];
	FinalBuffer.data[normals_offset + index_to * 12 + 2] = VertexBuffer.normals[index_from * 12 + 2];
	FinalBuffer.data[normals_offset + index_to * 12 + 3] = VertexBuffer.normals[index_from * 12 + 3];
	FinalBuffer.data[normals_offset + index_to * 12 + 4] = VertexBuffer.normals[index_from * 12 + 4];
	FinalBuffer.data[normals_offset + index_to * 12 + 5] = VertexBuffer.normals[index_from * 12 + 5];
	FinalBuffer.data[normals_offset + index_to * 12 + 6] = VertexBuffer.normals[index_from * 12 + 6];
	FinalBuffer.data[normals_offset + index_to * 12 + 7] = VertexBuffer.normals[index_from * 12 + 7];
	FinalBuffer.data[normals_offset + index_to * 12 + 8] = VertexBuffer.normals[index_from * 12 + 8];
	FinalBuffer.data[normals_offset + index_to * 12 + 9] = VertexBuffer.normals[index_from * 12 + 9];
	FinalBuffer.data[normals_offset + index_to * 12 + 10] = VertexBuffer.normals[index_from * 12 + 10];
	FinalBuffer.data[normals_offset + index_to * 12 + 11] = VertexBuffer.normals[index_from * 12 + 11];
	
	FinalBuffer.data[colors_offset + index_to * 16 + 0] = VertexBuffer.colors[index_from * 16 + 0];
	FinalBuffer.data[colors_offset + index_to * 16 + 1] = VertexBuffer.colors[index_from * 16 + 1];
	FinalBuffer.data[colors_offset + index_to * 16 + 2] = VertexBuffer.colors[index_from * 16 + 2];
	FinalBuffer.data[colors_offset + index_to * 16 + 3] = VertexBuffer.colors[index_from * 16 + 3];
	FinalBuffer.data[colors_offset + index_to * 16 + 4] = VertexBuffer.colors[index_from * 16 + 4];
	FinalBuffer.data[colors_offset + index_to * 16 + 5] = VertexBuffer.colors[index_from * 16 + 5];
	FinalBuffer.data[colors_offset + index_to * 16 + 6] = VertexBuffer.colors[index_from * 16 + 6];
	FinalBuffer.data[colors_offset + index_to * 16 + 7] = VertexBuffer.colors[index_from * 16 + 7];
	FinalBuffer.data[colors_offset + index_to * 16 + 8] = VertexBuffer.colors[index_from * 16 + 8];
	FinalBuffer.data[colors_offset + index_to * 16 + 9] = VertexBuffer.colors[index_from * 16 + 9];
	FinalBuffer.data[colors_offset + index_to * 16 + 10] = VertexBuffer.colors[index_from * 16 + 10];
	FinalBuffer.data[colors_offset + index_to * 16 + 11] = VertexBuffer.colors[index_from * 16 + 11];
	FinalBuffer.data[colors_offset + index_to * 16 + 12] = VertexBuffer.colors[index_from * 16 + 12];
	FinalBuffer.data[colors_offset + index_to * 16 + 13] = VertexBuffer.colors[index_from * 16 + 13];
	FinalBuffer.data[colors_offset + index_to * 16 + 14] = VertexBuffer.colors[index_from * 16 + 14];
	FinalBuffer.data[colors_offset + index_to * 16 + 15] = VertexBuffer.colors[index_from * 16 + 15];
	*/
}

void main () {
	int index = int(gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * 32 + gl_GlobalInvocationID.z * 32 * 32);
	while (index < QuadCount.count) {
		Transfer(index, index);
		index += 32768;
	}
}
