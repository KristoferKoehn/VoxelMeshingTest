#ifndef PGENERATOR_H
#define PGENERATOR_H

#include <godot_cpp/classes/fast_noise_lite.hpp>

namespace godot {

	class PGenerator : public FastNoiseLite {
		GDCLASS(PGenerator, FastNoiseLite)

		private:
            FastNoiseLite* cutoff;
            float cutoffElevation;
		protected:
			static void _bind_methods();

		public:


			PGenerator();
			~PGenerator();
	};

}

#endif