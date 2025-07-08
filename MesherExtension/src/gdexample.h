#ifndef GDEXAMPLE_H
#define GDEXAMPLE_H

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/mesh_instance3d.hpp>

namespace godot {

class GDExample : public Node {
	GDCLASS(GDExample, Node)

private:
	double time_passed;
	static std::vector<uint32_t> arr;


protected:
	static void _bind_methods();

public:
	GDExample();
	~GDExample();
	//Ref<ArrayMesh> mesh_chunk(const int32_t *voxels, int num_threads);
	Array generate_mesh_array(const int32_t *voxels, int num_threads);
	PackedInt32Array process_chunk(MeshInstance3D* m, const Variant &data, bool generate);
	void _process(double delta) override;
	void _ready() override;
};

}

#endif