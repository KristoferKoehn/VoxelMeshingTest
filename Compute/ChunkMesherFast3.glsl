#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER = 402653184;

//64 bytes
struct Quad {
	vec4[3] vertices;
	float UVIndex;
	float Normx;
	float Normy;
	float Normz;
};


struct QuadIn {
	//48bytes
	vec4[3] vertices;
	//48bytes
	vec4[3] color;
	//16bytes
	vec4 custom0;
		//12bytes
	float normX;
	float normY;
	float normZ;
	//8 bytes
	int greedy;
	int next;
	int[31] padding;
};

//128 bytes wahoo
struct Quad2 {
	//48 bytes
	vec4[3] vertices;
	//48 bytes
	vec4[3] color;
	//16 bytes
	vec4 custom0;
	//16 bytes
	vec4 Normal;
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

layout(set = 0, binding = 0, std430) buffer quads{
	Quad data[1048576 / 64];
} Quads;

layout(set = 0, binding = 1, std430) buffer quadcount{
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

//split these into different buffers^

const vec4[3] TopQuadVertices = vec4[3](vec4(-0.5f,  0.5f, -0.5f,  0.5f), vec4( 0.5f, -0.5f,  0.5f,  0.5f), vec4( 0.5f, -0.5f,  0.5f,  0.5f));
const vec3 TopNormal = vec3(0, 1, 0);

const vec4[3] NorthQuadVertices = vec4[3](vec4(-0.5f, -0.5f, -0.5f,  0.5f), vec4(-0.5f, -0.5f,  0.5f,  0.5f), vec4(-0.5f, -0.5f,  0.5f, -0.5f));
const vec3 NorthNormal = vec3(0, 0, -1);

const vec4[3] EastQuadVertices = vec4[3](vec4(-0.5f, -0.5f,  0.5f, -0.5f), vec4(-0.5f, -0.5f, -0.5f,  0.5f), vec4(-0.5f, -0.5f,  0.5f,  0.5f));
const vec3 EastNormal = vec3(-1, 0, 0);

const vec4[3] SouthQuadVertices = vec4[3](vec4( 0.5f, -0.5f,  0.5f, -0.5f), vec4(-0.5f,  0.5f, -0.5f,  0.5f), vec4( 0.5f,  0.5f,  0.5f,  0.5f));
const vec3 SouthNormal = vec3(0, 0, 1);

const vec4[3] WestQuadVertices = vec4[3](vec4( 0.5f, -0.5f, -0.5f,  0.5f), vec4(-0.5f,  0.5f,  0.5f,  0.5f), vec4( 0.5f,  0.5f,  0.5f, -0.5f));
const vec3 WestNormal = vec3(1, 0, 0);

const vec4[3] BottomQuadVertices = vec4[3](vec4( 0.5f, -0.5f, -0.5f, -0.5f), vec4(-0.5f, -0.5f, -0.5f, -0.5f), vec4( 0.5f,  0.5f, -0.5f,  0.5f));
const vec3 BottomNormal = vec3(0, -1, 0);

int MaxQuads = 402653184 / 64;

vec4[3] AddPosition(vec4[3] vert, vec3 pos) {
	vert[0] = vert[0] + pos.xyzx;
	vert[1] = vert[1] + pos.yzxy;
	vert[2] = vert[2] + pos.zxyz;
	return vert;
}

void main () {
	int WorkGroupDataLength = ChunkDimensions.ChunkSize / ChunkDimensions.WorkGroupSide;

	int Gx = int(gl_GlobalInvocationID.x);
	int Gy = int(gl_GlobalInvocationID.y);
	int Gz = int(gl_GlobalInvocationID.z);

	//Find the offset index IN *QUAD* ARRAY
	//based on the Gx Gy Gz

	for (int x = Gx * WorkGroupDataLength + 1; x < (Gx + 1) * WorkGroupDataLength + 1; x++) {
		for (int y = Gy * WorkGroupDataLength + 1; y < (Gy + 1) * WorkGroupDataLength + 1; y++) {
			for (int z = Gz * WorkGroupDataLength + 1; z < (Gz + 1) * WorkGroupDataLength + 1; z++) {
				if(ChunkData.data[x][y][z] == 0) {
					continue;
				}
				
				//do west face?
				if (ChunkData.data[x+1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x+1][y][z]].transparent != 0) {					
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Quads.data[IndexTicket].vertices = AddPosition(WestQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = WestNormal.x;
					Quads.data[IndexTicket].Normy = WestNormal.y;
					Quads.data[IndexTicket].Normz = WestNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				}
				
				//do east face?
				if (ChunkData.data[x-1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x-1][y][z]].transparent != 0) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);

					Quads.data[IndexTicket].vertices = AddPosition(EastQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = EastNormal.x;
					Quads.data[IndexTicket].Normy = EastNormal.y;
					Quads.data[IndexTicket].Normz = EastNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				}	
				
				//do south face
				if (ChunkData.data[x][y][z+1] == 0) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					Quads.data[IndexTicket].vertices = AddPosition(VoxelData.QuadInput[f.SouthQuadIndex].vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = SouthNormal.x;
					Quads.data[IndexTicket].Normy = SouthNormal.y;
					Quads.data[IndexTicket].Normz = SouthNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				}

				
				if (ChunkData.data[x][y][z-1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z-1]].transparent != 0) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Quads.data[IndexTicket].vertices = AddPosition( NorthQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = NorthNormal.x;
					Quads.data[IndexTicket].Normy = NorthNormal.y;
					Quads.data[IndexTicket].Normz = NorthNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				}
			
				
				if (ChunkData.data[x][y+1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y+1][z]].transparent != 0) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Quads.data[IndexTicket].vertices = AddPosition( TopQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = TopNormal.x;
					Quads.data[IndexTicket].Normy = TopNormal.y;
					Quads.data[IndexTicket].Normz = TopNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				} 
				
				if (ChunkData.data[x][y - 1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y-1][z]].transparent != 0) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Quads.data[IndexTicket].vertices = AddPosition( BottomQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].Normx = BottomNormal.x;
					Quads.data[IndexTicket].Normy = BottomNormal.y;
					Quads.data[IndexTicket].Normz = BottomNormal.z;
					Quads.data[IndexTicket].UVIndex = ChunkData.data[x][y][z];
				}
			}
		}
	}
	QuadCount.WorkgroupCounter++;
}