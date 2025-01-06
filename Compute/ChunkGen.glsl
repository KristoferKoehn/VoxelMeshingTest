#[compute]
#version 450

const int CHUNK_SIDE_LENGTH = 64;
const int CHUNK_DATA_LENGTH = 64 + 2;

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;


layout(set = 0, binding = 0, std430) buffer cutoffbuffer {
	float Layer1[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
	float Layer2[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
	float Layer3[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
} CutoffBuffer;


layout(set = 0, binding = 1, std430) buffer noisebuffer {
	float Terrain1[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
	float Terrain2[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
	float Terrain3[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
} NoiseBuffer;

layout(set = 0, binding = 2, std430) buffer chunkbuffer {
	int chunk[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
} ChunkBuffer; 

layout(set = 0, binding = 3, std430) buffer chunkdimensions{
	int ChunkSize;
	int WorkGroupSide;
} ChunkDimensions;

void main () {
	int WorkGroupDataLength = ChunkDimensions.ChunkSize / ChunkDimensions.WorkGroupSide;

	int Gx = int(floor(gl_GlobalInvocationID.x));
	int Gy = int(floor(gl_GlobalInvocationID.y));
	int Gz = int(floor(gl_GlobalInvocationID.z));
	
	for (int x = Gx * WorkGroupDataLength; x < (Gx + 1) * WorkGroupDataLength; x++) {
		for (int y = Gy * WorkGroupDataLength; y < (Gy + 1) * WorkGroupDataLength; y++) {
			for (int z = Gz * WorkGroupDataLength; z < (Gz + 1) * WorkGroupDataLength; z++) {
				if (CutoffBuffer.Layer2[z][x] > 0.5 && CutoffBuffer.Layer3[z][x] > 0.5) {
					if(NoiseBuffer.Terrain1[z][y][x] > 0.7 && (12.0 + CutoffBuffer.Layer1[z][x] * 18) > y) {
						ChunkBuffer.chunk[x][y][z] = 1;
					} else {
						ChunkBuffer.chunk[x][y][z] = 0;
					}
				} else if (CutoffBuffer.Layer2[z][x] > 0.5 && CutoffBuffer.Layer3[z][x] < 0.5) {
					if(NoiseBuffer.Terrain1[z][y][x] > 0.7 && (12.0 + CutoffBuffer.Layer1[z][x] * 18) > y) {
						ChunkBuffer.chunk[x][y][z] = 2;
					} else {
						ChunkBuffer.chunk[x][y][z] = 0;
					}
				} else if (CutoffBuffer.Layer2[z][x] > 0.5 && CutoffBuffer.Layer2[z][x] > 0.5) {
					if(NoiseBuffer.Terrain3[z][y][x] > 0.7 && (12.0 + CutoffBuffer.Layer1[z][x] * 18) > y) {
						ChunkBuffer.chunk[x][y][z] = 3;
					} else {
						ChunkBuffer.chunk[x][y][z] = 0;
					}
				}  else if (CutoffBuffer.Layer2[z][x] < 0.5 && CutoffBuffer.Layer3[z][x] > 0.5) {
					if(NoiseBuffer.Terrain2[z][y][x] > 0.7 && (12.0 + CutoffBuffer.Layer1[z][x] * 18) > y) {
						ChunkBuffer.chunk[x][y][z] = 4;
					} else {
						ChunkBuffer.chunk[x][y][z] = 0;
					}
				} else {
					ChunkBuffer.chunk[x][y][z] = 0;
				}
			}
		}
	} 
}