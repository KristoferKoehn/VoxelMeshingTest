shader_type spatial;

uniform sampler2D WaterNoise;

void vertex() {
	/*
	
	if (COLOR.b == 1.0f) {
		COLOR = vec4(0.3, 0.3, 0.9, 0.4);
		VERTEX.y += texture(WaterNoise, UV / 10.0f + TIME/10.0f).r;
	} else {
		
	}
	*/
}

void fragment() {

	
	
	ALBEDO = COLOR.rgb;
	//ALPHA = COLOR.a;
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
