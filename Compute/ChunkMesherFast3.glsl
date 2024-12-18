#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;
const int MAX_BUFFER = 402653184;
const bool GREEDY = false;

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

layout(set = 0, binding = 5, std430) buffer greedystorage{
	Quad2 data[CHUNK_SIZE * 6][CHUNK_SIZE][CHUNK_SIZE];
} GreedyStorage; 


vec4[3] AddPosition(vec4[3] vert, vec3 pos) {
	vert[0] = vert[0] + pos.xyzx;
	vert[1] = vert[1] + pos.yzxy;
	vert[2] = vert[2] + pos.zxyz;
	return vert;
}

const float AOVAL = 0.1;
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

//todos:
/*
	get greediness working
	
	address greedy buffer properly
	
	LENGTH * 6 individual frames to be greedy about
	first LENGTH * 6 * 4 (3,072?) workgroups allowed, is greedyIndex
	
	
	greedyIndex % 4 is the starting quadrant on a greedy frame (y and z value)
	greedyIndex / 4 is the starting greedy frame (x value)
	
	greedyIndex / (LENGTH * 4) = side facing, need a const array of directions.
	get quad? check if zero? if zero set a bool or something
	for i:
		for j:
			if not visited and not 0 and == current.blocktype:
				stretch current quad
			else 
				if not same type of block
					check if expand y
				else
			
	
*/

//rename to grownorth
void growX(inout Quad2 q, int greedyIndex) {
	if (greedyIndex / (CHUNK_SIZE * 4) == 0) {
		//q.vertices[0] = q.vertices[0] + vec4(0,0,1,0); //pushes the 'southeast' vertex (quad relative)
		//q.vertices[1] = q.vertices[1] + vec4(0,1,0,0); //pushes the 'southwest' vertex (quad relative)
		q.vertices[2] = q.vertices[2] + vec4(1,0,0,1); //northwest and northeast
	}
}

//grow east/west?
void growY(inout Quad2 q, int greedyIndex) {
	if (greedyIndex / (CHUNK_SIZE * 4) == 0) {
		q.vertices[1] = q.vertices[1] + vec4(1,0,0,1); //pushes the 'southwest' vertex (quad relative) and pushes the 'northwest' vertex (quad relative)
	}
}


void GreedyStep(int GreedyIndex) {

	
	/*
	greedyIndex % 4 is the starting quadrant on a greedy frame (y and z value)
	greedyIndex / 4 is the starting greedy frame (x value)
	greedyIndex / (LENGTH * 4) = side facing, need a const array of directions.
	*/
	
	
	ivec3 greedyStart = ivec3(GreedyIndex/4, ivec2(GreedyIndex % 2 * (CHUNK_SIZE/ 4), (GreedyIndex % 4) / 2 * (CHUNK_SIZE/ 4)));
	
	//now, we loop over the thing,
	Quad2 currentQuad = GreedyStorage.data[greedyStart.x][greedyStart.y][greedyStart.z];
	bool[CHUNK_SIZE / 4][CHUNK_SIZE/ 4] visited;
	bool[CHUNK_SIZE / 4] QuadLength;
	for (int i = 0; i < CHUNK_SIZE / 4; i++) {
		for (int j = 0; j < CHUNK_SIZE / 4; j++) {
			ivec3 pos = greedyStart + ivec3(0, i, j);
			
			/*
			int IndexTicket = atomicAdd(QuadCount.count, 1);
			currentQuad = GreedyStorage.data[pos.x][pos.y][pos.z];
			growX(currentQuad, GreedyIndex);
			Quads.data[IndexTicket] = currentQuad;
			
			
			if(!visited[i][j]) {
				if(currentQuad.normal.w == 0) {
					if(GreedyStorage.data[pos.x][pos.y][pos.z].normal.w != 0) {
						currentQuad = GreedyStorage.data[pos.x][pos.y][pos.z];
					} else {
						continue;
					}
				}
				
				//check if current guy is same as next guy
				if (!(j == (CHUNK_SIZE / 4)) && currentQuad.normal.w == GreedyStorage.data[pos.x][pos.y][pos.z + 1].normal.w) {
					growX(currentQuad, GreedyIndex);
					visited[i][j + 1] = true;
				} else {
					//check if sideways expand?
					if(!(i == (CHUNK_SIZE / 4)) && currentQuad.normal.w == GreedyStorage.data[pos.x][pos.y + 1][pos.z].normal.w) {
						growY(currentQuad, GreedyIndex);
					} else {
						int IndexTicket = atomicAdd(QuadCount.count, 1);
						Quads.data[IndexTicket] = currentQuad;
					}
				}
			} else {
				int IndexTicket = atomicAdd(QuadCount.count, 1);
				Quads.data[IndexTicket] = currentQuad;
			}
			*/
		}
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
				
				//up face
				if ((ChunkData.data[x][y+1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y+1][z]].transparent != 0)) {
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.UpQuadIndex];
					if(q.greedy != 0 && GREEDY) {
						GreedyStorage.data[x + 0][y][z].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						GreedyStorage.data[x + 0][y][z].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						GreedyStorage.data[x + 0][y][z].custom0 = q.custom0;
						GreedyStorage.data[x + 0][y][z].color = q.color;
						AOUpFace(GreedyStorage.data[x + 0][y][z], vec3(x, y, z));
					} else { 
						int IndexTicket = atomicAdd(QuadCount.count, 1);
						Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
						Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
						Quads.data[IndexTicket].custom0 = q.custom0;
						Quads.data[IndexTicket].color = q.color;
						AOUpFace(Quads.data[IndexTicket], vec3(x, y, z));
					}
				} 
				
				//do west face?
				if ((ChunkData.data[x+1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x+1][y][z]].transparent != 0)) {					
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
				if ((ChunkData.data[x-1][y][z] == 0 || VoxelData.FaceData[ChunkData.data[x-1][y][z]].transparent != 0)) {
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
				if ((ChunkData.data[x][y][z+1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z+1]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.SouthQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AOSouthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}

				
				if ((ChunkData.data[x][y][z-1] == 0 || VoxelData.FaceData[ChunkData.data[x][y][z-1]].transparent != 0)) {
					int IndexTicket = atomicAdd(QuadCount.count, 1);
					Face f = VoxelData.FaceData[ChunkData.data[x][y][z]];
					QuadIn q = VoxelData.QuadInput[f.NorthQuadIndex];
					Quads.data[IndexTicket].vertices = AddPosition( q.vertices, vec3(x - ChunkDimensions.ChunkSize/2 - 1, y - ChunkDimensions.ChunkSize/2 - 1, z - ChunkDimensions.ChunkSize/2 - 1));
					Quads.data[IndexTicket].normal = vec4(q.normX, q.normY, q.normZ, ChunkData.data[x][y][z]);
					Quads.data[IndexTicket].custom0 = q.custom0;
					Quads.data[IndexTicket].color = q.color;
					AONorthFace(Quads.data[IndexTicket], vec3(x, y, z));
				}
				
				//down face
				if ((ChunkData.data[x][y - 1][z] == 0 || VoxelData.FaceData[ChunkData.data[x][y-1][z]].transparent != 0)) {
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
	
	int greedyIndex = Gx + Gy + Gz;
	if (GREEDY && greedyIndex / 3072 < 1) {
		GreedyStep(greedyIndex);
	} else {
		return;
	}
}