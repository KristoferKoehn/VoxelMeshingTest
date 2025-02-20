shader_type spatial;
render_mode unshaded; 

uniform sampler2D _Symbol : source_color, filter_linear;

void vertex() {
	
	 mat4 billboard = VIEW_MATRIX * mat4(
		INV_VIEW_MATRIX[0],
		INV_VIEW_MATRIX[1],
		INV_VIEW_MATRIX[2],
		MODEL_MATRIX[3]
	);
	MODELVIEW_MATRIX = billboard;
}

void fragment() {
	vec4 symbol = texture(_Symbol, UV);
	ALBEDO = symbol.rgb;
	ALPHA = symbol.a;
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
