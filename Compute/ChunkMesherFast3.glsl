#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER = 402653184;

//256 bytes
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
	//124 bytes padding
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
	vec4 normal;
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
	Quad2 data[1048576 / 64];
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

layout(set = 0, binding = 5, std430) buffer faceculldirection{
	bool Up;
	bool North;
	bool East;
	bool South;
	bool West;
	bool Down;
} FaceCullDirection;

int MaxQuads = 402653184 / 64;

vec4[3] AddPosition(vec4[3] vert, vec3 pos) {
	vert[0] = vert[0] + pos.xyzx;
	vert[1] = vert[1] + pos.yzxy;
	vert[2] = vert[2] + pos.zxyz;
	return vert;
}

//todos:
/*
	Investigate player position dot product overhead.
	
	how often would we update? how can we mitigate the need to update?
	make the angle wide and only update if the player moves more than 4 blocks?
	

*/

const float AOVAL = 0.7;
void NorthEastVertexAO(inout Quad2 q) { 
	q.color[2].gba = q.color[2].gba * AOVAL; //north east
}

void NorthWestVertexAO(inout Quad2 q) { 
	q.color[1].ba = q.color[1].ba * AOVAL; //north west
	q.color[2].r = q.color[2].r * AOVAL; 
}

void SouthWestVertexAO(inout Quad2 q) { 
	q.color[0].a = q.color[0].a * AOVAL; // this is the 'south west' corner
	q.color[1].rg = q.color[1].rg * AOVAL; 
}

void SouthEastVertexAO(inout Quad2 q) { 
	q.color[0].rgb = q.color[0].rgb *AOVAL; // this is the 'south east' corner
}

void SouthVerticesAO(inout Quad2 q) {
	SouthWestVertexAO(q);
	SouthEastVertexAO(q);
}

void NorthVerticesAO(inout Quad2 q) {
	NorthEastVertexAO(q);
	NorthWestVertexAO(q);
}

void EastVerticesAO(inout Quad2 q) {
	NorthEastVertexAO(q);
	SouthEastVertexAO(q);
}

void WestVerticesAO(inout Quad2 q) {
	NorthWestVertexAO(q);
	SouthWestVertexAO(q);
}

vec4[3] flipvec4(vec4[3] quad) {
	vec4[3] flipped;
	flipped[0] = vec4(quad[0].w, quad[1].xyz);
	flipped[1] = vec4(quad[1].w, quad[2].xyz);
	flipped[2] = vec4(quad[2].w, quad[0].xyz);
	return flipped;
}

void FlipQuad(inout Quad2 q) {
	q.vertices = flipvec4(q.vertices);
	q.color = flipvec4(q.color);
	q.normal.w = q.normal.w + 24001.0;
}

void AOUpFace(inout Quad2 q, vec3 voxelPos) {

	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 1, 0);
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) +  int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
}

void AODownFace(inout Quad2 q, vec3 voxelPos) {

	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, -1, 0);
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z) + 1] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z) - 1] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) +  int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
}

void AONorthFace(inout Quad2 q, vec3 voxelPos) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 0, -1);
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)- 1][int(targpos.z) ] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
}

void AOSouthFace(inout Quad2 q, vec3 voxelPos) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(0, 0, 1);
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y)][int(targpos.z)] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)][int(targpos.z)] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y)- 1][int(targpos.z) ] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x) + 1][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x) - 1][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
}

void AOWestFace(inout Quad2 q, vec3 voxelPos) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(1, 0, 0);
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) + 1] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)- 1][int(targpos.z)  - 1] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) - 1] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z) + 1] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
}

void AOEastFace(inout Quad2 q, vec3 voxelPos) {
	bool Northeast = false;
	bool Northwest = false;
	bool Southeast = false;
	bool Southwest = false;
	
	vec3 targpos = voxelPos + vec3(-1, 0, 0);
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) - 1] != 0) {
		WestVerticesAO(q);
		Northwest = true;
		Southwest = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z)] != 0) {
		NorthVerticesAO(q);
		Northwest = true;
		Northeast = true;
	}
	
	if (ChunkData.data[int(targpos.x)][int(targpos.y)][int(targpos.z) + 1] != 0) {
		EastVerticesAO(q);
		Northeast = true;
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z)] != 0) {
		SouthVerticesAO(q);
		Southeast = true;
		Southwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) - 1] != 0) {
		NorthWestVertexAO(q);
		Northwest = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y)- 1][int(targpos.z) + 1] != 0) {
		SouthEastVertexAO(q);
		Southeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) + 1][int(targpos.z) + 1] != 0) {
		NorthEastVertexAO(q);
		Northeast = true;
	}
	if (ChunkData.data[int(targpos.x)][int(targpos.y) - 1][int(targpos.z) - 1] != 0) {
		SouthWestVertexAO(q);
		Southwest = true;
	}
	if ( int(Southwest) + int(Northeast) < int(Southeast) + int(Northwest)) {
		FlipQuad(q);
	}
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
				if ((ChunkData.data[x+1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x+1][y][z]].transparent != 0) && FaceCullDirection.West) {					
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.WestQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AOWestFace(Quads.data[IndexTicket], vec3(x, y, z));
				}
				
				//do east face?
				if ((ChunkData.data[x-1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x-1][y][z]].transparent != 0) && FaceCullDirection.East) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.EastQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition(q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AOEastFace(Quads.data[IndexTicket], vec3(x, y, z));
				}	
				
				//do south face
				if ((ChunkData.data[x][y][z+1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z+1]].transparent != 0) && FaceCullDirection.South) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.SouthQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AOSouthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}

				
				if ((ChunkData.data[x][y][z-1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z-1]].transparent != 0) && FaceCullDirection.North) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.NorthQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AONorthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}
			
				//up face
				if ((ChunkData.data[x][y+1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y+1][z]].transparent != 0) && FaceCullDirection.Up) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.UpQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AOUpFace(Quads.data[IndexTicket], vec3(x, y, z));
				} 
				
				//down face
				if ((ChunkData.data[x][y - 1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y-1][z]].transparent != 0) && FaceCullDirection.Down) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.DownQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AODownFace(Quads.data[IndexTicket], vec3(x,y,z));
				}
			}
		}
	}
	QuadCount.WorkgroupCounter++;
}