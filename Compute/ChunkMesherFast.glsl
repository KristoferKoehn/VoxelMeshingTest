#[compute]
#version 450

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int CHUNK_SIZE = 64;

//should be vec4[3] for vertices, three floats for normal (x,y,z), then 3 vec2s for UVs.
//that comes out to 84 bytes instead of the current 144

//^^ that stuff is old, it's now 64 bytes :sunglasses:
struct Quad {
	vec4[3] vertices;
	float UVIndex;
	float Normx;
	float Normy;
	float Normz;
};

layout(set = 0, binding = 0, std430) buffer quads{
	Quad data[];
} Quads;

layout(set = 0, binding = 1, std430) buffer quadcount{
	uint count;
} QuadCount;

layout(set = 0, binding = 2, std430) buffer chunkdata{
	int data[CHUNK_SIZE][CHUNK_SIZE][CHUNK_SIZE];
} ChunkData;

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

const vec4[3] UV = vec4[3](vec4(1,1,0,0), vec4(1,0,0,0), vec4(0,1,0,0));

vec4[3] AddPosition(vec4[3] vert, vec3 pos) {
	vert[0] = vert[0] + pos.xyzx;
	vert[1] = vert[1] + pos.yzxy;
	vert[2] = vert[2] + pos.zxyz;
	return vert;
}

void main () {
	QuadCount.count = 0;

	for (int x = 0; x < CHUNK_SIZE; x++) {
		for (int y = 0; y < CHUNK_SIZE; y++) {
			for (int z = 0; z < CHUNK_SIZE; z++) {
				
				if(ChunkData.data[x][y][z] == 0) {
					continue;
				}
				
				//there's something funky going on with the face culling.
				//I'm so tired
				
				
				//if y = CHUNK_SIZE - 1, make face.
				//if not, check if above block is real. if not, make face
				
				
				if (y == CHUNK_SIZE - 1 || (y != CHUNK_SIZE -1 && ChunkData.data[x][y+1][z] == 0)) {
					Quad f;
					f.vertices = AddPosition( TopQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = TopNormal.xyzx;
					f.Normx = TopNormal.x;
					f.Normy = TopNormal.y;
					f.Normz = TopNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				} 
				
				if (z == 0 || (z != 0 && ChunkData.data[x][y][z-1] == 0)) {
					Quad f;
					f.vertices = AddPosition( NorthQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = NorthNormal.xyzx;
					f.Normx = NorthNormal.x;
					f.Normy = NorthNormal.y;
					f.Normz = NorthNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				}
				
				if (x == 0 || ( x != 0 && ChunkData.data[x-1][y][z] == 0)) {
					Quad f;
					f.vertices = AddPosition( EastQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = EastNormal.xyzx;
					f.Normx = EastNormal.x;
					f.Normy = EastNormal.y;
					f.Normz = EastNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				}	

				if (z == CHUNK_SIZE - 1 || (z != CHUNK_SIZE - 1 && ChunkData.data[x][y][z+1] == 0)) {
					Quad f;
					f.vertices = AddPosition( SouthQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = SouthNormal.xyzx;
					f.Normx = SouthNormal.x;
					f.Normy = SouthNormal.y;
					f.Normz = SouthNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				}
				
				if (x == CHUNK_SIZE - 1 || (x != CHUNK_SIZE - 1 && ChunkData.data[x+1][y][z] == 0)) {
					Quad f;
					f.vertices = AddPosition(WestQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = WestNormal.xyzx;
					f.Normx = WestNormal.x;
					f.Normy = WestNormal.y;
					f.Normz = WestNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				}
				
				if (y == 0 || ( y != 0 && ChunkData.data[x][y - 1][z] == 0)) {
					Quad f;
					f.vertices = AddPosition( BottomQuadVertices, vec3(x - CHUNK_SIZE/2, y - CHUNK_SIZE/2, z - CHUNK_SIZE/2));
					//f.normals = BottomNormal.xyzx;
					f.Normx = BottomNormal.x;
					f.Normy = BottomNormal.y;
					f.Normz = BottomNormal.z;
					f.UVIndex = QuadCount.count % 6;
					Quads.data[QuadCount.count] = f;
					QuadCount.count++;
				}
			}
		}
	}
}