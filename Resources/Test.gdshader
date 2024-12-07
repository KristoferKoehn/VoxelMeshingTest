shader_type spatial;

uniform sampler2D AlbedoTexture : filter_nearest;
uniform sampler2D MetallicTexture : filter_nearest;
uniform sampler2D TransparencyTexture : filter_nearest;
uniform sampler2D EmissivenessTexture : filter_nearest;

const vec2 TextureCellSize = vec2(1.0/32.0, 1.0/32.0);
uniform float TextureAtlasSize;

varying vec2 AlbedoPos;
varying vec2 MetallicPos;
varying vec2 EmissivenessPos;
varying vec2 TransparencyPos;

varying float Metallicity;
varying float Emissiveness;
varying float Transparency;

void vertex() {
	AlbedoPos = vec2(CUSTOM0.x / TextureAtlasSize, mod(CUSTOM0.x, TextureAtlasSize));
	MetallicPos = vec2(CUSTOM0.y / TextureAtlasSize, mod(CUSTOM0.y, TextureAtlasSize));
	EmissivenessPos = vec2(CUSTOM0.z / TextureAtlasSize, mod(CUSTOM0.z, TextureAtlasSize));
	TransparencyPos = vec2(CUSTOM0.w / TextureAtlasSize, mod(CUSTOM0.w, TextureAtlasSize));
	
	Metallicity = CUSTOM0.y;
	Emissiveness = CUSTOM0.z;
	Transparency = CUSTOM0.w;
}

void fragment() {
	ALBEDO = COLOR.rgb;
	if (AlbedoPos != vec2(0,0)) {
			ALBEDO = ALBEDO * texture(AlbedoTexture, AlbedoPos + UV * TextureCellSize).rgb;
	}
	
	if (MetallicPos != vec2(0,0)) {
		if (Metallicity < 1023.0) {
			ROUGHNESS = METALLIC = texture(MetallicTexture, MetallicPos + UV * TextureCellSize).r;
			ROUGHNESS = 1.0 - ROUGHNESS;
		} else {
			ROUGHNESS = 1.0 - (Metallicity - 1023.0);
			METALLIC = Metallicity - 1023.0;
		}
	}

	if (EmissivenessPos != vec2(0,0)) {
		if (Emissiveness < 1023.0) {
			EMISSION = ALBEDO * texture(EmissivenessTexture, EmissivenessPos * TextureCellSize).r;
		} else {
			EMISSION = ALBEDO * (Emissiveness - 1023.0);
		}
	}
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
