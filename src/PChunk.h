#ifndef PCHUNK_H
#define PCHUNK_H

#include <godot_cpp/classes/mesh_instance3d.hpp>

namespace godot {

	class PChunk : public MeshInstance3D {
		GDCLASS(PChunk, MeshInstance3D)

		private:
			ArrayMesh* arrayMesh = nullptr;
			PackedVector3Array* CollisionMesh;
		protected:
			static void _bind_methods();
			void PChunk::MeshAssignment(Array ArrayList);
		public:
			PackedVector3Array PChunk::GetCollisionMesh();
			PChunk();
			~PChunk();
			void _process(double delta) override;
			void PChunk::set_bytes(PackedByteArray face_bytes, bool GenerateCollision);
			void PChunk::set_bytes2(PackedByteArray face_bytes, bool GenerateCollision);
	};

}

#endif