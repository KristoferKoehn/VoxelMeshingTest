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
	float vertices[MAX_BUFFER_LENGTH];
	float normals[MAX_BUFFER_LENGTH];
	float UV[MAX_BUFFER_LENGTH];
	float colors[MAX_BUFFER_LENGTH];
	float custom0[MAX_BUFFER_LENGTH];
	float CollisionVertices[MAX_BUFFER_LENGTH];
	float PaddingBuffer[MAX_BUFFER_LENGTH];
	int indices[MAX_BUFFER_LENGTH];
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


void ApplyIndices(int IndexTicket) {
	VertexBuffer.indices[IndexTicket * 6 + 0] = IndexTicket * 4 + 0;
	VertexBuffer.indices[IndexTicket * 6 + 1] = IndexTicket * 4 + 1;
	VertexBuffer.indices[IndexTicket * 6 + 2] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 3] = IndexTicket * 4 + 0;
	VertexBuffer.indices[IndexTicket * 6 + 4] = IndexTicket * 4 + 2;
	VertexBuffer.indices[IndexTicket * 6 + 5] = IndexTicket * 4 + 3;
}


void CollisionMeshAssign(int IndexTicket, vec4[3] vertices) {
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 0] = vertices[0].x;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 1] = vertices[0].y;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 2] = vertices[0].z;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 3] = vertices[0].w;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 4] = vertices[1].x;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 5] = vertices[1].y;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 6] = vertices[1].z;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 7] = vertices[1].w;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 8] = vertices[2].x;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 9] = vertices[2].y;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 10] = vertices[2].z;
	VertexBuffer.CollisionVertices[IndexTicket * 12 + 11] = vertices[2].w;
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
					ApplyIndices(IndexTicket);
					//ApplyVertices(Quads.colors, q.color, IndexTicket);
					//Quads.custom0[IndexTicket * 4 + 0] = q.custom0.x;
					//Quads.custom0[IndexTicket * 4 + 1] = q.custom0.y;
					//Quads.custom0[IndexTicket * 4 + 2] = q.custom0.z;
					//Quads.custom0[IndexTicket * 4 + 3] = q.custom0.w;
					
					//CollisionMeshAssign(Quads.CollisionVertices, IndexTicket, AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1)));
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
					ApplyIndices(IndexTicket);
					
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
					ApplyIndices(IndexTicket);
					
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
					ApplyIndices(IndexTicket);
					
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
					ApplyIndices(IndexTicket);

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
					ApplyIndices(IndexTicket);
					
					//AODownFace(Quads.data[IndexTicket], vec3(x,y,z));
				}
			}
		}
	}
}