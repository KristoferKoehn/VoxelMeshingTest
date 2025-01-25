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

void GreedyTransfer(int greedyTicket) {
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
	int GreedyIndex = int(gl_GlobalInvocationID.x);
	
	int face = GreedyIndex / 64;

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
			
			StartPosition = ivec3(0, GreedyIndex, 0);
			ForwardDirection = ivec3(0, 0, 1);
			SideDirection = ivec3(1, 0, 0);	
			
			break;
		case 1:
			StartPosition = ivec3((64 * 1), 0, GreedyIndex - (64 * 1));
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(1, 0, 0);
			break;
		case 2:
			StartPosition = ivec3(GreedyIndex, 0, 0);
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(0, 0, 1);
			break;
		case 3:
			StartPosition = ivec3((64 * 3), 0, GreedyIndex - (64 * 3));
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(1, 0, 0);
			break;
		case 4:
			StartPosition = ivec3(GreedyIndex, 0, 0);
			ForwardDirection = ivec3(0, 1, 0);
			SideDirection = ivec3(0, 0, 1);
			break;
		case 5:
			StartPosition = ivec3((64 * 5), GreedyIndex - (64 * 5), 0);
			ForwardDirection = ivec3(0, 0, 1);
			SideDirection = ivec3(1, 0, 0);	
			break;
	}
	
	
	if (ForwardDirection != vec3(0)) {
	
		ivec3 CurrentPos = StartPosition;
		bool visited[CHUNK_SIZE][CHUNK_SIZE];
		
		for (int i = 0; i < CHUNK_SIZE; i++) {
			for (int j = 0; j < CHUNK_SIZE; j++) {
			
			/*
			
			if not visited
			
				position
				length
				width
				bool expand = true
				
				while expand
				
					expand forward ? yes : no
					expand down ? yes : no
					
					if we expand, set visited to true at that spot
					
				whence done
					expand vertices
					greedy transfer
			
			*/
				/*
				if (!visited[i][j]) {
					vec3 position = StartPosition + ForwardDirection * i + SideDirection * j;
					int length = 1;
					int width = 1;
					bool expand = true;
					int CurrentFace = -1;
					
					if (CurrentFace == -1 && GreedyBuffer.data[position.x][position.y][position.z] != 0) {
						CurrentFace = GreedyBuffer.data1[GreedyBuffer.data[position.x][position.y][position.z]];
					}
					
					while (expand) {
					
						//check if forward is expandable
						vec3 probe = (position + length * ForwardDirection);
						if (GreedyBuffer.data[probe.x][probe.y][probe.z] )
						
						//check if side is expandable
						
					}

				} */
			
				if (GreedyBuffer.data[CurrentPos.x][CurrentPos.y][CurrentPos.z] != 0) {
					GreedyTransfer(GreedyBuffer.data[CurrentPos.x][CurrentPos.y][CurrentPos.z]);
					atomicAdd(QuadCount.padding, 1);
				}
				CurrentPos = CurrentPos + ForwardDirection;
			}
			CurrentPos = CurrentPos - ForwardDirection * 64;
			CurrentPos = CurrentPos + SideDirection;
		}
	}
}

