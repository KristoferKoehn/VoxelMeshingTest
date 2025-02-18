shader_type spatial;
const float seed1 = 0.223;

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


void vertex() {

}

void fragment() {
	/*
	vec3 pos = NODE_POSITION_WORLD + (INV_VIEW_MATRIX * vec4(VIEW * FRAGCOORD.z,0)).xyz;
	pos = pos * 10.0;
	float r = perlinNoise3D(pos, 0.13);
	float b = perlinNoise3D(pos, 0.321);*/
	ALBEDO = COLOR.rgb;// + vec3(r, 0, b);
}