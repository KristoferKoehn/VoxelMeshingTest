shader_type spatial;
render_mode cull_disabled, shadows_disabled;

varying flat int ID;

void vertex() {
	
}

void fragment() {
	ALBEDO = vec3(UV, 0);
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
