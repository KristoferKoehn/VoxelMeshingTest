shader_type spatial;

uniform sampler2D starry : filter_nearest;
uniform sampler2D banana : filter_nearest;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 dist = vec2((INV_VIEW_MATRIX * vec4(NODE_POSITION_VIEW, 0)).z, -(INV_VIEW_MATRIX * vec4(NODE_POSITION_VIEW, 0)).z);
	vec2 scaleA = dist * 1.2;
	vec2 scaleB = dist * 1.7;

	ALBEDO = texture(starry, UV - scaleA * (INV_VIEW_MATRIX * vec4(NODE_POSITION_VIEW, 0)).xy).rgb;
	ALBEDO += texture(banana, UV - scaleB * (INV_VIEW_MATRIX * vec4(NODE_POSITION_VIEW, 0)).xy + vec2(0.4,0.4)).rgb;
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
