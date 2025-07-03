//#include "Biomes/biome_registry.hpp"
#include <thread>
#include "gdexample.h"
#include "Noise/noise_functions.hpp"
#include "biomes.hpp"
#include "cutoffs.hpp"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/packed_vector3_array.hpp>
#include <godot_cpp/variant/packed_color_array.hpp>
#include <godot_cpp/variant/packed_int32_array.hpp>
#include <vector>
#include <array>
#include <cstdint>
#include <functional>
#include <chrono>
#include <string>
#include <iomanip> // For std::setw
#include <sstream> // For std::ostringstream

using namespace godot;

std::vector<uint32_t> GDExample::arr(66 * 66 * 66);
std::unordered_map<uint32_t, std::string> keys;
int biome_count = 0;

constexpr float SCALE = 1.0f/512.0f;

void GDExample::_bind_methods()
{
	//Ref<ArrayMesh> GDExample::generate_and_mesh(Vector3 pos) {
	ClassDB::bind_method(D_METHOD("generate_and_mesh", "pos"), &GDExample::generate_and_mesh);
}

GDExample::GDExample() {
    print_line_rich("=== [pulse]loaded mesher extension[/pulse] ===");
    auto& biome_map = get_biome_function_map();
    auto& cutoff_map = get_cutoff_function_map();
    biome_count = biome_map.size();
    if (biome_map.size() != cutoff_map.size()) {
        print_line_rich("[color=yellow]!!! [pulse] MAPPING SIZE MISMATCH [/pulse] !!![/color]");
    } else {
        print_line_rich("[color=green][[[ [pulse]  MAPPING SIZE MATCH [/pulse]   ]]][/color]");
    }

    std::set<std::string> all_keys;
    uint8_t i = 0;
    for (const auto& [name, _] : biome_map) {
        all_keys.insert(name);
        keys[i] = name;
        i++;
    }
    for (const auto& [name, _] : cutoff_map) {
        all_keys.insert(name);
    }

    size_t max_len = 0;
    for (const auto& name : all_keys) {
        max_len = std::max(max_len, name.length());
    }

    // Column widths
    int name_width = static_cast<int>(max_len) + 3;
    int col_width  = 10;

    //magic formatting
    print_line_rich(vformat("%*s   %s     %s", max_len, " ", "biome", "cutoff"));

    for (const auto& name : all_keys) {
        bool has_biome = biome_map.find(name) != biome_map.end();
        bool has_cutoff = cutoff_map.find(name) != cutoff_map.end();

        const char* biome_status = has_biome ? "[pulse][color=green]OK[/color][/pulse]" : "[pulse][color=red] X[/color][/pulse]";
        const char* cutoff_status = has_cutoff ? "[pulse][color=green]OK[/color][/pulse]" : "[pulse][color=red] X[/color][/pulse]";

        print_line_rich(vformat("%-*s  %-2s         %-2s", name_width, name.c_str(), biome_status, cutoff_status));
    }
    print_line_rich("-------------------------------");
}



GDExample::~GDExample() {
	// Add your cleanup here.
}

void GDExample::_ready() {
	
}


constexpr int CHUNK_SIZE = 64;
constexpr int PAD = 1;
constexpr int PADDED_SIZE = CHUNK_SIZE + 2 * PAD;
constexpr int PADDED_VOLUME = PADDED_SIZE * PADDED_SIZE * PADDED_SIZE;


inline int index_3d(int x, int y, int z) {
    return x * PADDED_SIZE * PADDED_SIZE + y * PADDED_SIZE + z;
}

// Mask for one axis slice
struct MaskSlice {
    uint64_t solid[CHUNK_SIZE];       // 1 bit per voxel along Z
    uint32_t  type[CHUNK_SIZE][CHUNK_SIZE]; // per voxel type
};

// Greedy meshing with typed bitmask mask
void greedy_merge(const MaskSlice& slice, int axis, int depth, const std::function<void(int, int, int, int, int, int, uint32_t)>& emit_func) {
    bool visited[CHUNK_SIZE][CHUNK_SIZE] = {};

    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = slice.solid[y];
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            if (!(row & (1ULL << z)) || visited[y][z]) continue;
            uint32_t t = slice.type[y][z];

            // Greedy width (z-axis)
            int w = 1;
            while (z + w < CHUNK_SIZE && (row & (1ULL << (z + w))) && slice.type[y][z + w] == t && !visited[y][z + w]) w++;

            // Greedy height (y-axis)
            int h = 1;
            while (y + h < CHUNK_SIZE) {
                bool match = true;
                for (int dz = 0; dz < w; ++dz) {
                    if (!(slice.solid[y + h] & (1ULL << (z + dz))) || slice.type[y + h][z + dz] != t || visited[y + h][z + dz]) {
                        match = false;
                        break;
                    }
                }
                if (!match) {
					break;
				} 
                ++h;
            }

            // Mark visited
            for (int dy = 0; dy < h; ++dy)
                for (int dz = 0; dz < w; ++dz)
                    visited[y + dy][z + dz] = true;

            emit_func(axis, depth, y, z, w, h, t);

        }
    }
}

// +X face
void build_x_pos_mask(MaskSlice& out, const uint32_t* voxels, int x) {

    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD + 1, y + PAD, z + PAD);

            uint32_t here = voxels[idx_here];
            uint32_t next = voxels[idx_next];
            if (here != 0 && next == 0) {
                row |= (1ULL << z);
                out.type[y][z] = here;
            } else {
                out.type[y][z] = 0;
            }
        }
        out.solid[y] = row;
    }
}

// -X face
void build_x_neg_mask(MaskSlice& out, const uint32_t* voxels, int x) {
    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD - 1, y + PAD, z + PAD);
            uint32_t here = voxels[idx_here];
            uint32_t prev = voxels[idx_prev];
            if (here != 0 && prev == 0) {
                row |= (1ULL << z);
                out.type[y][z] = here;
            } else {
                out.type[y][z] = 0;
            }
        }
        out.solid[y] = row;
    }
}

// +Y face
void build_y_pos_mask(MaskSlice& out, const uint32_t* voxels, int y) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD, y + PAD + 1, z + PAD);
            uint32_t here = voxels[idx_here];
            uint32_t next = voxels[idx_next];
            if (here != 0 && next == 0) {
                row |= (1ULL << z);
                out.type[x][z] = here;
            } else {
                out.type[x][z] = 0;
            }
        }
        out.solid[x] = row;
    }
}

// -Y face
void build_y_neg_mask(MaskSlice& out, const uint32_t* voxels, int y) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD, y + PAD - 1, z + PAD);
            uint32_t here = voxels[idx_here];
            uint32_t prev = voxels[idx_prev];
            if (here != 0 && prev == 0) {
                row |= (1ULL << z);
                out.type[x][z] = here;
            } else {
                out.type[x][z] = 0;
            }
        }
        out.solid[x] = row;
    }
}

// +Z face
void build_z_pos_mask(MaskSlice& out, const uint32_t* voxels, int z) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int y = 0; y < CHUNK_SIZE; ++y) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD, y + PAD, z + PAD + 1);
            uint32_t here = voxels[idx_here];
            uint32_t next = voxels[idx_next];
            if (here != 0 && next == 0) {
                row |= (1ULL << y); // shift by y
                out.type[x][y] = here;
            } else {
                out.type[x][y] = 0;
            }
        }
        out.solid[x] = row;
    }
}

// -Z face
void build_z_neg_mask(MaskSlice& out, const uint32_t* voxels, int z) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int y = 0; y < CHUNK_SIZE; ++y) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD, y + PAD, z + PAD - 1);
            uint32_t here = voxels[idx_here];
            uint32_t prev = voxels[idx_prev];
            if (here != 0 && prev == 0) {
                row |= (1ULL << y); // shift by y
                out.type[x][y] = here;
            } else {
                out.type[x][y] = 0;
            }
        }
        out.solid[x] = row;
    }
}

void emit_face_data(
    int axis,
    int sign,
    int depth,
    int y, int z,
    int w, int h,
    uint32_t type,
    std::vector<Vector3>& positions,
    std::vector<Vector3>& normals,
    std::vector<Color>& colors,
    std::vector<int32_t>& indices
) {
    static const int AXIS_MAP[3][3] = {
        {1, 2, 0}, // X axis -> U=Y,V=Z,D=X
        {0, 2, 1}, // Y axis -> U=X,V=Z,D=Y
        {0, 1, 2}  // Z axis -> U=X,V=Y,D=Z
    };
    int axis_u = AXIS_MAP[axis][0];
    int axis_v = AXIS_MAP[axis][1];
    int axis_d = AXIS_MAP[axis][2];

    Vector3 base(0,0,0);
    base[axis_d] = depth + (sign > 0 ? 1.0f : 0.0f);
    base[axis_u] = y;
    base[axis_v] = z;

    Vector3 du(0,0,0); du[axis_u] = h;
    Vector3 dv(0,0,0); dv[axis_v] = w;

    int vi = positions.size();
    positions.push_back(base);          // 0
    positions.push_back(base + dv);     // 1
    positions.push_back(base + du);     // 2
    positions.push_back(base + dv + du);// 3

    float r;
    float g;
    float b;

    decode_rgb888(type, r, g, b);

    Color mat_color(r, g, b);
    for (int i = 0; i < 4; ++i) colors.push_back(mat_color);

    Vector3 normal(0,0,0); 
    normal[axis_d] = static_cast<float>(sign);
    for (int i = 0; i < 4; ++i) normals.push_back(normal);

    std::array<int32_t, 6> face_indices;
    if (axis == 1) {
        // Y axis is "special"
        if (sign > 0) {
            face_indices = {vi + 0, vi + 2, vi + 1, vi + 2, vi + 3, vi + 1};
        } else {
            face_indices = {vi + 0, vi + 1, vi + 2, vi + 2, vi + 1, vi + 3};
        }
    } else {
        // X or Z axis
        if (sign > 0) {
            face_indices = {vi + 0, vi + 1, vi + 2, vi + 2, vi + 1, vi + 3};
        } else {
            face_indices = {vi + 0, vi + 2, vi + 1, vi + 2, vi + 3, vi + 1};
        }
    }
    indices.insert(indices.end(), face_indices.begin(), face_indices.end());
}

Array get_mesh_array(const uint32_t* voxels, int num_threads) {
    struct PartialData {
        std::vector<Vector3> positions;
        std::vector<Color> colors;
        std::vector<int32_t> indices;
        std::vector<Vector3> normals;
    };

    auto build_axis = [&](int axis, int sign, int start_depth, int end_depth) {
        PartialData data;
        MaskSlice mask;

        for (int d = start_depth; d < end_depth; ++d) {
            switch (axis) {
                case 0: sign > 0 ? build_x_pos_mask(mask, voxels, d) : build_x_neg_mask(mask, voxels, d); break;
                case 1: sign > 0 ? build_y_pos_mask(mask, voxels, d) : build_y_neg_mask(mask, voxels, d); break;
                case 2: sign > 0 ? build_z_pos_mask(mask, voxels, d) : build_z_neg_mask(mask, voxels, d); break;
            }

            greedy_merge(mask, axis, d, [&](int axis_, int depth, int a, int b, int w, int h, uint32_t t) {
                emit_face_data(axis_, sign, depth, a, b, w, h, t,
                               data.positions, data.normals, data.colors, data.indices);
            });
        }

        return data;
    };

    std::vector<std::thread> threads;
    std::mutex results_mutex;
    std::vector<PartialData> all_results;

    auto worker = [&](int axis, int sign) {
        int slices_per_thread = (CHUNK_SIZE + num_threads - 1) / num_threads;
        std::vector<PartialData> local_results;
        local_results.reserve(num_threads);

        for (int thread_index = 0; thread_index < num_threads; ++thread_index) {
            int start = thread_index * slices_per_thread;
            int end = std::min(CHUNK_SIZE, start + slices_per_thread);
            if (start >= CHUNK_SIZE) break;

            local_results.push_back(build_axis(axis, sign, start, end));
        }

        std::lock_guard<std::mutex> lock(results_mutex);
        all_results.insert(all_results.end(),
                           std::make_move_iterator(local_results.begin()),
                           std::make_move_iterator(local_results.end()));
    };

    // Spawn one worker per face direction
    threads.emplace_back([&]() { worker(0, +1); }); // +X
    threads.emplace_back([&]() { worker(0, -1); }); // -X
    threads.emplace_back([&]() { worker(1, +1); }); // +Y
    threads.emplace_back([&]() { worker(1, -1); }); // -Y
    threads.emplace_back([&]() { worker(2, +1); }); // +Z
    threads.emplace_back([&]() { worker(2, -1); }); // -Z

    for (auto& t : threads) {
        t.join();
    }

    // Merge all partials
    std::vector<Vector3> positions;
    std::vector<Color> colors;
    std::vector<int32_t> indices;
    std::vector<Vector3> normals;

    size_t total_positions = 0;
    size_t total_indices = 0;

    for (const auto& pd : all_results) {
        total_positions += pd.positions.size();
        total_indices += pd.indices.size();
    }

    positions.reserve(total_positions);
    colors.reserve(total_positions);
    normals.reserve(total_positions);
    indices.reserve(total_indices);

    int vertex_offset = 0;
    for (auto& pd : all_results) {
        positions.insert(positions.end(), pd.positions.begin(), pd.positions.end());
        colors.insert(colors.end(), pd.colors.begin(), pd.colors.end());
        normals.insert(normals.end(), pd.normals.begin(), pd.normals.end());

        for (int i : pd.indices)
            indices.push_back(i + vertex_offset);

        vertex_offset += pd.positions.size();
    }

    // === 🧠 Direct memory copy to PackedArrays ===
    Array arrays;
    arrays.resize(Mesh::ARRAY_MAX);

    // Vertex positions
    PackedVector3Array gpos;
    gpos.resize(positions.size());
    if (!positions.empty()) {
        memcpy(gpos.ptrw(), positions.data(), positions.size() * sizeof(Vector3));
    }

    // Vertex colors
    PackedColorArray gcol;
    gcol.resize(colors.size());
    if (!colors.empty()) {
        memcpy(gcol.ptrw(), colors.data(), colors.size() * sizeof(Color));
    }

    // Vertex indices
    PackedInt32Array gidx;
    gidx.resize(indices.size());
    if (!indices.empty()) {
        memcpy(gidx.ptrw(), indices.data(), indices.size() * sizeof(int32_t));
    }

    // Normals
    PackedVector3Array gnorm;
    gnorm.resize(normals.size());
    if (!normals.empty()) {
        memcpy(gnorm.ptrw(), normals.data(), normals.size() * sizeof(Vector3));
    }

    arrays[Mesh::ARRAY_VERTEX] = gpos;
    arrays[Mesh::ARRAY_COLOR] = gcol;
    arrays[Mesh::ARRAY_INDEX] = gidx;
    arrays[Mesh::ARRAY_NORMAL] = gnorm;

    return arrays;
}

Ref<ArrayMesh> GDExample::mesh_chunk(const uint32_t* voxels, int num_threads) {
    struct PartialData {
        std::vector<Vector3> positions;
        std::vector<Color> colors;
        std::vector<int32_t> indices;
        std::vector<Vector3> normals;
    };
    auto build_axis = [&](int axis, int sign, int start_depth, int end_depth) {
        PartialData data;
        MaskSlice mask;
        for (int d = start_depth; d < end_depth; ++d) {
            switch (axis) {
                case 0: sign > 0 ? build_x_pos_mask(mask, voxels, d) : build_x_neg_mask(mask, voxels, d); break;
                case 1: sign > 0 ? build_y_pos_mask(mask, voxels, d) : build_y_neg_mask(mask, voxels, d); break;
                case 2: sign > 0 ? build_z_pos_mask(mask, voxels, d) : build_z_neg_mask(mask, voxels, d); break;
            }
            greedy_merge(mask, axis, d, [&](int axis_, int depth, int a, int b, int w, int h, uint32_t t) {
                emit_face_data(axis_, sign, depth, a, b, w, h, t,
                                data.positions, data.normals, data.colors, data.indices);
            });
        }
        return data;
    };
    
    std::vector<std::thread> threads;
    std::mutex results_mutex;
    std::vector<PartialData> all_results;

    auto worker = [&](int axis, int sign) {
        int slices_per_thread = (CHUNK_SIZE + num_threads - 1) / num_threads;
        std::vector<PartialData> local_results;
        local_results.reserve(num_threads); // reserve slots per thread

        for (int thread_index = 0; thread_index < num_threads; ++thread_index) {
            int start = thread_index * slices_per_thread;
            int end = std::min(CHUNK_SIZE, start + slices_per_thread);
            if (start >= CHUNK_SIZE) break;
            
            local_results.push_back(build_axis(axis, sign, start, end));
        }

        std::lock_guard<std::mutex> lock(results_mutex);
        all_results.insert(all_results.end(),
                           std::make_move_iterator(local_results.begin()),
                           std::make_move_iterator(local_results.end()));
    };

    // Spawn one “worker” per face-direction
    threads.emplace_back([&]() { worker(0, +1); }); // +X
    threads.emplace_back([&]() { worker(0, -1); }); // -X
    threads.emplace_back([&]() { worker(1, +1); }); // +Y
    threads.emplace_back([&]() { worker(1, -1); }); // -Y
    threads.emplace_back([&]() { worker(2, +1); }); // +Z
    threads.emplace_back([&]() { worker(2, -1); }); // -Z

    for (auto &t : threads) {
        t.join();
    }

    // Merge all partials
    std::vector<Vector3> positions;
    std::vector<Color> colors;
    std::vector<int32_t> indices;
    std::vector<Vector3> normals;

    int vertex_offset = 0;
    for (auto &pd : all_results) {
        for (auto &p : pd.positions) positions.push_back(p);
        for (auto &c : pd.colors) colors.push_back(c);
        for (auto &n : pd.normals) normals.push_back(n);
        for (int i : pd.indices) indices.push_back(i + vertex_offset);
        vertex_offset += pd.positions.size();
    }

    // Pack into Godot arrays using direct memory copy
    Array arrays;
    arrays.resize(Mesh::ARRAY_MAX);

    // Fill positions
    PackedVector3Array gpos;
    gpos.resize(positions.size());
    memcpy(gpos.ptrw(), positions.data(), positions.size() * sizeof(Vector3));
    arrays[Mesh::ARRAY_VERTEX] = gpos;

    // Fill colors
    PackedColorArray gcol;
    gcol.resize(colors.size());
    memcpy(gcol.ptrw(), colors.data(), colors.size() * sizeof(Color));
    arrays[Mesh::ARRAY_COLOR] = gcol;

    // Fill indices
    PackedInt32Array gidx;
    gidx.resize(indices.size());
    memcpy(gidx.ptrw(), indices.data(), indices.size() * sizeof(int32_t));
    arrays[Mesh::ARRAY_INDEX] = gidx;

    // Fill normals
    PackedVector3Array gnorm;
    gnorm.resize(normals.size());
    memcpy(gnorm.ptrw(), normals.data(), normals.size() * sizeof(Vector3));
    arrays[Mesh::ARRAY_NORMAL] = gnorm;

    Ref<ArrayMesh> mesh = memnew(ArrayMesh);
    if (gpos.size() > 0) {
        mesh->add_surface_from_arrays(Mesh::PRIMITIVE_TRIANGLES, arrays);
    }
    return mesh;
}

int cutoff_calc(Vector3 pos, int seed) {
    float distance;
    int primary_biome;
    int closest_biome;
    auto& cutoff_map = get_cutoff_function_map();
    voronoi_data(pos, seed, primary_biome, closest_biome, distance);
    int p_cutoff = cutoff_map.at(keys[primary_biome % biome_count])(Vector2(pos.x, pos.z) * 256.0, seed);
    
    int s_cutoff = cutoff_map.at(keys[closest_biome % biome_count])(Vector2(pos.x, pos.z) * 256.0, seed);
    if (distance > 0.7) {
        return sigmoid_lerp(p_cutoff, s_cutoff, (distance - 0.7) * 1.6666);
    }

    return p_cutoff;
}

void generate(uint32_t* data, Vector3 pos, int threads) {
    constexpr int SIZE = 66;

    std::vector<std::thread> workers;
    auto worker = [SIZE](uint32_t* data, int z_start, int z_end, Vector3 pos) {
        auto& biome_map = get_biome_function_map();

        for (int k = z_start; k < z_end; k++) {
            for (int i = 0; i < SIZE; i++) {
                int biome = biome_id_from_voronoi(((pos + Vector3(k, 0, i)) * 1.0f / 1024.0f), 5) % biome_count;
                int cutoff = cutoff_calc((pos + Vector3(k, 0, i)) * 1.0f / 1024.0f, 5);

                for (int j = 0; j < ((cutoff > 0 && cutoff < 64) ? cutoff : 1); j++) {
                    int index = i + j * SIZE + k * SIZE * SIZE;
                    data[index] = biome_map.at(keys[biome])(pos + Vector3(k, j, i), 5);
                }
            }
        }
    };

    int slice = SIZE / threads;

    for (int t = 0; t < threads; t++) {
        int z_start = t * slice;
        int z_end = (t == threads - 1) ? SIZE : z_start + slice;
        workers.emplace_back(worker, data, z_start, z_end, pos);
    }

    for (auto& t : workers) {
        t.join();
    }
}


Ref<ArrayMesh> GDExample::generate_and_mesh(Vector3 pos) {
    
	auto start = std::chrono::high_resolution_clock::now();
    //std::vector<uint32_t> data(66*66*66);
    //uint32_t* data = generate(pos, 1);
    PackedInt32Array v;
    v.resize(66 * 66 * 66);
    generate((uint32_t*)v.ptrw(), pos, 1);
    
    
    Array arr = get_mesh_array((uint32_t*)v.ptrw(), 1);
    Ref<ArrayMesh> mesh = memnew(ArrayMesh);
    
    
    if (((PackedVector3Array)arr[Mesh::ARRAY_VERTEX]).size() > 0) {
        mesh->add_surface_from_arrays(Mesh::PRIMITIVE_TRIANGLES, arr);
    }
    
    
    auto elapsed = std::chrono::high_resolution_clock::now() - start;
    print_line_rich(vformat("[color=dimgray]%.2fms[/color]",std::chrono::duration<double, std::milli>(elapsed).count()));
    return mesh;
	//return mesh_chunk(data, 4);
}

void GDExample::process_chunk(MeshInstance3D& m, uint32_t* data = nullptr) {

    
    //get mesh and data(nullable)
    //if data == nullptr, generate
    //unroll vertices for collision (for now, maybe abuse greedy for faster array building)
    //generate arraymesh, assign
    //build collision structures, deferred add child

    
    //unroll the vertex stuff with a loop after the fact for now, I need the game working
    std::vector<uint32_t> v_data(data, data + 66 * 66 * 66); //seriously?? ok...
    //start AO investigation
    //generate(v.ptr(), pos)

    if (data == nullptr) {
        Vector3 pos = m.get_position();
        Vector3 chunkCoord = Vector3(
            floor(pos.x / 64.0f),
            floor(pos.y / 64.0f),
            floor(pos.z / 64.0f)
        );
        m.set_mesh(generate_and_mesh(chunkCoord));
    }

}

void GDExample::_process(double delta) {

}

