#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER_LENGTH = 786432;

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
	int greedy;
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

layout(set = 0, binding = 0, std430) buffer vertexbuffer{
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
	int WorkgroupCounter;
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
	/*
	UV.append(Vector3(1,1,0));
	UV.append(Vector3(0,1,0));
	UV.append(Vector3(0,0,0));
	UV.append(Vector3(1,0,0));




	VertexBuffer.UV[IndexTicket * 8 + 0] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 1] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 2] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 3] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 4] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 5] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 6] = UVIndex;
	VertexBuffer.UV[IndexTicket * 8 + 7] = UVIndex;
	*/
		
	//offset by 1 pixel down and to the right
	vec2 start = vec2( mod(int(UVIndex), 32), int(UVIndex) / 32) * 1.0/32.0;// + vec2(1.0/2112.0, 1.0/2112.0);
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

void main () {
	int WorkGroupDataLength = ChunkDimensions.ChunkSize / ChunkDimensions.WorkGroupSide;

	int Gx = int(gl_GlobalInvocationID.x);
	int Gy = int(gl_GlobalInvocationID.y);
	int Gz = int(gl_GlobalInvocationID.z);
	
	for (int x = Gx * WorkGroupDataLength + 1; x < (Gx + 1) * WorkGroupDataLength + 1; x++) {
		for (int y = Gy * WorkGroupDataLength + 1; y < (Gy + 1) * WorkGroupDataLength + 1; y++) {
			for (int z = Gz * WorkGroupDataLength + 1; z < (Gz + 1) * WorkGroupDataLength + 1; z++) {
				if(ChunkData.data[x][y][z] == 0) {
					continue;
				}
				
				//up face
				if ((ChunkData.data[x][y+1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y+1][z]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.UpQuadIndex];
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
					//AOUpFace(Quads.data[IndexTicket], vec3(x, y, z));
					
				} 
				
				//do west face?
				if ((ChunkData.data[x+1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x+1][y][z]].transparent != 0)) {				
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.WestQuadIndex];
					
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
					//AOWestFace(Quads.data[IndexTicket], vec3(x, y, z));
					
				}
				
				//do east face?
				if ((ChunkData.data[x-1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x-1][y][z]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.EastQuadIndex];
					
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
					
					//AOEastFace(Quads.data[IndexTicket], vec3(x, y, z));
				}	
				
				//do south face
				if ((ChunkData.data[x][y][z+1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z+1]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.SouthQuadIndex];
					
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
					
					//AOSouthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}

				
				if ((ChunkData.data[x][y][z-1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z-1]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.NorthQuadIndex];
					
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

					//AONorthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}
				
				//down face
				if ((ChunkData.data[x][y - 1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y-1][z]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.DownQuadIndex];
					
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
					
					//AODownFace(Quads.data[IndexTicket], vec3(x,y,z));
				}
			}
		}
	}
}