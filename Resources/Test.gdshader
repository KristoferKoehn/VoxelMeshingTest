shader_type spatial;

uniform sampler2D WaterNoise : filter_nearest;

void vertex() {

}

void fragment() {
	ALBEDO = COLOR.rgb;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
