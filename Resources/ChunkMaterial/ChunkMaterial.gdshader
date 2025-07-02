shader_type spatial;

varying vec4 c;

void vertex() {
	c = COLOR;
}

void fragment() {
	ALBEDO = c.rgb;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
