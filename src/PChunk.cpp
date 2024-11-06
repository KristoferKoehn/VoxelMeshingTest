#include "PChunk.h"
#include <stdlib.h>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <godot_cpp/classes/array_mesh.hpp>
#include <godot_cpp/classes/static_body3d.hpp>
#include <godot_cpp/classes/collision_shape3d.hpp>
#include <godot_cpp/classes/concave_polygon_shape3d.hpp>

using namespace godot;
using namespace std;

void PChunk::_bind_methods() {
	ClassDB::bind_method(D_METHOD("set_bytes"), &PChunk::set_bytes);
	ClassDB::bind_method(D_METHOD("GetCollisionMesh"), &PChunk::GetCollisionMesh);
}

PChunk::PChunk() {
	if (!Engine::get_singleton()->is_editor_hint()) { 
		set_process_mode(Node::ProcessMode::PROCESS_MODE_DISABLED);
	}
}

PChunk::~PChunk() {
	// Add your cleanup here.
}

void PChunk::_process(double delta) {

}

void PChunk::MeshAssignment(Array ArrayList) {
	if (arrayMesh != nullptr) {
		memdelete(arrayMesh);
	}
	arrayMesh = memnew(ArrayMesh());
	arrayMesh->call_deferred("add_surface_from_arrays", godot::Mesh::PRIMITIVE_TRIANGLES, ArrayList);
	this->set_mesh(arrayMesh);
	return;
}

PackedVector3Array PChunk::GetCollisionMesh() {

	return *CollisionMesh;
	//sb->add_child(cs);
	//cs->set_shape(s);
}

void PChunk::set_bytes(PackedByteArray face_bytes, bool GenerateCollision)
{
	Array arr;
	arr.resize(godot::Mesh::ARRAY_MAX);
	PackedVector3Array Vertices;


	PackedVector3Array* UnpackedVertices = memnew(PackedVector3Array());
	PackedVector3Array Normals;
	PackedColorArray Colors;
	PackedInt32Array Indices;
	
	//for every workgroup octant (count.size() / word_size)
	//	for each face counted in octant
	//		get face data, append 

	int vertexCount = 0;
	for (int i = 0, faceIndex = 0; i < face_bytes.size()/64; i++, faceIndex += 64) {
		Vector3 a = Vector3(face_bytes.decode_float(faceIndex),      face_bytes.decode_float(faceIndex + 4),  face_bytes.decode_float(faceIndex + 8));
		Vector3 b = Vector3(face_bytes.decode_float(faceIndex + 12), face_bytes.decode_float(faceIndex + 16), face_bytes.decode_float(faceIndex + 20));
		Vector3 c = Vector3(face_bytes.decode_float(faceIndex + 24), face_bytes.decode_float(faceIndex + 28), face_bytes.decode_float(faceIndex + 32));
		Vector3 d = Vector3(face_bytes.decode_float(faceIndex + 36), face_bytes.decode_float(faceIndex + 40), face_bytes.decode_float(faceIndex + 44));

		Vertices.append(a);
		Vertices.append(b);
		Vertices.append(c);
		Vertices.append(d);

		UnpackedVertices->append(a);
		UnpackedVertices->append(b);
		UnpackedVertices->append(c);
		UnpackedVertices->append(a);
		UnpackedVertices->append(c);
		UnpackedVertices->append(d);
		
		Vector3 normal = Vector3(face_bytes.decode_float(faceIndex + 52), face_bytes.decode_float(faceIndex + 56), face_bytes.decode_float(faceIndex + 60));

		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);
		
		switch ((int)face_bytes.decode_float(faceIndex + 48))
		{
			case 1:
				Colors.append(Color(8 / 255.0, 147 / 255.0, 0));
				Colors.append(Color(8 / 255.0, 147 / 255.0, 0));
				Colors.append(Color(8 / 255.0, 147 / 255.0, 0));
				Colors.append(Color(8 / 255.0, 147 / 255.0, 0));
				break;
			case 2:
				Colors.append(Color(219 / 255.0, 168 / 255.0, 96 / 255.0));
				Colors.append(Color(219 / 255.0, 168 / 255.0, 96 / 255.0));
				Colors.append(Color(219 / 255.0, 168 / 255.0, 96 / 255.0));
				Colors.append(Color(219 / 255.0, 168 / 255.0, 96 / 255.0));
				break;
			case 3:
				Colors.append(Color(128 / 255.0, 128 / 255.0, 128 / 255.0));
				Colors.append(Color(128 / 255.0, 128 / 255.0, 128 / 255.0));
				Colors.append(Color(128 / 255.0, 128 / 255.0, 128 / 255.0));
				Colors.append(Color(128 / 255.0, 128 / 255.0, 128 / 255.0));
				break;
			case 16:
				Colors.append(Color(0, 0, 255 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				break;
			default:
				Colors.append(Color(0, 0, 255 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				Colors.append(Color(0, 0, 1 / 255.0));
				break;

		}

		Indices.append_array({0 + vertexCount, 1 + vertexCount, 2 + vertexCount, 0 + vertexCount, 2 + vertexCount, 3 + vertexCount});
		vertexCount += 4;
	}


	CollisionMesh = UnpackedVertices;

	arr[godot::Mesh::ARRAY_VERTEX] = Vertices;
	arr[godot::Mesh::ARRAY_NORMAL] = Normals;
	arr[godot::Mesh::ARRAY_COLOR] = Colors;
	arr[godot::Mesh::ARRAY_INDEX] = Indices;

	MeshAssignment(arr);


	/*
	ArrayMesh* am = memnew(ArrayMesh());
	
	am->add_surface_from_arrays(godot::Mesh::PRIMITIVE_TRIANGLES, arr);

	this->set_mesh(am);
	*/
}
