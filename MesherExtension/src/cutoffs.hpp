#pragma once
#include <godot_cpp/variant/vector3.hpp>
#include <cstdint>
#include <functional>
#include <unordered_map>
#include <string>

using CutoffFunc = int32_t (*)(godot::Vector2, int);


// --- Function definitions ---
static int32_t rolling_hills(godot::Vector2 pos, int seed) {
	/*
	OpenSimplex2 os(seed);
	uint32_t s = os.noise((pos.y) * 0.025, (pos.x) * 0.025) * 4 + 32;*/

	int32_t s = perlinNoise2D(pos * 0.025 , seed) * 12 + 25;
	return s;
}

static int32_t cutoff_mountains(godot::Vector2 pos, int seed) {
	OpenSimplex2 os(seed);
	int32_t s = os.noise((pos.y) * 0.02, (pos.x) * 0.02) * 18 + 30;
	return s;
}

static int32_t cutoff_desert(godot::Vector2 pos, int seed) {
	//OpenSimplex2 os(seed);
	//Vector2 v = fractal_domain_warp_simplex(pos, seed, 3, 20.0, 4.0, 3.0, 1.0);
	int32_t s = perlinNoise2D(pos * 0.03, seed) * 20 + 25;
	return s;
}

// --- Static function registry ---
inline const std::unordered_map<std::string, CutoffFunc>& get_cutoff_function_map() {
	static const std::unordered_map<std::string, CutoffFunc> funcs = {
		{ "grassland", rolling_hills},
		{ "mountains", cutoff_mountains },
		{ "desert",   cutoff_desert },
		// Add more here...
	};
	return funcs;
}
