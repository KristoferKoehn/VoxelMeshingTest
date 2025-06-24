#include <godot_cpp/variant/vector2.hpp>
#include <cmath>

using namespace godot;

#include <cmath>
#include <cstdint>

// Basic hash to float in [0,1]
float hash3d(int x, int y, int z, uint32_t seed = 0) {
    uint32_t h = x * 374761393 + y * 668265263 + z * 14466567 + seed * 374761393;
    h = (h ^ (h >> 13)) * 1274126177;
    return (h & 0x7FFFFFFF) / float(0x7FFFFFFF);
}

// Smoothstep interpolation
inline float smoothstep(float t) {
    return t * t * (3 - 2 * t);
}

// Linear interpolation
inline float lerp(float a, float b, float t) {
    return a + t * (b - a);
}

// Helper: fract for float
inline float fract(float f) {
    return f - std::floor(f);
}

// Helper: fract for Vector2
inline Vector2 fract_vec(const Vector2 &v) {
    return Vector2(fract(v.x), fract(v.y));
}

// Helper: floor for Vector2
inline Vector2 floor_vec(const Vector2 &v) {
    return Vector2(std::floor(v.x), std::floor(v.y));
}

// Pseudo-random feature point generator using seed
Vector2 random2_seeded(const Vector2 &p, float seed) {
    float dot1 = p.dot(Vector2(127.1f, 311.7f)) + seed;
    float dot2 = p.dot(Vector2(269.5f, 183.3f)) + seed;
    return Vector2(
        fract(std::sin(dot1) * 43758.5453f),
        fract(std::sin(dot2) * 43758.5453f)
    );
}

int hash_biome(const Vector2 &p, float seed) {
    float h = fract(std::sin(p.dot(Vector2(12.9898f, 78.233f)) + seed) * 43758.5453f);
    return int(h * 4.0f); // Change 4.0f to biome count if desired
}

float value_noise_3d(float x, float y, float z, uint32_t seed = 0) {
    int xi = int(floor(x));
    int yi = int(floor(y));
    int zi = int(floor(z));

    float xf = x - xi;
    float yf = y - yi;
    float zf = z - zi;

    float u = smoothstep(xf);
    float v = smoothstep(yf);
    float w = smoothstep(zf);

    float c000 = hash3d(xi,     yi,     zi,     seed);
    float c100 = hash3d(xi + 1, yi,     zi,     seed);
    float c010 = hash3d(xi,     yi + 1, zi,     seed);
    float c110 = hash3d(xi + 1, yi + 1, zi,     seed);
    float c001 = hash3d(xi,     yi,     zi + 1, seed);
    float c101 = hash3d(xi + 1, yi,     zi + 1, seed);
    float c011 = hash3d(xi,     yi + 1, zi + 1, seed);
    float c111 = hash3d(xi + 1, yi + 1, zi + 1, seed);

    float x00 = lerp(c000, c100, u);
    float x10 = lerp(c010, c110, u);
    float x01 = lerp(c001, c101, u);
    float x11 = lerp(c011, c111, u);

    float y0 = lerp(x00, x10, v);
    float y1 = lerp(x01, x11, v);

    return lerp(y0, y1, w); // Range [0,1]
}

float bitcrush_noise(float x, float y, float scale = 8.0f) {
    return fmod(floor(x * scale) + floor(y * scale), 17.0f) / 17.0f;
}


float fbm_3d(float x, float y, float z, int octaves = 4, float lacunarity = 2.0f, float gain = 0.5f, uint32_t seed = 0) {
    float amplitude = 0.5f;
    float frequency = 1.0f;
    float sum = 0.0f;

    for (int i = 0; i < octaves; ++i) {
        sum += value_noise_3d(x * frequency, y * frequency, z * frequency, seed + i * 31) * amplitude;
        frequency *= lacunarity;
        amplitude *= gain;
    }

    return sum; // Approx. in range [0,1] for typical use
}

// Main Voronoi function with seed
int biome_from_voronoi(const Vector2 &pos, float seed) {
    Vector2 cell = floor_vec(pos);
    Vector2 frac = fract_vec(pos);

    float min_dist = 10000.0f;
    Vector2 closest_cell;

    for (int y = -1; y <= 1; ++y) {
        for (int x = -1; x <= 1; ++x) {
            Vector2 offset((float)x, (float)y);
            Vector2 lattice = cell + offset;
            Vector2 feature = random2_seeded(lattice, seed);
            Vector2 diff = offset + feature - frac;

            float dist = diff.length();
            if (dist < min_dist) {
                min_dist = dist;
                closest_cell = lattice;
            }
        }
    }

    return hash_biome(closest_cell, seed);
}
