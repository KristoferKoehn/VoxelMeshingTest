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

// Hash function to generate pseudo-random gradients in 3D
vec3 hash3(vec3 p, float seed) {
    p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
             dot(p, vec3(269.5, 183.3, 246.1)),
             dot(p, vec3(113.5, 271.9, 372.3)));
    return -1.0 + 2.0 * fract(sin(p + seed) * 43758.5453123);
}

// 3D Perlin noise function
float perlinNoise3D(vec3 p, float seed) {
    vec3 i = floor(p);
    vec3 f = fract(p);

    // Compute gradients at the eight corners of the cell
    vec3 g000 = hash3(i + vec3(0.0, 0.0, 0.0), seed);
    vec3 g100 = hash3(i + vec3(1.0, 0.0, 0.0), seed);
    vec3 g010 = hash3(i + vec3(0.0, 1.0, 0.0), seed);
    vec3 g110 = hash3(i + vec3(1.0, 1.0, 0.0), seed);
    vec3 g001 = hash3(i + vec3(0.0, 0.0, 1.0), seed);
    vec3 g101 = hash3(i + vec3(1.0, 0.0, 1.0), seed);
    vec3 g011 = hash3(i + vec3(0.0, 1.0, 1.0), seed);
    vec3 g111 = hash3(i + vec3(1.0, 1.0, 1.0), seed);

    // Compute dot products between gradient and position offset
    float d000 = dot(g000, f - vec3(0.0, 0.0, 0.0));
    float d100 = dot(g100, f - vec3(1.0, 0.0, 0.0));
    float d010 = dot(g010, f - vec3(0.0, 1.0, 0.0));
    float d110 = dot(g110, f - vec3(1.0, 1.0, 0.0));
    float d001 = dot(g001, f - vec3(0.0, 0.0, 1.0));
    float d101 = dot(g101, f - vec3(1.0, 0.0, 1.0));
    float d011 = dot(g011, f - vec3(0.0, 1.0, 1.0));
    float d111 = dot(g111, f - vec3(1.0, 1.0, 1.0));

    // Smooth interpolation
    vec3 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix(d000, d100, u.x), mix(d010, d110, u.x), u.y), mix(mix(d001, d101, u.x), mix(d011, d111, u.x), u.y), u.z);
}


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
				
				if (perlinNoise2D(ChunkPosition.xz + vec2(float(x - 1) / 64.0, float(z - 1) / 64.0), seed) * 0.0 + 33 > float(y)) {
					
					if (perlinNoise3D(ChunkPosition.xyz + vec3(float(x - 1) / 64.0, float(y - 1) / 64.0, float(z - 1) / 64.0), seed) > 0) {
						ChunkBuffer.chunk[x][y][z] = 2;
					} else {
						ChunkBuffer.chunk[x][y][z] = 0;
					}
					//ChunkBuffer.chunk[x][y][z] = 2;
				} else {
					ChunkBuffer.chunk[x][y][z] = 0;
				}
			}
		}
	} 
}