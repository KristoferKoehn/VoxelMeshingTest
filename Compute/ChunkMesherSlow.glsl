#[compute]
#version 450

layout(local_size_x = 6, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) buffer topvertices{
	vec4 data[];
} TopVertices;

layout(set = 0, binding = 1, std430) buffer topnormals{
	vec4 data[];
} TopNormals;

layout(set = 0, binding = 2, std430) buffer topuvs{
	vec4 data[];
} TopUVs;

layout(set = 0, binding = 3, std430) buffer topcount{
	uint data;
} TopCount;

layout(set = 0, binding = 5, std430) buffer northvertices{
	vec4 data[];
} NorthVertices;

layout(set = 0, binding = 6, std430) buffer northnormals{
	vec4 data[];
} NorthNormals;

layout(set = 0, binding = 7, std430) buffer northuvs{
	vec4 data[];
} NorthUVs;

layout(set = 0, binding = 8, std430) buffer northcount{
	uint data;
} NorthCount;

layout(set = 0, binding = 9, std430) buffer eastvertices{
	vec4 data[];
} EastVertices;

layout(set = 0, binding = 10, std430) buffer eastnormals{
	vec4 data[];
} EastNormals;

layout(set = 0, binding = 11, std430) buffer eastuvs{
	vec4 data[];
} EastUVs;

layout(set = 0, binding = 12, std430) buffer eastcount{
	uint data;
} EastCount;

layout(set = 0, binding = 13, std430) buffer southvertices{
	vec4 data[];
} SouthVertices;

layout(set = 0, binding = 14, std430) buffer southnormals{
	vec4 data[];
} SouthNormals;

layout(set = 0, binding = 15, std430) buffer southuvs{
	vec4 data[];
} SouthUVs;

layout(set = 0, binding = 16, std430) buffer southcount{
	uint data;
} SouthCount;

layout(set = 0, binding = 17, std430) buffer westvertices{
	vec4 data[];
} WestVertices;

layout(set = 0, binding = 18, std430) buffer westnormals{
	vec4 data[];
} WestNormals;

layout(set = 0, binding = 19, std430) buffer westuvs{
	vec4 data[];
} WestUVs;

layout(set = 0, binding = 20, std430) buffer westcount{
	uint data;
} WestCount;

layout(set = 0, binding = 21, std430) buffer bottomvertices{
	vec4 data[];
} BottomVertices;

layout(set = 0, binding = 22, std430) buffer bottomnormals{
	vec4 data[];
} BottomNormals;

layout(set = 0, binding = 23, std430) buffer bottomuvs{
	vec4 data[];
} BottomUVs;


layout(set = 0, binding = 24, std430) buffer bottomcount{
	uint data;
} BottomCount;

const int CHUNK_SIZE = 128;
const int OFFSET = CHUNK_SIZE/2;

layout(set = 0, binding = 4, std430) buffer chunkData{
	uint data[CHUNK_SIZE][CHUNK_SIZE][CHUNK_SIZE];
} ChunkData;

struct face {
	vec4[3] vertices;
	vec4[3] normals;
	vec4[3] UVs;
};


void TopFace() { 
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					if (y != CHUNK_SIZE - 1 && ChunkData.data[x][y + 1][z] != 0) {
						continue;
					}
					
					TopVertices.data[FaceSpacing]     = vec4(-0.5f + (x - OFFSET),  0.5f + (y - OFFSET), -0.5f + (z - OFFSET), 0.5f + (x - OFFSET));
					TopVertices.data[FaceSpacing + 1] = vec4( 0.5f + (y - OFFSET), -0.5f + (z - OFFSET),  0.5f + (x - OFFSET), 0.5f + (y - OFFSET));
					TopVertices.data[FaceSpacing + 2] = vec4( 0.5f + (z - OFFSET), -0.5f + (x - OFFSET),  0.5f + (y - OFFSET), 0.5f + (z - OFFSET));
					
					vec3 norms = vec3(0, 1, 0);
					TopNormals.data[FaceSpacing]     = norms.xyzx;
					TopNormals.data[FaceSpacing + 1] = norms.yzxy;
					TopNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					TopUVs.data[FaceSpacing]     = vec4(1,1,0,0);
					TopUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
					TopUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					
					FaceSpacing += 3;
				}
			}
		}
	}
	
	TopCount.data = FaceSpacing/3;
}

void NorthFace() {
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					if (z != 0 && ChunkData.data[x][y][z-1] != 0) {
						continue;
					}
					NorthVertices.data[FaceSpacing]     = vec4(-0.5f + (x - OFFSET), -0.5f + (y - OFFSET), -0.5f + (z - OFFSET),  0.5f + (x - OFFSET));
					NorthVertices.data[FaceSpacing + 1] = vec4(-0.5f + (y - OFFSET), -0.5f + (z - OFFSET),  0.5f + (x - OFFSET),  0.5f + (y - OFFSET));
					NorthVertices.data[FaceSpacing + 2] = vec4(-0.5f + (z - OFFSET), -0.5f + (x - OFFSET),  0.5f + (y - OFFSET), -0.5f + (z - OFFSET));
					
					vec3 norms = vec3(0, 0, -1);
					NorthNormals.data[FaceSpacing]     = norms.xyzx;
					NorthNormals.data[FaceSpacing + 1] = norms.yzxy;
					NorthNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					NorthUVs.data[FaceSpacing]     = vec4(1,1,0,0);
					NorthUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
					NorthUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					
					FaceSpacing += 3;
				}
			}
		}
	}
	
	NorthCount.data = FaceSpacing/3;
}

void EastFace() {
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					if (x != 0 && ChunkData.data[x - 1][y][z] != 0) {
						continue;
					}
					EastVertices.data[FaceSpacing]     = vec4(-0.5f + (x - OFFSET), -0.5f + (y - OFFSET),  0.5f + (z - OFFSET), -0.5f + (x - OFFSET));
					EastVertices.data[FaceSpacing + 1] = vec4(-0.5f + (y - OFFSET), -0.5f + (z - OFFSET), -0.5f + (x - OFFSET),  0.5f + (y - OFFSET));
					EastVertices.data[FaceSpacing + 2] = vec4(-0.5f + (z - OFFSET), -0.5f + (x - OFFSET),  0.5f + (y - OFFSET),  0.5f + (z - OFFSET));
					
					vec3 norms = vec3(-1, 0, 0);
					EastNormals.data[FaceSpacing]     = norms.xyzx;
					EastNormals.data[FaceSpacing + 1] = norms.yzxy;
					EastNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					EastUVs.data[FaceSpacing]     = vec4(1,1,0,0);
					EastUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
					EastUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					
					FaceSpacing += 3;
				}
			}
		}
	}
	
	EastCount.data = FaceSpacing/3;
}

void SouthFace() {
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					if (z != CHUNK_SIZE - 1 && ChunkData.data[x][y][z + 1] != 0) {
						continue;
					}
					
					SouthVertices.data[FaceSpacing]     = vec4( 0.5f + (x - OFFSET), -0.5f + (y - OFFSET),  0.5f + (z - OFFSET), -0.5f + (x - OFFSET));
					SouthVertices.data[FaceSpacing + 1] = vec4(-0.5f + (y - OFFSET),  0.5f + (z - OFFSET), -0.5f + (x - OFFSET),  0.5f + (y - OFFSET));
					SouthVertices.data[FaceSpacing + 2] = vec4( 0.5f + (z - OFFSET),  0.5f + (x - OFFSET),  0.5f + (y - OFFSET),  0.5f + (z - OFFSET));
					
					vec3 norms = vec3(0, 0, 1);
					SouthNormals.data[FaceSpacing]     = norms.xyzx;
					SouthNormals.data[FaceSpacing + 1] = norms.yzxy;
					SouthNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					SouthUVs.data[FaceSpacing]     = vec4(1,1,0,0);
					SouthUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
					SouthUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					
					FaceSpacing += 3;
				}
			}
		}
	}
	
	SouthCount.data = FaceSpacing/3;
}

void WestFace() {
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					
					
					if (x != CHUNK_SIZE - 1 && ChunkData.data[x+1][y][z] != 0) {
						continue;
					}
					
					WestVertices.data[FaceSpacing]     = vec4( 0.5f + (x - OFFSET), -0.5f + (y - OFFSET), -0.5f + (z - OFFSET),  0.5f + (x - OFFSET));
					WestVertices.data[FaceSpacing + 1] = vec4(-0.5f + (y - OFFSET),  0.5f + (z - OFFSET),  0.5f + (x - OFFSET),  0.5f + (y - OFFSET));
					WestVertices.data[FaceSpacing + 2] = vec4( 0.5f + (z - OFFSET),  0.5f + (x - OFFSET),  0.5f + (y - OFFSET),  -0.5f + (z - OFFSET));
					
					vec3 norms = vec3(1, 0, 0);
					WestNormals.data[FaceSpacing]     = norms.xyzx;
					WestNormals.data[FaceSpacing + 1] = norms.yzxy;
					WestNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					WestUVs.data[FaceSpacing]     = vec4(1,1,0,0);
					WestUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
					WestUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					
					FaceSpacing += 3;
				}
			}
		}
	}
	
	WestCount.data = FaceSpacing/3;
}

void BottomFace() {
	int FaceSpacing = 0;
	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				if (ChunkData.data[x][y][z] != 0) {
					if (y != 0 && ChunkData.data[x][y-1][z] != 0) {
						continue;
					}
					
					
					BottomVertices.data[FaceSpacing]     = vec4( 0.5f + (x - OFFSET), -0.5f + (y - OFFSET), -0.5f + (z - OFFSET),  -0.5f + (x - OFFSET));
					BottomVertices.data[FaceSpacing + 1] = vec4(-0.5f + (y - OFFSET),  -0.5f + (z - OFFSET),  -0.5f + (x - OFFSET),  -0.5f + (y - OFFSET));
					BottomVertices.data[FaceSpacing + 2] = vec4( 0.5f + (z - OFFSET),  0.5f + (x - OFFSET),  -0.5f + (y - OFFSET),  0.5f + (z - OFFSET));
					
					vec3 norms = vec3(0, -1, 0);
					BottomNormals.data[FaceSpacing]     = norms.xyzx;
					BottomNormals.data[FaceSpacing + 1] = norms.yzxy;
					BottomNormals.data[FaceSpacing + 2] = norms.zxyz;
					
					
					//find some better way to do this
					
					if (ChunkData.data[x][y][z] == 1) {
						BottomUVs.data[FaceSpacing]     = vec4(1,1,0,0);
						BottomUVs.data[FaceSpacing + 1] = vec4(0.5,0,0,0);
						BottomUVs.data[FaceSpacing + 2] = vec4(0,0.5,0,0);
					} else {
						BottomUVs.data[FaceSpacing]     = vec4(1,1,0,0);
						BottomUVs.data[FaceSpacing + 1] = vec4(1,0,0,0);
						BottomUVs.data[FaceSpacing + 2] = vec4(0,1,0,0);
					}
					FaceSpacing += 3;
				}
			}
		}
	}
	
	BottomCount.data = FaceSpacing/3;
}

void main() {
	
		switch (gl_GlobalInvocationID.x) {
		case 0:
			TopFace();
			break;
		case 1:
			NorthFace();
			break;
		case 2: 
			EastFace();
			break;
		case 3:
			SouthFace();
			break;
		case 4:
			WestFace();
			break;
		case 5: 
			BottomFace();
			break;
	}

	
	//x workgroup for each face direction. 

	
}

