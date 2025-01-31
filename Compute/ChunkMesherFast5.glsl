#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER_LENGTH = 786432;
const float AOVAL = 0.6;

const int NorthWest = 2;
const int NorthEast = 3;
const int SouthEast = 0;
const int SouthWest = 1;

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

vec4[3] AddPosition(vec4[3] vert, vec3 pos) {
	vert[0] = vert[0] + pos.xyzx;
	vert[1] = vert[1] + pos.yzxy;
	vert[2] = vert[2] + pos.zxyz;
	return vert;
}

void ApplyUV(float UVIndex, int IndexTicket) {
	vec2 start = vec2( mod(int(UVIndex), 32), int(UVIndex) / 32) * 1.0/32.0;
	vec2 finish = start + vec2(1.0/32.0, 1.0/32.0);
	VertexBuffer.UV[IndexTicket * 8 + 0] = finish.x;
	VertexBuffer.UV[IndexTicket * 8 + 1] = finish.y;
	VertexBuffer.UV[IndexTicket * 8 + 2] = start.x;
	VertexBuffer.UV[IndexTicket * 8 + 3] = finish.y;
	VertexBuffer.UV[IndexTicket * 8 + 4] = start.x;
	VertexBuffer.UV[IndexTicket * 8 + 5] = start.y;
	VertexBuffer.UV[IndexTicket * 8 + 6] = finish.x;
	VertexBuffer.UV[IndexTicket * 8 + 7] = start.y;
}

void ApplyGreedyUV(float UVIndex, int IndexTicket) {
	vec2 start = vec2( mod(int(UVIndex), 32), int(UVIndex) / 32) * 1.0/32.0;
	vec2 finish = start + vec2(1.0/32.0, 1.0/32.0);
	GreedyBuffer.UV[IndexTicket * 8 + 0] = finish.x;
	GreedyBuffer.UV[IndexTicket * 8 + 1] = finish.y;
	GreedyBuffer.UV[IndexTicket * 8 + 2] = start.x;
	GreedyBuffer.UV[IndexTicket * 8 + 3] = finish.y;
	GreedyBuffer.UV[IndexTicket * 8 + 4] = start.x;
	GreedyBuffer.UV[IndexTicket * 8 + 5] = start.y;
	GreedyBuffer.UV[IndexTicket * 8 + 6] = finish.x;
	GreedyBuffer.UV[IndexTicket * 8 + 7] = start.y;
}

void ApplyIndices(int IndexTicket) {
	VertexBuffer.indices[IndexTicket * 6 + 0] = IndexTicket * 4 + 0;  
	VertexBuffer.indices[IndexTicket * 6 + 1] = IndexTicket * 4 + 1;
	VertexBuffer.indices[IndexTicket * 6 + 2] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 3] = IndexTicket * 4 + 0;
	VertexBuffer.indices[IndexTicket * 6 + 4] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 5] = IndexTicket * 4 + 3;
}

void CollisionMeshAssign(vec4[3] vertices, int IndexTicket) {
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 0]  = vertices[0].x;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 1]  = vertices[0].y;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 2]  = vertices[0].z;
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 3]  = vertices[0].w;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 4]  = vertices[1].x;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 5]  = vertices[1].y;
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 6]  = vertices[1].z;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 7]  = vertices[1].w;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 8]  = vertices[2].x;
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 9]  = vertices[0].x;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 10] = vertices[0].y;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 11] = vertices[0].z;
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 12] = vertices[1].z;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 13] = vertices[1].w;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 14] = vertices[2].x;
	
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 15] = vertices[2].y;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 16] = vertices[2].z;
	VertexBuffer.CollisionVertices[IndexTicket * 18 + 17] = vertices[2].w;
}

void ApplyVertices(vec4 vertices[3], int index) {
	int rIndex = index * 12;
	VertexBuffer.vertices[rIndex + 0] = vertices[0].x;
	VertexBuffer.vertices[rIndex + 1] = vertices[0].y;
	VertexBuffer.vertices[rIndex + 2] = vertices[0].z;
	VertexBuffer.vertices[rIndex + 3] = vertices[0].w;
	
	VertexBuffer.vertices[rIndex + 4] = vertices[1].x;
	VertexBuffer.vertices[rIndex + 5] = vertices[1].y;
	VertexBuffer.vertices[rIndex + 6] = vertices[1].z;
	VertexBuffer.vertices[rIndex + 7] = vertices[1].w;
	
	VertexBuffer.vertices[rIndex + 8] = vertices[2].x;
	VertexBuffer.vertices[rIndex + 9] = vertices[2].y;
	VertexBuffer.vertices[rIndex + 10] = vertices[2].z;
	VertexBuffer.vertices[rIndex + 11] = vertices[2].w;
}

void ApplyGreedyVertices(vec4 vertices[3], int index) {
	int rIndex = index * 12;
	GreedyBuffer.vertices[rIndex + 0] = vertices[0].x;
	GreedyBuffer.vertices[rIndex + 1] = vertices[0].y;
	GreedyBuffer.vertices[rIndex + 2] = vertices[0].z;
	GreedyBuffer.vertices[rIndex + 3] = vertices[0].w;

	GreedyBuffer.vertices[rIndex + 4] = vertices[1].x;
	GreedyBuffer.vertices[rIndex + 5] = vertices[1].y;
	GreedyBuffer.vertices[rIndex + 6] = vertices[1].z;
	GreedyBuffer.vertices[rIndex + 7] = vertices[1].w;

	GreedyBuffer.vertices[rIndex + 8] = vertices[2].x;
	GreedyBuffer.vertices[rIndex + 9] = vertices[2].y;
	GreedyBuffer.vertices[rIndex + 10] = vertices[2].z;
	GreedyBuffer.vertices[rIndex + 11] = vertices[2].w;
}


void ApplyColor(vec4 vertices[3], int index) {
	int rIndex = index * 16;
	VertexBuffer.colors[rIndex + 0] = vertices[0].x;
	VertexBuffer.colors[rIndex + 1] = vertices[0].y;
	VertexBuffer.colors[rIndex + 2] = vertices[0].z;
	VertexBuffer.colors[rIndex + 3] = 1.0;

	VertexBuffer.colors[rIndex + 4] = vertices[0].w;
	VertexBuffer.colors[rIndex + 5] = vertices[1].x;
	VertexBuffer.colors[rIndex + 6] = vertices[1].y;
	VertexBuffer.colors[rIndex + 7] = 1.0;

	VertexBuffer.colors[rIndex + 8] = vertices[1].z;
	VertexBuffer.colors[rIndex + 9] = vertices[1].w;
	VertexBuffer.colors[rIndex + 10] = vertices[2].x;
	VertexBuffer.colors[rIndex + 11] = 1.0;
	
	VertexBuffer.colors[rIndex + 12] = vertices[2].y;
	VertexBuffer.colors[rIndex + 13] = vertices[2].z;
	VertexBuffer.colors[rIndex + 14] = vertices[2].w;
	VertexBuffer.colors[rIndex + 15] = 1.0;
}

void ApplyGreedyColor(vec4 vertices[3], int index) {
	int rIndex = index * 16;
	GreedyBuffer.colors[rIndex + 0] = vertices[0].x;
	GreedyBuffer.colors[rIndex + 1] = vertices[0].y;
	GreedyBuffer.colors[rIndex + 2] = vertices[0].z;
	GreedyBuffer.colors[rIndex + 3] = 1.0;

	GreedyBuffer.colors[rIndex + 4] = vertices[0].w;
	GreedyBuffer.colors[rIndex + 5] = vertices[1].x;
	GreedyBuffer.colors[rIndex + 6] = vertices[1].y;
	GreedyBuffer.colors[rIndex + 7] = 1.0;

	GreedyBuffer.colors[rIndex + 8] = vertices[1].z;
	GreedyBuffer.colors[rIndex + 9] = vertices[1].w;
	GreedyBuffer.colors[rIndex + 10] = vertices[2].x;
	GreedyBuffer.colors[rIndex + 11] = 1.0;

	GreedyBuffer.colors[rIndex + 12] = vertices[2].y;
	GreedyBuffer.colors[rIndex + 13] = vertices[2].z;
	GreedyBuffer.colors[rIndex + 14] = vertices[2].w;
	GreedyBuffer.colors[rIndex + 15] = 1.0;
}

void AODimmer(int index, int dir) {
	int rIndex = index * 16;
	VertexBuffer.colors[rIndex + dir * 4 + 0] = VertexBuffer.colors[rIndex + dir * 4 + 0] * AOVAL;
	VertexBuffer.colors[rIndex + dir * 4 + 1] = VertexBuffer.colors[rIndex + dir * 4 + 1] * AOVAL;
	VertexBuffer.colors[rIndex + dir * 4 + 2] = VertexBuffer.colors[rIndex + dir * 4 + 2] * AOVAL;
}

void AOGreedyDimmer(int index, int dir) {
	int rIndex = index * 16;
	GreedyBuffer.colors[rIndex + dir * 4 + 0] = GreedyBuffer.colors[rIndex + dir * 4 + 0] * AOVAL;
	GreedyBuffer.colors[rIndex + dir * 4 + 1] = GreedyBuffer.colors[rIndex + dir * 4 + 1] * AOVAL;
	GreedyBuffer.colors[rIndex + dir * 4 + 2] = GreedyBuffer.colors[rIndex + dir * 4 + 2] * AOVAL;
}

void ApplyNormals(vec4 normals[3], int index) {
	int rIndex = index * 12;
	VertexBuffer.normals[rIndex + 0] = normals[0].x;
	VertexBuffer.normals[rIndex + 1] = normals[0].y;
	VertexBuffer.normals[rIndex + 2] = normals[0].z;
	VertexBuffer.normals[rIndex + 3] = normals[0].x;
	
	VertexBuffer.normals[rIndex + 4] = normals[0].y;
	VertexBuffer.normals[rIndex + 5] = normals[0].z;
	VertexBuffer.normals[rIndex + 6] = normals[0].x;
	VertexBuffer.normals[rIndex + 7] = normals[0].y;
	
	VertexBuffer.normals[rIndex + 8] = normals[0].z;
	VertexBuffer.normals[rIndex + 9] = normals[0].x;
	VertexBuffer.normals[rIndex + 10] = normals[0].y;
	VertexBuffer.normals[rIndex + 11] = normals[0].z;
}

void ApplyGreedyNormals(vec4 normals[3], int index) {
	int rIndex = index * 12;
	GreedyBuffer.normals[rIndex + 0] = normals[0].x;
	GreedyBuffer.normals[rIndex + 1] = normals[0].y;
	GreedyBuffer.normals[rIndex + 2] = normals[0].z;
	GreedyBuffer.normals[rIndex + 3] = normals[0].x;

	GreedyBuffer.normals[rIndex + 4] = normals[0].y;
	GreedyBuffer.normals[rIndex + 5] = normals[0].z;
	GreedyBuffer.normals[rIndex + 6] = normals[0].x;
	GreedyBuffer.normals[rIndex + 7] = normals[0].y;

	GreedyBuffer.normals[rIndex + 8] = normals[0].z;
	GreedyBuffer.normals[rIndex + 9] = normals[0].x;
	GreedyBuffer.normals[rIndex + 10] = normals[0].y;
	GreedyBuffer.normals[rIndex + 11] = normals[0].z;
}

void FlipColor(int index) {
	int rIndex = index * 16;
	float temp1 = VertexBuffer.colors[rIndex + 0];
	float temp2 = VertexBuffer.colors[rIndex + 1];
	float temp3 = VertexBuffer.colors[rIndex + 2];
	float temp4 = VertexBuffer.colors[rIndex + 3];
	
	VertexBuffer.colors[rIndex + 0] =  VertexBuffer.colors[rIndex + 4];
	VertexBuffer.colors[rIndex + 1] =  VertexBuffer.colors[rIndex + 5];
	VertexBuffer.colors[rIndex + 2] =  VertexBuffer.colors[rIndex + 6];
	VertexBuffer.colors[rIndex + 3] =  VertexBuffer.colors[rIndex + 7];

	VertexBuffer.colors[rIndex + 4] =  VertexBuffer.colors[rIndex + 8 ];
	VertexBuffer.colors[rIndex + 5] =  VertexBuffer.colors[rIndex + 9 ];
	VertexBuffer.colors[rIndex + 6] =  VertexBuffer.colors[rIndex + 10];
	VertexBuffer.colors[rIndex + 7] =  VertexBuffer.colors[rIndex + 11];
	
	VertexBuffer.colors[rIndex + 8 ] = VertexBuffer.colors[rIndex + 12];
	VertexBuffer.colors[rIndex + 9 ] = VertexBuffer.colors[rIndex + 13];
	VertexBuffer.colors[rIndex + 10] = VertexBuffer.colors[rIndex + 14];
	VertexBuffer.colors[rIndex + 11] = VertexBuffer.colors[rIndex + 15];

	VertexBuffer.colors[rIndex + 12] = temp1;
	VertexBuffer.colors[rIndex + 13] = temp2;
	VertexBuffer.colors[rIndex + 14] = temp3;
	VertexBuffer.colors[rIndex + 15] = temp4;
}

void FlipGreedyColor(int index) {
	int rIndex = index * 16;
	float temp1 = GreedyBuffer.colors[rIndex + 0];
	float temp2 = GreedyBuffer.colors[rIndex + 1];
	float temp3 = GreedyBuffer.colors[rIndex + 2];
	float temp4 = GreedyBuffer.colors[rIndex + 3];
	
	GreedyBuffer.colors[rIndex + 0] =  GreedyBuffer.colors[rIndex + 4];
	GreedyBuffer.colors[rIndex + 1] =  GreedyBuffer.colors[rIndex + 5];
	GreedyBuffer.colors[rIndex + 2] =  GreedyBuffer.colors[rIndex + 6];
	GreedyBuffer.colors[rIndex + 3] =  GreedyBuffer.colors[rIndex + 7];

	GreedyBuffer.colors[rIndex + 4] =  GreedyBuffer.colors[rIndex + 8 ];
	GreedyBuffer.colors[rIndex + 5] =  GreedyBuffer.colors[rIndex + 9 ];
	GreedyBuffer.colors[rIndex + 6] =  GreedyBuffer.colors[rIndex + 10];
	GreedyBuffer.colors[rIndex + 7] =  GreedyBuffer.colors[rIndex + 11];

	GreedyBuffer.colors[rIndex + 8 ] = GreedyBuffer.colors[rIndex + 12];
	GreedyBuffer.colors[rIndex + 9 ] = GreedyBuffer.colors[rIndex + 13];
	GreedyBuffer.colors[rIndex + 10] = GreedyBuffer.colors[rIndex + 14];
	GreedyBuffer.colors[rIndex + 11] = GreedyBuffer.colors[rIndex + 15];

	GreedyBuffer.colors[rIndex + 12] = temp1;
	GreedyBuffer.colors[rIndex + 13] = temp2;
	GreedyBuffer.colors[rIndex + 14] = temp3;
	GreedyBuffer.colors[rIndex + 15] = temp4;
}

void FlipUV(int index) {
	float temp1 = VertexBuffer.UV[index * 8 + 0];
	float temp2 = VertexBuffer.UV[index * 8 + 1];
	VertexBuffer.UV[index * 8 + 0] = VertexBuffer.UV[index * 8 + 2];
	VertexBuffer.UV[index * 8 + 1] = VertexBuffer.UV[index * 8 + 3];	
	VertexBuffer.UV[index * 8 + 2] = VertexBuffer.UV[index * 8 + 4];
	VertexBuffer.UV[index * 8 + 3] = VertexBuffer.UV[index * 8 + 5];	
	VertexBuffer.UV[index * 8 + 4] = VertexBuffer.UV[index * 8 + 6];	
	VertexBuffer.UV[index * 8 + 5] = VertexBuffer.UV[index * 8 + 7];
	VertexBuffer.UV[index * 8 + 6] = temp1;	
	VertexBuffer.UV[index * 8 + 7] = temp2;
}

void FlipGreedyUV(int index) {
	float temp1 = VertexBuffer.UV[index * 8 + 0];
	float temp2 = VertexBuffer.UV[index * 8 + 1];
	GreedyBuffer.UV[index * 8 + 0] = GreedyBuffer.UV[index * 8 + 2];
	GreedyBuffer.UV[index * 8 + 1] = GreedyBuffer.UV[index * 8 + 3];	
	GreedyBuffer.UV[index * 8 + 2] = GreedyBuffer.UV[index * 8 + 4];
	GreedyBuffer.UV[index * 8 + 3] = GreedyBuffer.UV[index * 8 + 5];	
	GreedyBuffer.UV[index * 8 + 4] = GreedyBuffer.UV[index * 8 + 6];	
	GreedyBuffer.UV[index * 8 + 5] = GreedyBuffer.UV[index * 8 + 7];
	GreedyBuffer.UV[index * 8 + 6] = temp1;	
	GreedyBuffer.UV[index * 8 + 7] = temp2;
}

void FlipVertex(int index) {
	int rIndex = index * 12;
	float temp1 = VertexBuffer.vertices[rIndex + 0];
	float temp2 = VertexBuffer.vertices[rIndex + 1];
	float temp3 = VertexBuffer.vertices[rIndex + 2];
	
	VertexBuffer.vertices[rIndex + 0] = VertexBuffer.vertices[rIndex + 3];
	VertexBuffer.vertices[rIndex + 1] = VertexBuffer.vertices[rIndex + 4];
	VertexBuffer.vertices[rIndex + 2] = VertexBuffer.vertices[rIndex + 5];
	
	VertexBuffer.vertices[rIndex + 3] = VertexBuffer.vertices[rIndex + 6];
	VertexBuffer.vertices[rIndex + 4] = VertexBuffer.vertices[rIndex + 7];
	VertexBuffer.vertices[rIndex + 5] = VertexBuffer.vertices[rIndex + 8];

	VertexBuffer.vertices[rIndex + 6] = VertexBuffer.vertices[rIndex + 9];
	VertexBuffer.vertices[rIndex + 7] = VertexBuffer.vertices[rIndex + 10];
	VertexBuffer.vertices[rIndex + 8] = VertexBuffer.vertices[rIndex + 11];
	
	VertexBuffer.vertices[rIndex + 9]  = temp1;
	VertexBuffer.vertices[rIndex + 10] = temp2;
	VertexBuffer.vertices[rIndex + 11] = temp3;
}

void FlipGreedyVertex(int index) {
	int rIndex = index * 12;
	float temp1 = GreedyBuffer.vertices[rIndex + 0];
	float temp2 = GreedyBuffer.vertices[rIndex + 1];
	float temp3 = GreedyBuffer.vertices[rIndex + 2];
	
	GreedyBuffer.vertices[rIndex + 0] = GreedyBuffer.vertices[rIndex + 3];
	GreedyBuffer.vertices[rIndex + 1] = GreedyBuffer.vertices[rIndex + 4];
	GreedyBuffer.vertices[rIndex + 2] = GreedyBuffer.vertices[rIndex + 5];

	GreedyBuffer.vertices[rIndex + 3] = GreedyBuffer.vertices[rIndex + 6];
	GreedyBuffer.vertices[rIndex + 4] = GreedyBuffer.vertices[rIndex + 7];
	GreedyBuffer.vertices[rIndex + 5] = GreedyBuffer.vertices[rIndex + 8];

	GreedyBuffer.vertices[rIndex + 6] = GreedyBuffer.vertices[rIndex + 9];
	GreedyBuffer.vertices[rIndex + 7] = GreedyBuffer.vertices[rIndex + 10];
	GreedyBuffer.vertices[rIndex + 8] = GreedyBuffer.vertices[rIndex + 11];

	GreedyBuffer.vertices[rIndex + 9]  = temp1;
	GreedyBuffer.vertices[rIndex + 10] = temp2;
	GreedyBuffer.vertices[rIndex + 11] = temp3;
}

void FlipFace(int index) {
	FlipVertex(index);
	FlipUV(index);
	FlipColor(index);
}

void FlipGreedyFace(int index) {
	FlipGreedyVertex(index);
	FlipGreedyUV(index);
	FlipGreedyColor(index);
}

void AOUpFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 1, 0);
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) { //west 1, 0, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}

		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) { //north 0, 0, 1
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		
		Northwest = true;
		Northeast = true;
	} 
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}

		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}

		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	}
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
			if (greedy) {
				FlipGreedyFace(index);
			} else {
				FlipFace(index);
			}
		}
	}
}

void AODownFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, -1, 0);
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) { //west -1, 0, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) { //north 0, 0, 1
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}

		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	}
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
			if (greedy) {
				FlipGreedyFace(index);
			} else {
				FlipFace(index);
			}
		}
	}
	
}

void AONorthFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 0, -1);
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) { //1, 0, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) { //0, 1, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)- 1][int(targpos.z) ] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	} 
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (greedy) {
			FlipGreedyFace(index);
		} else {
			FlipFace(index);
		}
	}
}

void AOEastFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(-1, 0, 0);
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) { // 0, 0, -1
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) { // 0, 1, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)- 1][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	}
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (greedy) {
			FlipGreedyFace(index);
		} else {
			FlipFace(index);
		}
	}
}

void AOSouthFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 0, 1);
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) { //-1, 0, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) { //0, 1, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)- 1][int(targpos.z) ] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	}
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (greedy) {
			FlipGreedyFace(index);
		} else {
			FlipFace(index);
		}
	}
}

void AOWestFace(int index, vec3 voxelPos, bool greedy) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(1, 0, 0);
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) { //0, 0, 1
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, SouthWest);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, SouthWest);
		}
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) { // 0, 1, 0
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);
			AOGreedyDimmer(index, NorthEast);
		} else {
			AODimmer(index, NorthWest);
			AODimmer(index, NorthEast);
		}
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, NorthEast);
			AODimmer(index, SouthEast);		
		}
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);
			AOGreedyDimmer(index, SouthWest);		
		} else {
			AODimmer(index, SouthEast);
			AODimmer(index, SouthWest);		
		}
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthWest);	
		} else {
			AODimmer(index, NorthWest);
		}
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)- 1][int(targpos.z)  - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthEast);	
		} else {
			AODimmer(index, SouthEast);
		}
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) - 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, NorthEast);	
		} else {
			AODimmer(index, NorthEast);
		}
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z) + 1] != 0) {
		if (greedy) {
			AOGreedyDimmer(index, SouthWest);	
		} else {
			AODimmer(index, SouthWest);
		}
		Southwest = true;
	}
	
	if (int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		if (greedy) {
			FlipGreedyFace(index);
		} else {
			FlipFace(index);
		}
	}
}

//0 up, 1 north, 2 east, 3 south, 4 west, 5 down
void GreedyData(QuadIn q, int face, int blocktype, vec3 position) {
	int IndexTicket = atomicAdd(QuadCount.greedycount, 1) + 1;
	ApplyGreedyVertices(AddPosition( q.vertices, vec3(position.x - ChunkDimensions.ChunkSize/2 - 1, position.y - ChunkDimensions.ChunkSize/2 - 1, position.z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
	GreedyBuffer.data[int(position.x - 1) + face * CHUNK_SIZE][int(position.y - 1)][int(position.z - 1)] = IndexTicket;
	GreedyBuffer.data1[IndexTicket] = blocktype;
	vec4 n = vec4(q.normX, q.normY, q.normZ, blocktype);
	vec4[3] norm;
	norm[0] = n;
	norm[1] = n;
	norm[2] = n;
	ApplyGreedyNormals(norm, IndexTicket);
	ApplyGreedyColor(q.color, IndexTicket);
	ApplyGreedyUV(q.custom0.x, IndexTicket);
	
	switch (face) {
		case 0:
			AOUpFace(IndexTicket, position, true);		
			break;
		case 1:
			AONorthFace(IndexTicket, position, true);
			break;
		case 2:
			AOEastFace(IndexTicket, position, true);
			break;
		case 3:
			AOSouthFace(IndexTicket, position, true);		
			break;
		case 4:
			AOWestFace(IndexTicket, position, true);			
			break;
		case 5:
			AODownFace(IndexTicket, position, true);			
			break;
	} 
}

/*
on each face, check if greedy. if so, call the greedy data function


detangle greedying from the base mesher

	-greedy data function -DONE with current feature set.
 		-keeps track of all the faces (same as indexticket)
		-puts the data in the greedy version of the buffer
		-puts the index in the greedy matrix
		-AOs the face 
		
	-greedy step
		-dance move
		-selects worker from group (idk how many, we can change later I guess?)
		-determines orientation of face
		-walks along on one axis until stops, checks if it can expand the other way. if not, take that greedyindex and slap it in the quadindex.
		-that should be it!!!
*/

void main () {
	int WorkGroupDataLength = ChunkDimensions.ChunkSize / ChunkDimensions.WorkGroupSide;

	int Gx = int(gl_GlobalInvocationID.x);
	int Gy = int(gl_GlobalInvocationID.y);
	int Gz = int(gl_GlobalInvocationID.z);

	for (int x = Gx * WorkGroupDataLength + 1; x < (Gx + 1) * WorkGroupDataLength + 1; x++) {
		for (int y = Gy * WorkGroupDataLength + 1; y < (Gy + 1) * WorkGroupDataLength + 1; y++) {
			for (int z = Gz * WorkGroupDataLength + 1; z < (Gz + 1) * WorkGroupDataLength + 1; z++) {
				if (ChunkData.data[x][y][z] == 0) {
					continue;
				}
			
			
				//up face
				if ((ChunkData.data[x][y+1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y+1][z]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.UpQuadIndex];
					
					if (q.greedy) {
						GreedyData(q, 0, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AOUpFace(IndexTicket, vec3(x, y, z), false);
					}
				} 
				
				
				//north face
				if ((ChunkData.data[x][y][z-1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z-1]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.NorthQuadIndex];
					
					if (q.greedy) {
						GreedyData(q, 1, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);					
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AONorthFace(IndexTicket, vec3(x, y, z), false);
					}
				}
				
				//do east face
				if ((ChunkData.data[x-1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x-1][y][z]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.EastQuadIndex];
					
					if (q.greedy) {
						GreedyData(q, 2, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);					
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AOEastFace(IndexTicket, vec3(x, y, z), false);
					}
				}	
				
				//do south face
				if ((ChunkData.data[x][y][z+1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z+1]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.SouthQuadIndex];
					
					if (q.greedy) {
						GreedyData(q, 3, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);					
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AOSouthFace(IndexTicket, vec3(x, y, z), false);
					}
				}
				
				//do west face
				if ((ChunkData.data[x+1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x+1][y][z]].transparent != 0)) {				
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.WestQuadIndex];
					if (q.greedy) {
						GreedyData(q, 4, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AOWestFace(IndexTicket, vec3(x, y, z), false);
					}
				}
				
				//down face
				if ((ChunkData.data[x][y - 1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y-1][z]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.DownQuadIndex];
					if (q.greedy) {
						GreedyData(q, 5, ChunkData.data[x][y][z], vec3(x, y, z));
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);					
						ApplyVertices(AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						vec4 n = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						vec4[3] norm;
						norm[0] = n;
						norm[1] = n;
						norm[2] = n;
						ApplyNormals(norm, IndexTicket);
						ApplyColor(q.color, IndexTicket);
						ApplyIndices(IndexTicket);
						ApplyUV(q.custom0.x, IndexTicket);
						CollisionMeshAssign(AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)), IndexTicket);
						AODownFace(IndexTicket, vec3(x,y,z), false);
					}
				}
			}
		}
	}
}