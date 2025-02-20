shader_type spatial;
render_mode cull_disabled, unshaded;

uniform sampler2D _Matcap : source_color, filter_linear;

uniform vec3 color_a : source_color;
uniform vec3 color_b : source_color;


float hash(vec2 uv) {
	return fract(sin(dot(uv, vec2(426.1, 311.7))) * 46758.545373);
}

float circle (vec2 uv) {
	return length(uv - vec2(0.5));
}

void vertex() {
	
	 	// Billboard Mode: Particles
	mat4 mat_world = mat4(
			normalize(INV_VIEW_MATRIX[0]),
			normalize(INV_VIEW_MATRIX[1]),
			normalize(INV_VIEW_MATRIX[2]),
			MODEL_MATRIX[3]);
	mat_world = mat_world * mat4(
			vec4(cos(INSTANCE_CUSTOM.x), -sin(INSTANCE_CUSTOM.x), 0.0, 0.0),
			vec4(sin(INSTANCE_CUSTOM.x), cos(INSTANCE_CUSTOM.x), 0.0, 0.0),
			vec4(0.0, 0.0, 1.0, 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_MATRIX = VIEW_MATRIX * mat_world;

	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);

	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 normal_vs = -NORMAL;
	normal_vs *= 0.5;
	normal_vs += 0.5;
	
	vec3 matcap = texture(_Matcap, normal_vs.xy).rgb;
	
	vec3 color = mix(color_a, color_b, sin(TIME * 2.0) * 0.2 + 0.2);
	matcap += color;
	
	ALBEDO = matcap;
	ALPHA = matcap.r;
	
	if (UV.x - UV.y > 0.00) {
		ALPHA = 0.0;
	}
	
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
