shader_type spatial;
render_mode unshaded, blend_add;

uniform sampler2D _Matcap : source_color, filter_linear;

uniform vec3 color_a : source_color;
uniform vec3 color_b : source_color;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 normal_vs = -NORMAL;
	normal_vs *= 0.5;
	normal_vs += 0.5;
	
	vec3 matcap = texture(_Matcap, normal_vs.xy).rgb;
	
	vec3 color = mix(color_a, color_b, sin(TIME * 2.0) * 0.5 + 0.5);
	matcap += color;
	
	ALBEDO = matcap;
	ALPHA = matcap.r;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
