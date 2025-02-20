shader_type spatial;
render_mode unshaded, cull_front;

uniform sampler2D _Matcap : source_color, filter_linear;

uniform float x_stretch = 10.0;
uniform float y_stretch = 10.0;

float hash(vec2 uv) {
	return fract(sin(dot(uv, vec2(426.1, 311.7))) * 46758.545373);
}


float circle (vec2 uv) {
	return length(uv - vec2(0.5));
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 uv_c = fract(vec2(UV.x * x_stretch, UV.y * y_stretch));
	float c = circle(uv_c);
	c = 1.0 - smoothstep(c + 0.05, c - 0.05, 0.4);
	
	vec2 f = floor(UV * 5.0);
	float h01 = hash(f) * c;
	float h02 = hash(f + 0.456) * c;
	float gray = mix(h01, h02, sin(TIME * PI) * 0.5 + 0.5);
	gray *= 0.1;
	
	vec3 vertex_vs = normalize(VERTEX);
	vec3 normal_vs = NORMAL;
	
	vec3 coord = cross(vertex_vs, normal_vs);
	vec2 uv_vs = vec2(coord.x, coord.y);
	uv_vs *= 0.5;
	uv_vs += 0.5;
	uv_vs += gray;
	
	vec3 matcap = texture(_Matcap, uv_vs).rgb;
	
	ALBEDO = matcap;
	ALPHA = 0.6;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
