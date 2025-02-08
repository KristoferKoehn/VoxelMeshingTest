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

int GetBlockType(int IndexTicket) {
	return int(GreedyBuffer.data1[IndexTicket]);
}

void NorthStretch(int GreedyIndex, vec3 forward) {
	int rIndex = GreedyIndex * 12;
	
	/*
	GreedyBuffer.vertices[rIndex + 0 * 3 + 0] = GreedyBuffer.vertices[rIndex + 0 * 3 + 0] + 1;
	GreedyBuffer.vertices[rIndex + 0 * 3 + 1] = GreedyBuffer.vertices[rIndex + 0 * 3 + 1] + 1;
	GreedyBuffer.vertices[rIndex + 0 * 3 + 2] = GreedyBuffer.vertices[rIndex + 0 * 3 + 2] + 1;
	
	
	GreedyBuffer.vertices[rIndex + 1 * 3 + 0] = GreedyBuffer.vertices[rIndex + 1 * 3 + 0] + 1;
	GreedyBuffer.vertices[rIndex + 1 * 3 + 1] = GreedyBuffer.vertices[rIndex + 1 * 3 + 1] + 1;
	GreedyBuffer.vertices[rIndex + 1 * 3 + 2] = GreedyBuffer.vertices[rIndex + 1 * 3 + 2] + 1;
	*/
	
	GreedyBuffer.vertices[rIndex + 2 * 3 + 0] = GreedyBuffer.vertices[rIndex + 2 * 3 + 0] + forward.x;
	GreedyBuffer.vertices[rIndex + 2 * 3 + 1] = GreedyBuffer.vertices[rIndex + 2 * 3 + 1] + forward.y;
	GreedyBuffer.vertices[rIndex + 2 * 3 + 2] = GreedyBuffer.vertices[rIndex + 2 * 3 + 2] + forward.z;
	
	GreedyBuffer.vertices[rIndex + 3 * 3 + 0] = GreedyBuffer.vertices[rIndex + 3 * 3 + 0] + forward.x;
	GreedyBuffer.vertices[rIndex + 3 * 3 + 1] = GreedyBuffer.vertices[rIndex + 3 * 3 + 1] + forward.y;
	GreedyBuffer.vertices[rIndex + 3 * 3 + 2] = GreedyBuffer.vertices[rIndex + 3 * 3 + 2] + forward.z;
	
}

void WestStretch(int GreedyIndex, vec3 sideways) {
	int rIndex = GreedyIndex * 12;
	
	/*
	GreedyBuffer.vertices[rIndex + 0 * 3 + 0] = GreedyBuffer.vertices[rIndex + 0 * 3 + 0] + 1;
	GreedyBuffer.vertices[rIndex + 0 * 3 + 1] = GreedyBuffer.vertices[rIndex + 0 * 3 + 1] + 1;
	GreedyBuffer.vertices[rIndex + 0 * 3 + 2] = GreedyBuffer.vertices[rIndex + 0 * 3 + 2] + 1;
	*/
	
	GreedyBuffer.vertices[rIndex + 1 * 3 + 0] = GreedyBuffer.vertices[rIndex + 1 * 3 + 0] + sideways.x;
	GreedyBuffer.vertices[rIndex + 1 * 3 + 1] = GreedyBuffer.vertices[rIndex + 1 * 3 + 1] + sideways.y;
	GreedyBuffer.vertices[rIndex + 1 * 3 + 2] = GreedyBuffer.vertices[rIndex + 1 * 3 + 2] + sideways.z;
	
	GreedyBuffer.vertices[rIndex + 2 * 3 + 0] = GreedyBuffer.vertices[rIndex + 2 * 3 + 0] + sideways.x;
	GreedyBuffer.vertices[rIndex + 2 * 3 + 1] = GreedyBuffer.vertices[rIndex + 2 * 3 + 1] + sideways.y;
	GreedyBuffer.vertices[rIndex + 2 * 3 + 2] = GreedyBuffer.vertices[rIndex + 2 * 3 + 2] + sideways.z;
	
	/*
	GreedyBuffer.vertices[rIndex + 3 * 3 + 0] = GreedyBuffer.vertices[rIndex + 3 * 3 + 0] + sideways.x;
	GreedyBuffer.vertices[rIndex + 3 * 3 + 1] = GreedyBuffer.vertices[rIndex + 3 * 3 + 1] + sideways.y;
	GreedyBuffer.vertices[rIndex + 3 * 3 + 2] = GreedyBuffer.vertices[rIndex + 3 * 3 + 2] + sideways.z;
	*/

}

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
}

/*
bool StretchNorth(ivec3 pos, ivec3 north_dist, ivec3 west_dist, int block) { //this needs north dir and west dir for this frame. maybe length is replaced by vec3 = North * length?
	vec3 n_pos = north_dist + normalize(north_dist);

	for (int i = pos.x; i < pos.x + width; i++) {
		
	}	
}
*/

void main () {
	int GreedyIndex = int(gl_GlobalInvocationID.x);
	
	int face = (GreedyIndex) / 64;

	//the idea is to scan along the long-axis, but rotate the direction of the scan to line up with the co-planes.
	//from 0-383, such that the position is viewed as 64-cubed local, scanning along whichever useful axis, but the
	//'real position' within the array is adjusted as such: pos.x = pos.x + face * 64 after the rotation.
	
	//this switch case will assign the 'forward' advance and the 'sideways' advance direction OR the rotation frame for the face.
	
	//consider a stack of 2d grids, where we walk from the top left to the bottom right, advancing down and to the right.
	//we advance down as much as we can, then to the right. Once we cannot advance, we transfer the greedy data to the 
	//vertex buffer, and continue onward.
	
	ivec3 StartPosition = ivec3(0);
	ivec3 ForwardDirection = ivec3(0);
	ivec3 SideDirection = ivec3(0);
	//up, north, east, south, west, down
	switch (face) {
		case 0:
			
			//up case
			StartPosition = ivec3(0, GreedyIndex, 0);
			ForwardDirection = ivec3(0, 0, 1);
			SideDirection = ivec3(1, 0, 0);	
			break;
		case 1:
		
			//north case
			StartPosition = ivec3((64 * 1), 0, GreedyIndex - (64 * 1));
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(1, 0, 0);
			break;
		case 2:
		
			//east case
			StartPosition = ivec3(GreedyIndex, 0, 63);
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(0, 0, -1);
			break;
		case 3:
		
			//south case
			StartPosition = ivec3((64 * 4 - 1), 0, GreedyIndex - (64 * 3));
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(-1, 0, 0);
			break;
		case 4:
			
			//west case
			StartPosition = ivec3(GreedyIndex, 0, 0);
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(0, 0, 1);
			break;
		case 5:
			
			//down case
			StartPosition = ivec3((64 * 6 - 1), GreedyIndex - (64 * 5), 0);
			ForwardDirection = ivec3(0, 0, 1);
			SideDirection = ivec3(-1, 0, 0);	
			break;
		default:
			return;
	}

	ivec3 CurrentSidewaysPosition = StartPosition;
	bool visited[CHUNK_SIZE][CHUNK_SIZE];
	int StretchyFace = -1;
	for (int i = 0; i < CHUNK_SIZE; i++) {
		ivec3 CurrentPos = CurrentSidewaysPosition;
		for (int j = 0; j < CHUNK_SIZE; j++) {
			/*
			i,j is starting position
			int length = 1
			int width = 1
			
			functions that loop over a side to check if expandable
			CheckExpandNorth
			
			while(expandedNorth || expandedWest) {
				if (GetBlockType(GreedyBuffer.data[CurrentPos.x][CurrentPos.y][CurrentPos.z]) == 0) {
					CurrentPos = CurrentPos + ForwardDirection;
					visited[i][j] = true;
					continue;
				}
			}
			
			
			
			*/
			if (GetBlockType(GreedyBuffer.data[CurrentPos.x][CurrentPos.y][CurrentPos.z]) == 0) {
				CurrentPos = CurrentPos + ForwardDirection;
				visited[i][j] = true;
				continue;
			}
		
			if (StretchyFace == -1) {
				StretchyFace = GreedyBuffer.data[CurrentPos.x][CurrentPos.y][CurrentPos.z];
			}
			
			if(GetBlockType(StretchyFace) == GetBlockType(GreedyBuffer.data[CurrentPos.x + ForwardDirection.x][CurrentPos.y + ForwardDirection.y][CurrentPos.z + ForwardDirection.z])) {
				NorthStretch(StretchyFace, ForwardDirection);
				visited[i][j] = true;
			} else { 
				GreedyTransfer(StretchyFace);
				StretchyFace = -1;
			}

			
			CurrentPos = CurrentPos + ForwardDirection;
		}
		
		if (StretchyFace != -1) {
			GreedyTransfer(StretchyFace);
			StretchyFace = -1;
		}
		CurrentSidewaysPosition = CurrentSidewaysPosition + SideDirection;
	}

}

