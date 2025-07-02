#ifndef GDEXAMPLE_H
#define GDEXAMPLE_H

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/array_mesh.hpp>

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
	Ref<ArrayMesh> mesh_chunk(const uint32_t *voxels, int num_threads);
	Ref<ArrayMesh> generate_and_mesh(Vector3 pos);
	void _process(double delta) override;
	void _ready() override;
};

}

#endif