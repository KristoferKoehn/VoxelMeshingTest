#pragma once
#include "Noise/noise_functions.hpp"
#include <godot_cpp/variant/vector3.hpp>
#include <godot_cpp/variant/vector2.hpp>
#include <cstdint>
#include <functional>
#include <unordered_map>
#include <string>

using BiomeFunc = uint32_t (*)(godot::Vector3, int);

// --- Function definitions ---

static uint32_t rolling_hills(godot::Vector3 pos, int seed) {
    return encode_rgb888(0.1, 0.75, 0);
}

static uint32_t noise_mountains(godot::Vector3 pos, int seed) {
    if (pos.y > 48 + fbm2(Vector2(pos.x, pos.z)) * 4) {
        return encode_rgb888(1.0, 1.0, 1.0);
    } else {
        return encode_rgb888(0.15, 0.15, 0.1);
    }
}

static uint32_t noise_valleys(godot::Vector3 pos, int seed) {
    return encode_rgb888( 250.0 / 256.0, 21004.0 / 256.0, 127.0 / 256.0);
}

inline const std::unordered_map<std::string, BiomeFunc>& get_biome_function_map() {
    static const std::unordered_map<std::string, BiomeFunc> funcs = {
        { "rolling_hill", rolling_hills },
        { "mountains",     noise_mountains },
        { "valleys",       noise_valleys },
        // Add more here...
    };
    return funcs;
}

inline const int get_biome_count() {
    return get_biome_function_map().size();
}