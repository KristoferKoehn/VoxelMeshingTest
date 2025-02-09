#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER_LENGTH = 786432;
const float AOVAL = 0.6;

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

layout(set = 0, binding = 1, std430) buffer quadcount {
	int count;
	int greedycount;
	int test;
	int padding;
} QuadCount;

layout(set = 0, binding = 6, std430) buffer finalbuffer {
	float vertices[MAX_BUFFER_LENGTH];			//0 12 floats
	float normals[MAX_BUFFER_LENGTH];			//1 12 floats
	float UV[MAX_BUFFER_LENGTH];				//2 8 floats
	float colors[MAX_BUFFER_LENGTH];			//3 16 floats
	float custom0[MAX_BUFFER_LENGTH];			//4 16 floats
	float CollisionVertices[MAX_BUFFER_LENGTH];	//5 18 floats
	float EmissiveColor[MAX_BUFFER_LENGTH];		//6 16 floats
	int indices[MAX_BUFFER_LENGTH];				//7 6 ints
	//expansion testing
	float data1[MAX_BUFFER_LENGTH];				//8
	float data2[MAX_BUFFER_LENGTH];				//9
	float data3[MAX_BUFFER_LENGTH];				//10
	float data4[MAX_BUFFER_LENGTH];				//11
} FinalBuffer;


/*

vertices = 12
vertices * count = normals_offset
normals = 12
normals * count + normals_offset = uv_offset
uvs = 8
uvs * count + normals_offset + uv_offset = custom0_offset
collision * count + custom0_offset + normals_offset + uv_offset = collision_offset
emissive = 16
emissive * count + collision_offset + custom0_offset + normals_offset + uv_offset = index_offset

*/

layout(set = 0, binding = 2, std430) buffer chunkdata{
	int data[CHUNK_SIZE + 2][CHUNK_SIZE + 2][CHUNK_SIZE + 2];
} ChunkData;

layout(set = 0, binding = 3, std430) buffer chunkdimensions{
	int ChunkSize;
	int WorkGroupSide;
} ChunkDimensions;

/*
void GreedyTransfer(int greedyTicket) {
	if (greedyTicket == 0) return;

	int IndexTicket = atomicAdd(QuadCount.count, 1);
	
	VertexBuffer.vertices[IndexTicket * 12 + 0] =  GreedyBuffer.vertices[greedyTicket * 12 + 0]; //0 x
	VertexBuffer.vertices[IndexTicket * 12 + 1] =  GreedyBuffer.vertices[greedyTicket * 12 + 1]; //0 y
	VertexBuffer.vertices[IndexTicket * 12 + 2] =  GreedyBuffer.vertices[greedyTicket * 12 + 2]; //0 z
	VertexBuffer.vertices[IndexTicket * 12 + 3] =  GreedyBuffer.vertices[greedyTicket * 12 + 3]; //0 w
	VertexBuffer.vertices[IndexTicket * 12 + 4] =  GreedyBuffer.vertices[greedyTicket * 12 + 4]; //1 x
	VertexBuffer.vertices[IndexTicket * 12 + 5] =  GreedyBuffer.vertices[greedyTicket * 12 + 5]; //1 y
	VertexBuffer.vertices[IndexTicket * 12 + 6] =  GreedyBuffer.vertices[greedyTicket * 12 + 6]; //1 z
	VertexBuffer.vertices[IndexTicket * 12 + 7] =  GreedyBuffer.vertices[greedyTicket * 12 + 7]; //1 w
	VertexBuffer.vertices[IndexTicket * 12 + 8] =  GreedyBuffer.vertices[greedyTicket * 12 + 8]; //2 x
	VertexBuffer.vertices[IndexTicket * 12 + 9] =  GreedyBuffer.vertices[greedyTicket * 12 + 9]; //2 y
	VertexBuffer.vertices[IndexTicket * 12 + 10] = GreedyBuffer.vertices[greedyTicket * 12 + 10];//2 z
	VertexBuffer.vertices[IndexTicket * 12 + 11] = GreedyBuffer.vertices[greedyTicket * 12 + 11];//2 w
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 0]  = GreedyBuffer.vertices[greedyTicket * 12 + 0]; 
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 1]  = GreedyBuffer.vertices[greedyTicket * 12 + 1];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 2]  = GreedyBuffer.vertices[greedyTicket * 12 + 2];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 3]  = GreedyBuffer.vertices[greedyTicket * 12 + 3];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 4]  = GreedyBuffer.vertices[greedyTicket * 12 + 4];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 5]  = GreedyBuffer.vertices[greedyTicket * 12 + 5];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 6]  = GreedyBuffer.vertices[greedyTicket * 12 + 6];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 7]  = GreedyBuffer.vertices[greedyTicket * 12 + 7];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 8]  = GreedyBuffer.vertices[greedyTicket * 12 + 8];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 9]  = GreedyBuffer.vertices[greedyTicket * 12 + 0];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 10] = GreedyBuffer.vertices[greedyTicket * 12 + 1];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 11] = GreedyBuffer.vertices[greedyTicket * 12 + 2];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 12] = GreedyBuffer.vertices[greedyTicket * 12 + 6];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 13] = GreedyBuffer.vertices[greedyTicket * 12 + 7];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 14] = GreedyBuffer.vertices[greedyTicket * 12 + 8];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 15] = GreedyBuffer.vertices[greedyTicket * 12 + 9]; 
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 16] = GreedyBuffer.vertices[greedyTicket * 12 + 10];
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 17] = GreedyBuffer.vertices[greedyTicket * 12 + 11];
	
	VertexBuffer.UV[IndexTicket * 8 + 0] = GreedyBuffer.UV[greedyTicket * 8 + 0];
	VertexBuffer.UV[IndexTicket * 8 + 1] = GreedyBuffer.UV[greedyTicket * 8 + 1];
	VertexBuffer.UV[IndexTicket * 8 + 2] = GreedyBuffer.UV[greedyTicket * 8 + 2];
	VertexBuffer.UV[IndexTicket * 8 + 3] = GreedyBuffer.UV[greedyTicket * 8 + 3];
	VertexBuffer.UV[IndexTicket * 8 + 4] = GreedyBuffer.UV[greedyTicket * 8 + 4];
	VertexBuffer.UV[IndexTicket * 8 + 5] = GreedyBuffer.UV[greedyTicket * 8 + 5];
	VertexBuffer.UV[IndexTicket * 8 + 6] = GreedyBuffer.UV[greedyTicket * 8 + 6];
	VertexBuffer.UV[IndexTicket * 8 + 7] = GreedyBuffer.UV[greedyTicket * 8 + 7];
	
	VertexBuffer.normals[IndexTicket * 12 + 0] = GreedyBuffer.normals[greedyTicket * 12 + 0];
	VertexBuffer.normals[IndexTicket * 12 + 1] = GreedyBuffer.normals[greedyTicket * 12 + 1];
	VertexBuffer.normals[IndexTicket * 12 + 2] = GreedyBuffer.normals[greedyTicket * 12 + 2];
	VertexBuffer.normals[IndexTicket * 12 + 3] = GreedyBuffer.normals[greedyTicket * 12 + 3];
	VertexBuffer.normals[IndexTicket * 12 + 4] = GreedyBuffer.normals[greedyTicket * 12 + 4];
	VertexBuffer.normals[IndexTicket * 12 + 5] = GreedyBuffer.normals[greedyTicket * 12 + 5];
	VertexBuffer.normals[IndexTicket * 12 + 6] = GreedyBuffer.normals[greedyTicket * 12 + 6];
	VertexBuffer.normals[IndexTicket * 12 + 7] = GreedyBuffer.normals[greedyTicket * 12 + 7];
	VertexBuffer.normals[IndexTicket * 12 + 8] = GreedyBuffer.normals[greedyTicket * 12 + 8];
	VertexBuffer.normals[IndexTicket * 12 + 9] = GreedyBuffer.normals[greedyTicket * 12 + 9];
	VertexBuffer.normals[IndexTicket * 12 + 10]= GreedyBuffer.normals[greedyTicket * 12 + 10];
	VertexBuffer.normals[IndexTicket * 12 + 11]= GreedyBuffer.normals[greedyTicket * 12 + 11];
	
	VertexBuffer.colors[IndexTicket * 16 + 0] =  GreedyBuffer.colors[greedyTicket * 16 + 0];
	VertexBuffer.colors[IndexTicket * 16 + 1] =  GreedyBuffer.colors[greedyTicket * 16 + 1];
	VertexBuffer.colors[IndexTicket * 16 + 2] =  GreedyBuffer.colors[greedyTicket * 16 + 2];
	VertexBuffer.colors[IndexTicket * 16 + 3] =  GreedyBuffer.colors[greedyTicket * 16 + 3];
	VertexBuffer.colors[IndexTicket * 16 + 4] =  GreedyBuffer.colors[greedyTicket * 16 + 4];
	VertexBuffer.colors[IndexTicket * 16 + 5] =  GreedyBuffer.colors[greedyTicket * 16 + 5];
	VertexBuffer.colors[IndexTicket * 16 + 6] =  GreedyBuffer.colors[greedyTicket * 16 + 6];
	VertexBuffer.colors[IndexTicket * 16 + 7] =  GreedyBuffer.colors[greedyTicket * 16 + 7];
	VertexBuffer.colors[IndexTicket * 16 + 8] =  GreedyBuffer.colors[greedyTicket * 16 + 8];
	VertexBuffer.colors[IndexTicket * 16 + 9] =  GreedyBuffer.colors[greedyTicket * 16 + 9];
	VertexBuffer.colors[IndexTicket * 16 + 10] = GreedyBuffer.colors[greedyTicket * 16 + 10];
	VertexBuffer.colors[IndexTicket * 16 + 11] = GreedyBuffer.colors[greedyTicket * 16 + 11];
	VertexBuffer.colors[IndexTicket * 16 + 12] = GreedyBuffer.colors[greedyTicket * 16 + 12];
	VertexBuffer.colors[IndexTicket * 16 + 13] = GreedyBuffer.colors[greedyTicket * 16 + 13];
	VertexBuffer.colors[IndexTicket * 16 + 14] = GreedyBuffer.colors[greedyTicket * 16 + 14];
	VertexBuffer.colors[IndexTicket * 16 + 15] = GreedyBuffer.colors[greedyTicket * 16 + 15];
	
	VertexBuffer.indices[IndexTicket * 6 + 0] = IndexTicket * 4 + 0;  
	VertexBuffer.indices[IndexTicket * 6 + 1] = IndexTicket * 4 + 1;
	VertexBuffer.indices[IndexTicket * 6 + 2] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 3] = IndexTicket * 4 + 0;
	VertexBuffer.indices[IndexTicket * 6 + 4] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 5] = IndexTicket * 4 + 3;
} */

void main () {
	
}

