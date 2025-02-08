#[compute]
#version 450

const int CHUNK_SIDE_LENGTH = 64;
const int CHUNK_DATA_LENGTH = 64 + 2;

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 2, std430) buffer chunkbuffer {
	int chunk[CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH][CHUNK_DATA_LENGTH];
} ChunkBuffer; 

layout(set = 0, binding = 3, std430) buffer chunkdimensions{
	float ChunkSize;
	float WorkGroupSide;
	vec2 padding;
	vec4 ChunkCoordinate;
} ChunkDimensions;

vec2 hash2(vec2 p, float seed) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p + seed) * 43758.5453123);
}

float perlinNoise2D(vec2 p, float seed) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    vec2 g00 = hash2(i + vec2(0.0, 0.0), seed);
    vec2 g10 = hash2(i + vec2(1.0, 0.0), seed);
    vec2 g01 = hash2(i + vec2(0.0, 1.0), seed);
    vec2 g11 = hash2(i + vec2(1.0, 1.0), seed);

    float d00 = dot(g00, f - vec2(0.0, 0.0));
    float d10 = dot(g10, f - vec2(1.0, 0.0));
    float d01 = dot(g01, f - vec2(0.0, 1.0));
    float d11 = dot(g11, f - vec2(1.0, 1.0));

    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(d00, d10, u.x), mix(d01, d11, u.x), u.y);
}

void main () {
	int WorkGroupDataLength = 2;

	int Gx = int(floor(gl_GlobalInvocationID.x));
	int Gy = int(floor(gl_GlobalInvocationID.y));
	int Gz = int(floor(gl_GlobalInvocationID.z));
	
	vec3 ChunkPosition = ChunkDimensions.ChunkCoordinate.xyz;
	float seed = 5.0;
	
	for (int x = Gx * WorkGroupDataLength; x < (Gx + 1) * WorkGroupDataLength; x++) {
		for (int y = Gy * WorkGroupDataLength; y < (Gy + 1) * WorkGroupDataLength; y++) {
			for (int z = Gz * WorkGroupDataLength; z < (Gz + 1) * WorkGroupDataLength; z++) {
				
				if (perlinNoise2D(ChunkPosition.xz + vec2(float(x - 1) / 64.0, float(z - 1) / 64.0), seed) * 33 + 33 > float(y)) {
					ChunkBuffer.chunk[x][y][z] = 2;
				} else {
					ChunkBuffer.chunk[x][y][z] = 0;
				}
			}
		}
	} 
}