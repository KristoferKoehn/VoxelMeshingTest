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
	ClassDB::bind_method(D_METHOD("set_bytes2"), &PChunk::set_bytes2);
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

	//arrayMesh->add_surface_from_arrays()

	godot::Mesh::ArrayFormat format = (godot::Mesh::ArrayFormat)(godot::Mesh::ArrayFormat::ARRAY_FORMAT_VERTEX |
				godot::Mesh::ArrayFormat::ARRAY_FORMAT_NORMAL |
				godot::Mesh::ArrayFormat::ARRAY_FORMAT_COLOR |
				godot::Mesh::ArrayFormat::ARRAY_FORMAT_INDEX |
				godot::Mesh::ArrayFormat::ARRAY_FORMAT_CUSTOM0 |
				godot::Mesh::ArrayFormat::ARRAY_FORMAT_TEX_UV
				);
	
	format = (godot::Mesh::ArrayFormat)(format | ((int)godot::Mesh::ARRAY_CUSTOM_RGBA_FLOAT << (int)godot::Mesh::ARRAY_FORMAT_CUSTOM0_SHIFT));

	arrayMesh->call_deferred("add_surface_from_arrays", godot::Mesh::PRIMITIVE_TRIANGLES, ArrayList, godot::Array(), godot::Dictionary(), format);
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

		//vertex assignment (48 bytes)
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
		
		//normal assignment (12 bytes)
		Vector3 normal = Vector3(face_bytes.decode_float(faceIndex + 52), face_bytes.decode_float(faceIndex + 56), face_bytes.decode_float(faceIndex + 60));

		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);
		
		//block ID (4 bytes)
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

	
	/*ArrayMesh* am = memnew(ArrayMesh());
	am->add_surface_from_arrays(godot::Mesh::PRIMITIVE_TRIANGLES, arr);
	this->set_mesh(am);*/
	
}

void PChunk::set_bytes2(PackedByteArray face_bytes, bool GenerateCollision)
{
	Array arr;
	arr.resize(godot::Mesh::ARRAY_MAX);
	PackedVector3Array Vertices;

	PackedVector3Array* UnpackedVertices = memnew(PackedVector3Array());
	PackedVector3Array Normals;
	PackedVector3Array UV;
	PackedColorArray Colors;
	PackedInt32Array Indices;
	PackedFloat32Array Custom0;
	
	//for every workgroup octant (count.size() / word_size)
	//	for each face counted in octant
	//		get face data, append 

	int vertexCount = 0;
	for (int i = 0, faceIndex = 0; i < face_bytes.size()/128; i++, faceIndex += 128) {

		//vertex assignment (48 bytes)
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

		Color e = Color(face_bytes.decode_float(faceIndex + 48), face_bytes.decode_float(faceIndex + 52), face_bytes.decode_float(faceIndex + 56));
		Color f = Color(face_bytes.decode_float(faceIndex + 60), face_bytes.decode_float(faceIndex + 64), face_bytes.decode_float(faceIndex + 68));
		Color g = Color(face_bytes.decode_float(faceIndex + 72), face_bytes.decode_float(faceIndex + 76), face_bytes.decode_float(faceIndex + 80));
		Color h = Color(face_bytes.decode_float(faceIndex + 84), face_bytes.decode_float(faceIndex + 88), face_bytes.decode_float(faceIndex + 92));

		Colors.append(e);
		Colors.append(f);
		Colors.append(g);
		Colors.append(h);

		//append the crafted UVs (always (0,0) to (1,1)) rotate this if it's weird
		UV.append(Vector3(1,1,0));
		UV.append(Vector3(0,1,0));
		UV.append(Vector3(0,0,0));
		UV.append(Vector3(1,0,0));


		/*
		Vector4 k = Vector4(face_bytes.decode_float(faceIndex + 96), face_bytes.decode_float(faceIndex + 100), face_bytes.decode_float(faceIndex + 104),  face_bytes.decode_float(faceIndex + 108));

		Custom0.append(k);
		Custom0.append(k);
		Custom0.append(k);
		Custom0.append(k);
		*/

		PackedFloat32Array fl = {(float)face_bytes.decode_float(faceIndex + 96), 
								 (float)face_bytes.decode_float(faceIndex + 100), 
								 (float)face_bytes.decode_float(faceIndex + 104),  
								 (float)face_bytes.decode_float(faceIndex + 108)};

		Custom0.append_array(fl);
		Custom0.append_array(fl);
		Custom0.append_array(fl);
		Custom0.append_array(fl);

		Vector3 normal = Vector3(face_bytes.decode_float(faceIndex + 112), face_bytes.decode_float(faceIndex + 116), face_bytes.decode_float(faceIndex + 120));

		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);
		Normals.append(normal);

		Indices.append_array({0 + vertexCount, 1 + vertexCount, 2 + vertexCount, 0 + vertexCount, 2 + vertexCount, 3 + vertexCount});
		vertexCount += 4;
	}

	CollisionMesh = UnpackedVertices;

	arr[godot::Mesh::ARRAY_VERTEX] = Vertices;
	arr[godot::Mesh::ARRAY_NORMAL] = Normals;
	arr[godot::Mesh::ARRAY_COLOR] = Colors;
	arr[godot::Mesh::ARRAY_INDEX] = Indices;
	arr[godot::Mesh::ARRAY_CUSTOM0] = Custom0;
	arr[godot::Mesh::ARRAY_TEX_UV] = UV;

	
	MeshAssignment(arr);

	/*ArrayMesh* am = memnew(ArrayMesh());
	am->add_surface_from_arrays(godot::Mesh::PRIMITIVE_TRIANGLES, arr);
	this->set_mesh(am);*/
	
}

