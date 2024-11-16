shader_type spatial;

uniform sampler2D AlbedoTexture : filter_nearest;
uniform sampler2D MetallicTexture : filter_nearest;
uniform sampler2D TransparencyTexture : filter_nearest;
uniform sampler2D EmissivenessTexture : filter_nearest;

uniform vec2 TextureCellSize;
uniform float TextureAtlasSize;

varying vec2 AlbedoUV;
varying vec2 MetallicUV;
varying vec2 TransparencyUV;
varying vec2 EmissivenessUV;

void vertex() {
	AlbedoUV = vec2(float(int(CUSTOM0.x) % int(TextureAtlasSize)), float(int(CUSTOM0.x) / int(TextureAtlasSize)));
	MetallicUV = vec2(float(int(CUSTOM0.y) % int(TextureAtlasSize)), float(int(CUSTOM0.y) / int(TextureAtlasSize)));
	TransparencyUV = vec2(float(int(CUSTOM0.z) % int(TextureAtlasSize)), float(int(CUSTOM0.z) / int(TextureAtlasSize)));
	EmissivenessUV = vec2(float(int(CUSTOM0.w) % int(TextureAtlasSize)), float(int(CUSTOM0.w) / int(TextureAtlasSize)));
}

void fragment() {
	ALBEDO = COLOR.rgb;
	ALBEDO = ALBEDO * texture(AlbedoTexture, UV * AlbedoUV).rgb;
	METALLIC = texture(MetallicTexture, UV * MetallicUV).r;
	//EMISSION = ALBEDO * texture(EmissivenessTexture, UV * EmissivenessUV).r;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
