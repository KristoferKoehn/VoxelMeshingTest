#pragma once
#include <godot_cpp/variant/vector3.hpp>
#include <cstdint>
#include <functional>
#include <unordered_map>
#include <string>

using CutoffFunc = uint32_t (*)(godot::Vector2, int);


// --- Function definitions ---
static uint32_t rolling_hills(godot::Vector2 pos, int seed) {
	OpenSimplex2 os(seed);
	uint32_t s = os.noise((pos.y) * 0.025, (pos.x) * 0.025) * 4 + 32;
	return s;
}

static uint32_t cutoff_mountains(godot::Vector2 pos, int seed) {
	OpenSimplex2 os(seed);
	uint32_t s = os.noise((pos.y) * 0.025, (pos.x) * 0.025) * 24 + 32;
	return s;
}

static uint32_t cutoff_valleys(godot::Vector2 pos, int seed) {
	OpenSimplex2 os(seed);
	Vector2 v = fractal_domain_warp_simplex(pos, seed, 20.0, 4, 0.05, 0.0, 0.5) * 0.025;
	uint32_t s = os.noise(v.x, v.y) * 6 + 30;
	return s;
}

// --- Static function registry ---
inline const std::unordered_map<std::string, CutoffFunc>& get_cutoff_function_map() {
	static const std::unordered_map<std::string, CutoffFunc> funcs = {
		{ "rolling_hill", rolling_hills},
		{ "mountains", cutoff_mountains },
		{ "valleys",   cutoff_valleys },
		// Add more here...
	};
	return funcs;
}
