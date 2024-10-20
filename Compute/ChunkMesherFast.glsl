#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 128;
const int MAX_BUFFER = 402653184;

//should be vec4[3] for vertices, three floats for normal (x,y,z), then 3 vec2s for UVs.
//that comes out to 84 bytes instead of the current 144

//^^ that stuff is old, it's now 64 bytes :sunglasses:
// 
struct Quad {
	vec4[3] vertices;
	float UVIndex;
	float Normx;
	float Normy;
	float Normz;
	
	/*
	custom time!!
	float metallicity
	emissiveness
	
	*/
};

layout(set = 0, binding = 0, std430) buffer quads{
	Quad data[402653184 / 64];
} Quads;

layout(set = 0, binding = 1, std430) buffer quadcount{
	int count[2024];
} QuadCount;

layout(set = 0, binding = 2, std430) buffer chunkdata{
	int data[CHUNK_SIZE + 2][CHUNK_SIZE + 2][CHUNK_SIZE + 2];
} ChunkData;

layout(set = 0, binding = 3, std430) buffer chunkdimensions{
	int ChunkSize;
	int WorkGroupSide;
} ChunkDimensions;

layout(set = 0, binding = 4, std430) buffer playerposition{
	float x;
	float y;
	float z;
	float padding;
} PlayerPosition;


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

int OffsetQuadDataIndex(int x, int y, int z) {
	int Scale = x + y * ChunkDimensions.WorkGroupSide + z * ChunkDimensions.WorkGroupSide * ChunkDimensions.WorkGroupSide;
	return MaxQuads / (ChunkDimensions.WorkGroupSide * ChunkDimensions.WorkGroupSide * ChunkDimensions.WorkGroupSide) * Scale;
}

int CountOffset(int x, int y, int z) {
	int Scale = x + y * ChunkDimensions.WorkGroupSide + z * ChunkDimensions.WorkGroupSide * ChunkDimensions.WorkGroupSide;
	return Scale;
}

int GetPackedData(int z, int packed) {
	return int((packed >> (z % 4) * 8) & 0xFF);
}

int GetBlockID(int x, int y, int z, int[CHUNK_SIZE + 2][CHUNK_SIZE + 2][CHUNK_SIZE+2] data) {
	int num = data[x][y][z/4];
	
	return GetPackedData(z, num);
}


void main () {

	int pX = int(floor((PlayerPosition.x + 128)/ 128)) - 1;
	int pZ = int(floor((PlayerPosition.z + 128)/ 128)) - 1;

	int WorkGroupDataLength = ChunkDimensions.ChunkSize / ChunkDimensions.WorkGroupSide;

	int Gx = int(gl_GlobalInvocationID.x);
	int Gy = int(gl_GlobalInvocationID.y);
	int Gz = int(gl_GlobalInvocationID.z);

	//Find the offset index IN *QUAD* ARRAY
	//based on the Gx Gy Gz

	int CountIndex = CountOffset(Gx, Gy, Gz);  
	int WorkGroupOffset = OffsetQuadDataIndex(Gx, Gy, Gz);
	QuadCount.count[CountIndex] = 0;

	for (int x = Gx * WorkGroupDataLength + 1; x < (Gx + 1) * WorkGroupDataLength + 1; x++) {
		for (int y = Gy * WorkGroupDataLength + 1; y < (Gy + 1) * WorkGroupDataLength + 1; y++) {
			for (int z = Gz * WorkGroupDataLength + 1; z < (Gz + 1) * WorkGroupDataLength + 1; z++) {
				if(ChunkData.data[x][y][z] == 0) {
					continue;
				}
				
				if(pX >= 0) {
					//do west face?
					if (ChunkData.data[x+1][y][z] == 0) {
						Quad f;
						f.vertices = AddPosition(WestQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						//f.normals = WestNormal.xyzx;
						f.Normx = WestNormal.x;
						f.Normy = WestNormal.y;
						f.Normz = WestNormal.z;
						f.UVIndex = ChunkData.data[x][y][z];
						Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
						QuadCount.count[CountIndex]++;
					}
					
				}
				
				if (pX <= 0) {
					//do east face?
					if (ChunkData.data[x-1][y][z] == 0) {
						Quad f;
						f.vertices = AddPosition( EastQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						//f.normals = EastNormal.xyzx;
						f.Normx = EastNormal.x;
						f.Normy = EastNormal.y;
						f.Normz = EastNormal.z;
						f.UVIndex = ChunkData.data[x][y][z];
						Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
						QuadCount.count[CountIndex]++;
					}	
				}
				
				if (pZ >= 0) {
					//do south face
					if (ChunkData.data[x][y][z+1] == 0) {
						Quad f;
						f.vertices = AddPosition( SouthQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						//f.normals = SouthNormal.xyzx;
						f.Normx = SouthNormal.x;
						f.Normy = SouthNormal.y;
						f.Normz = SouthNormal.z;
						f.UVIndex = ChunkData.data[x][y][z];
						Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
						QuadCount.count[CountIndex]++;
					}
				
				}
				
				if (pZ <= 0) {
					if (ChunkData.data[x][y][z-1] == 0) {
						Quad f;
						f.vertices = AddPosition( NorthQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						//f.normals = NorthNormal.xyzx;
						f.Normx = NorthNormal.x;
						f.Normy = NorthNormal.y;
						f.Normz = NorthNormal.z;
						f.UVIndex = ChunkData.data[x][y][z];
						Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
						QuadCount.count[CountIndex]++;
					}
				}
				
				
				if (ChunkData.data[x][y+1][z] == 0) {
					Quad f;
					f.vertices = AddPosition( TopQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					//f.normals = TopNormal.xyzx;
					f.Normx = TopNormal.x;
					f.Normy = TopNormal.y;
					f.Normz = TopNormal.z;
					f.UVIndex = ChunkData.data[x][y][z];
					Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
					QuadCount.count[CountIndex]++;
				} 
				
				if (ChunkData.data[x][y - 1][z] == 0) {
					Quad f;
					f.vertices = AddPosition( BottomQuadVertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					//f.normals = BottomNormal.xyzx;
					f.Normx = BottomNormal.x;
					f.Normy = BottomNormal.y;
					f.Normz = BottomNormal.z;
					f.UVIndex = ChunkData.data[x][y][z];
					Quads.data[WorkGroupOffset + QuadCount.count[CountIndex]] = f;
					QuadCount.count[CountIndex]++;
				}
				
			}
		}
	}
}