#include <thread>
#include "gdexample.h"
#include "noise_functions.hpp"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/packed_vector3_array.hpp>
#include <godot_cpp/variant/packed_color_array.hpp>
#include <godot_cpp/variant/packed_int32_array.hpp>
#include <vector>
#include <array>
#include <cstdint>
#include <functional>
#include <chrono>

using namespace godot;

std::vector<uint8_t> GDExample::arr(66 * 66 * 66);

/*
todos

get normals working

investigate binary bs, it's gotta be almost done-ish?

think on how to do collision. might be spicy?
do uber-mask greedy pass?

*/

void GDExample::_bind_methods()
{
	//Ref<ArrayMesh> GDExample::generate_and_mesh(Vector3 pos) {
	ClassDB::bind_method(D_METHOD("generate_and_mesh", "pos"), &GDExample::generate_and_mesh);
}

GDExample::GDExample() {
	print_error("SUCCESSFULLY RELOADED GDEXTENSION 5");
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
    uint8_t  type[CHUNK_SIZE][CHUNK_SIZE]; // per voxel type
};

// Greedy meshing with typed bitmask mask
void greedy_merge(const MaskSlice& slice, int axis, int depth, const std::function<void(int, int, int, int, int, int, uint8_t)>& emit_func) {
    bool visited[CHUNK_SIZE][CHUNK_SIZE] = {};

    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = slice.solid[y];
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            if (!(row & (1ULL << z)) || visited[y][z]) continue;
            uint8_t t = slice.type[y][z];

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
void build_x_pos_mask(MaskSlice& out, const uint8_t* voxels, int x) {

    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD + 1, y + PAD, z + PAD);

            uint8_t here = voxels[idx_here];
            uint8_t next = voxels[idx_next];
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
void build_x_neg_mask(MaskSlice& out, const uint8_t* voxels, int x) {
    for (int y = 0; y < CHUNK_SIZE; ++y) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD - 1, y + PAD, z + PAD);
            uint8_t here = voxels[idx_here];
            uint8_t prev = voxels[idx_prev];
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
void build_y_pos_mask(MaskSlice& out, const uint8_t* voxels, int y) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD, y + PAD + 1, z + PAD);
            uint8_t here = voxels[idx_here];
            uint8_t next = voxels[idx_next];
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
void build_y_neg_mask(MaskSlice& out, const uint8_t* voxels, int y) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int z = 0; z < CHUNK_SIZE; ++z) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD, y + PAD - 1, z + PAD);
            uint8_t here = voxels[idx_here];
            uint8_t prev = voxels[idx_prev];
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
void build_z_pos_mask(MaskSlice& out, const uint8_t* voxels, int z) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int y = 0; y < CHUNK_SIZE; ++y) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_next = index_3d(x + PAD, y + PAD, z + PAD + 1);
            uint8_t here = voxels[idx_here];
            uint8_t next = voxels[idx_next];
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
void build_z_neg_mask(MaskSlice& out, const uint8_t* voxels, int z) {
    for (int x = 0; x < CHUNK_SIZE; ++x) {
        uint64_t row = 0;
        for (int y = 0; y < CHUNK_SIZE; ++y) {
            int idx_here = index_3d(x + PAD, y + PAD, z + PAD);
            int idx_prev = index_3d(x + PAD, y + PAD, z + PAD - 1);
            uint8_t here = voxels[idx_here];
            uint8_t prev = voxels[idx_prev];
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
    uint8_t type,
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

    Color mat_color(depth/66.0f, y/66.0f, z/66.0f); 
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

Ref<ArrayMesh> GDExample::mesh_chunk(const uint8_t* voxels, int num_threads) {
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
            greedy_merge(mask, axis, d, [&](int axis_, int depth, int a, int b, int w, int h, uint8_t t) {
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


Ref<ArrayMesh> GDExample::generate_and_mesh(Vector3 pos) {
	constexpr int SIZE = 66;
	constexpr int THREADS = 8;
	constexpr int TOTAL = SIZE * SIZE * SIZE;

	auto start = std::chrono::high_resolution_clock::now();

	std::vector<std::thread> workers;

	auto worker = [SIZE](uint8_t* data, int z_start, int z_end, Vector3 pos) {
		for (int k = z_start; k < z_end; k++) {
			for (int j = 0; j < SIZE; j++) {
				for (int i = 0; i < SIZE; i++) {
                    int index = i + j * SIZE + k * SIZE * SIZE;
                    float f = fbm_3d((i + pos.z) * 1.0f/16.0f, (j + pos.y) * 1.0f/16.0f, (k + pos.x) * 1.0f/16.0f, 1, 0, 0.01) * 2.0;
                    //int g = abs(k % 3 - j % 4 - i % 5);
                    data[index] = f > 0.5 ? 1 : 0;
                    //data[index] = g > 0 ? 1 : 0;
				}
			}
		}
	};

	int slice = SIZE / THREADS;

	for (int t = 0; t < THREADS; t++) {
		int z_start = t * slice;
		int z_end = (t == THREADS - 1) ? SIZE : z_start + slice;
		workers.emplace_back(worker, arr.data(), z_start, z_end, pos);
	}

	for (auto& t : workers) {
		t.join();
	}

    auto elapsed = std::chrono::high_resolution_clock::now() - start;
    //print_error(std::chrono::duration<double, std::milli>(elapsed).count());
	return mesh_chunk(arr.data(), THREADS);
}

void GDExample::_process(double delta) {

}


