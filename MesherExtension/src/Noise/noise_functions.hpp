#pragma once

#include <godot_cpp/variant/vector2.hpp>
#include <godot_cpp/variant/vector3.hpp>
#include <cmath>
#include <vector>
#include <numeric>
#include <algorithm>
#include <cstdint>
#include "Noise/open_simplex.h"

using namespace godot;


uint32_t encode_rgb888(float r, float g, float b) {
    uint8_t r8 = static_cast<uint8_t>(std::clamp(r, 0.0f, 1.0f) * 255.0f + 0.5f);
    uint8_t g8 = static_cast<uint8_t>(std::clamp(g, 0.0f, 1.0f) * 255.0f + 0.5f);
    uint8_t b8 = static_cast<uint8_t>(std::clamp(b, 0.0f, 1.0f) * 255.0f + 0.5f);

    return (r8 << 16) | (g8 << 8) | b8;
}


void decode_rgb888(uint32_t encoded, float &r, float &g, float &b) {
    uint8_t r8 = (encoded >> 16) & 0xFF;
    uint8_t g8 = (encoded >> 8) & 0xFF;
    uint8_t b8 = (encoded >> 0)  & 0xFF;

    r = r8 / 255.0f;
    g = g8 / 255.0f;
    b = b8 / 255.0f;
}

// Helper: Hash a Vector2 with seed into a float in [0, 1)
float hash_vec2(const Vector2& v, int seed) {
    float dot_val = v.x * 127.1f + v.y * 311.7f + seed * 0.001f;
    return std::fmod(std::sin(dot_val) * 43758.5453f, 1.0f);
}

inline Vector2 fract(const Vector2& v) {
    return Vector2(v.x - std::floor(v.x), v.y - std::floor(v.y));
}

// Helper: Hash into -1 to 1
float value_noise(const Vector2& p, int seed) {
    return hash_vec2(p, seed) * 2.0f - 1.0f;
}


// 2D noise vector from position
Vector2 gradient_noise(const Vector2& p, int seed) {
    float dx = value_noise(p + Vector2(5.2f, 1.3f), seed);
    float dy = value_noise(p + Vector2(9.8f, 2.6f), seed + 1);
    return Vector2(dx, dy);
}

// Fractal domain warp with multiple octaves
Vector2 fractal_domain_warp_value(const Vector2& pos, int seed, int octaves = 3, float warp_strength = 0.5f, float frequency = 1.0f, float lacunarity = 2.0f, float gain = 0.5f) {
    Vector2 p = pos * frequency;
    Vector2 warp = Vector2(0.0f, 0.0f);

    for (int i = 0; i < octaves; ++i) {
        Vector2 noise = gradient_noise(p + warp, seed + i * 17);
        warp += noise * warp_strength;

        warp_strength *= gain;
        frequency *= lacunarity;
        p = pos * frequency;
    }

    return pos + warp;
}

inline Vector2 floor(const Vector2& v) {
    return Vector2(std::floor(v.x), std::floor(v.y));
}

inline float mix(float a, float b, float t) {
    return a * (1.0f - t) + b * t;
}

inline Vector2 mix(const Vector2& a, const Vector2& b, float t) {
    return a * (1.0f - t) + b * t;
}

inline float dot(const Vector2& a, const Vector2& b) {
    return a.x * b.x + a.y * b.y;
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

//
// Hash function: pseudo-random gradient from grid point
//
Vector2 hash2(const Vector2& p, float seed) {
    float x = p.x;
    float y = p.y;
    float _dot = x * 127.1f + y * 311.7f + seed * 0.01f;
    float s = std::sin(_dot) * 43758.5453f;
    float frac_x = s - std::floor(s);

    _dot = x * 269.5f + y * 183.3f + seed * 0.017f;
    s = std::sin(_dot) * 43758.5453f;
    float frac_y = s - std::floor(s);

    // Convert [0,1) to [-1,1) and normalize
    Vector2 g = Vector2(frac_x * 2.0f - 1.0f, frac_y * 2.0f - 1.0f);
    float len = std::sqrt(dot(g, g));
    return g * (1.0f / (len + 1e-6f));
}

//
// Perlin noise function
//
float perlinNoise2D(const Vector2& p, float seed) {
    Vector2 i = floor(p);
    Vector2 f = fract(p);

    Vector2 g00 = hash2(i + Vector2(0.0f, 0.0f), seed);
    Vector2 g10 = hash2(i + Vector2(1.0f, 0.0f), seed);
    Vector2 g01 = hash2(i + Vector2(0.0f, 1.0f), seed);
    Vector2 g11 = hash2(i + Vector2(1.0f, 1.0f), seed);

    float d00 = dot(g00, f - Vector2(0.0f, 0.0f));
    float d10 = dot(g10, f - Vector2(1.0f, 0.0f));
    float d01 = dot(g01, f - Vector2(0.0f, 1.0f));
    float d11 = dot(g11, f - Vector2(1.0f, 1.0f));

    Vector2 u = f * f * (Vector2(3.0f, 3.0f) - f * 2.0f); // Smoothstep

    float mix_x0 = mix(d00, d10, u.x);
    float mix_x1 = mix(d01, d11, u.x);
    return mix(mix_x0, mix_x1, u.y);
}

// Domain warp: displace input coordinates using pseudo-random gradient
Vector2 domain_warp(const Vector2& pos, float strength, int seed) {
    float dx = perlinNoise2D(pos + Vector2(5.2f, 1.3f), seed);
    float dy = perlinNoise2D(pos + Vector2(9.8f, 2.6f), seed + 1);
    return pos + Vector2(dx, dy) * strength; // 0.5 = warp strength
}

// Warp function
Vector2 fractal_domain_warp_simplex(const Vector2& pos, int64_t seed,
                            int octaves = 3,
                            double warp_strength = 0.5,
                            double frequency = 1.0,
                            double lacunarity = 2.0,
                            double gain = 0.5)
{
    Vector2 total_warp(0.0, 0.0);
    Vector2 base_pos = pos;
    
    for (int i = 0; i < octaves; ++i) {
        //OpenSimplex2 noise(seed + i * 37); // new seed per octave
        Vector2 p = base_pos + total_warp;
        /*
        double nx = noise.noise(p.x * frequency, p.y * frequency);
        double ny = noise.noise((p.x + 100.0) * frequency, (p.y - 100.0) * frequency); // decorrelated axis*/
        double nx = perlinNoise2D(p * frequency, seed);
        double ny = perlinNoise2D((p + Vector2(100, -100)) * frequency, seed);

        Vector2 offset(nx, ny);
        total_warp = total_warp + (offset * warp_strength);

        warp_strength *= gain;
        frequency *= lacunarity;
    }

    return pos + total_warp;
}


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

inline float smooth_sigmoid(float t) {
    // Clamp t for safety
    t = std::clamp(t, 0.0f, 1.0f);
    // Smooth sigmoid-like transition: logistic approximation
    float steepness = 10.0f; // increase for steeper transition
    return 1.0f / (1.0f + std::exp(-steepness * (t - 0.5f)));
}

inline float sigmoid_lerp(float a, float b, float t) {
    float s = smooth_sigmoid(t);
    return a + (b - a) * s;
}

// Linear interpolation
inline float lerp(float a, float b, float t) {
    return a + t * (b - a);
}

int lerp_round(int a, int b, float t) {
    return static_cast<int>(std::round(a + (b - a) * t));
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


// --- Gradient utility ---
inline float fade(float t) {
    return t * t * t * (t * (t * 6 - 15) + 10);
}


inline float grad(int hash, float x, float y) {
    // Convert low 4 bits of hash code into 12 gradient directions
    int h = hash & 7;
    float u = h < 4 ? x : y;
    float v = h < 4 ? y : x;
    return ((h & 1) ? -u : u) + ((h & 2) ? -2.0f * v : 2.0f * v);
}

// --- Permutation table ---
static int p[512];
static bool initialized = false;


float fbm2(const Vector2& pos, int octaves = 1, float lacunarity = 2.0f, float gain = 0.5f) {
    float amplitude = 0.5f;
    float frequency = 1.0f;
    float sum = 0.0f;

    Vector2 p = pos;

    for (int i = 0; i < octaves; i++) {
        OpenSimplex2 os(5.0);
        sum += amplitude * os.noise(p.x * frequency, p.y * frequency);
        amplitude *= gain;
        frequency *= lacunarity;
    }

    return sum;
}


// Displacement function (you can replace this with your own)
Vector2 displacement(const Vector2 &pos) {
    // Simple noise-like displacement for demonstration
    float n = fbm2(pos);
    float angle = n * 6.2831;
    return Vector2(cos(angle), sin(angle)) * 0.2;
}

int biome_id_from_voronoi(const Vector3 &pos, float seed) {
    Vector2 cell = Vector2(std::floor(pos.x), std::floor(pos.z));
    Vector2 frac = Vector2(fract(pos.x), fract(pos.z));

    frac += displacement(cell + frac) * 0.15;

    float min_dist = 1000.0f;
    Vector2 nearest_cell = Vector2(0, 0);

    for (int x = -1; x <= 1; ++x) {
        for (int z = -1; z <= 1; ++z) {
            Vector2 neighbor(x, z);
            Vector2 lattice_cell = cell + neighbor;

            Vector2 feature_point = fract(Vector2(
                std::sin(dot(lattice_cell, Vector2(127.1, 74.7))),
                std::sin(dot(lattice_cell, Vector2(113.5, 372.3)))
            ) * (43758.5453f + seed));

            Vector2 diff = neighbor + feature_point - frac;
            float dist = diff.length();

            if (dist < min_dist) {
                min_dist = dist;
                nearest_cell = lattice_cell;
            }
        }
    }

    float h = fract(std::sin(dot(nearest_cell, Vector2(12.9898, 45.164))) * 43758.5453f);
    return (static_cast<int>(std::floor(h * 256)));
}

void voronoi_data(const Vector3 &pos, float seed, int& primary_biome, int& secondary_biome, float& distance) {
    Vector2 cell = Vector2(std::floor(pos.x), std::floor(pos.z));
    Vector2 frac = Vector2(fract(pos.x), fract(pos.z));

    frac += displacement(cell + frac) * 0.15;

    float min_dist = 1000.0f;
    float second_min_dist = 1000.0f;
    Vector2 nearest_cell;
    Vector2 secondary_cell;

    for (int x = -1; x <= 1; ++x) {
        for (int z = -1; z <= 1; ++z) {
            Vector2 neighbor(x, z);
            Vector2 lattice_cell = cell + neighbor;

            Vector2 feature_point = fract(Vector2(
                std::sin(dot(lattice_cell, Vector2(127.1, 74.7))),
                std::sin(dot(lattice_cell, Vector2(113.5, 372.3)))
            ) * (43758.5453f + seed));

            Vector2 diff = neighbor + feature_point - frac;
            float dist = diff.length();

            if (dist < min_dist) {
                // Push current min down to second
                second_min_dist = min_dist;
                secondary_cell = nearest_cell;

                // Set new min
                min_dist = dist;
                nearest_cell = lattice_cell;
            } else if (dist < second_min_dist) {
                // Found new second closest
                second_min_dist = dist;
                secondary_cell = lattice_cell;
            }
        }
    }

    float h = fract(std::sin(dot(nearest_cell, Vector2(12.9898, 45.164))) * 43758.5453f);
    primary_biome = (static_cast<int>(std::floor(h * 256)));

    float g = fract(std::sin(dot(secondary_cell, Vector2(12.9898, 45.164))) * 43758.5453f);
    secondary_biome = (static_cast<int>(std::floor(g * 256)));

    // Compute edge proximity (0 = center, 1 = edge)
    float edge_factor = (second_min_dist - min_dist) / (second_min_dist + 1e-5f);
    distance = std::clamp(1.0f - edge_factor, 0.0f, 1.0f);
}

/*old
void voronoi_data(const Vector3 &pos, float seed, int& primary_biome, int& secondary_biome, float& distance) {
    Vector2 cell = Vector2(std::floor(pos.x), std::floor(pos.z));
    Vector2 frac = Vector2(fract(pos.x), fract(pos.z));

    frac += displacement(cell + frac) * 0.15;

    float min_dist = 1000.0f;
    float second_min_dist = 1000.0f;
    Vector2 nearest_cell = Vector2(0, 0);
    Vector2 secondary_cell = Vector2(0, 0);

    for (int x = -1; x <= 1; ++x) {
        for (int z = -1; z <= 1; ++z) {
            Vector2 neighbor(x, z);
            Vector2 lattice_cell = cell + neighbor;

            Vector2 feature_point = fract(Vector2(
                std::sin(dot(lattice_cell, Vector2(127.1, 74.7))),
                std::sin(dot(lattice_cell, Vector2(113.5, 372.3)))
            ) * (43758.5453f + seed));

            Vector2 diff = neighbor + feature_point - frac;
            float dist = diff.length();

            if (dist < min_dist) {
                second_min_dist = min_dist;
                secondary_cell = nearest_cell;
                min_dist = dist;
                nearest_cell = lattice_cell;
            } else if (dist < second_min_dist) {
                second_min_dist = dist;
                secondary_cell = lattice_cell;
            }
        }
    }

    float h = fract(std::sin(dot(nearest_cell, Vector2(12.9898, 45.164))) * 43758.5453f);
    primary_biome = (static_cast<int>(std::floor(h * 254)) + 1) % 3;
    float g = fract(std::sin(dot(secondary_cell, Vector2(12.9898, 45.164))) * 43758.5453f);
    secondary_biome = (static_cast<int>(std::floor(g * 254)) + 1) % 3;
    distance = 1.0f - (min_dist / (second_min_dist + 1e-5f)); // add epsilon to avoid div-by-zero
    distance = std::clamp(distance, 0.0f, 1.0f);
}*/