shader_type spatial;
render_mode blend_add;


void vertex() {
	// Called for every vertex the material is visible on.
}

vec3 fresnel_glow(float amount, float intensity, vec3 color, vec3 normal, vec3 view)
{
	return pow((1.0 - dot(normalize(normal), normalize(view))), amount) * color * intensity;
}

void fragment()
{
	vec3 base_color = VIEW * NORMAL * 2.1;
	//vec3 base_color = vec3(1);
	vec3 fresnel_color = vec3(0.0, 0.7, 0.9);
	vec3 fresnel = fresnel_glow(4.0, 4.5, fresnel_color, NORMAL, VIEW);
	ALBEDO = base_color + fresnel;
	ALPHA = clamp(length(fresnel), 0, 1.0);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
