shader_type spatial;

uniform sampler2D AlbedoTexture : filter_nearest;
uniform sampler2D MetallicTexture : filter_nearest;
uniform sampler2D TransparencyTexture : filter_nearest;
uniform sampler2D EmissivenessTexture : filter_nearest;

uniform vec2 TextureCellSize;
uniform float TextureAtlasSize;

varying vec2 CellPos;

varying vec2 AlbedoUV;
varying vec2 MetallicUV;
varying vec2 TransparencyUV;
varying vec2 EmissivenessUV;

void vertex() {
	float row = floor(float(CUSTOM0.x) / TextureAtlasSize);
	float column = mod(float(CUSTOM0.x), TextureAtlasSize);
	
	CellPos = vec2(row * TextureAtlasSize,column * TextureAtlasSize);
	//AlbedoUV = vec2(row, column);
	AlbedoUV = CellPos * vec2(1.0/32.0, 1.0/32.0);
	MetallicUV = vec2(float(int(CUSTOM0.y) % int(TextureAtlasSize)), float(int(CUSTOM0.y) / int(TextureAtlasSize)));
	TransparencyUV = vec2(float(int(CUSTOM0.z) % int(TextureAtlasSize)), float(int(CUSTOM0.z) / int(TextureAtlasSize)));
	EmissivenessUV = vec2(float(int(CUSTOM0.w) % int(TextureAtlasSize)), float(int(CUSTOM0.w) / int(TextureAtlasSize)));
}

void fragment() {
	ALBEDO = COLOR.rgb;
	ALBEDO = ALBEDO * texture(AlbedoTexture, CellPos + UV * AlbedoUV).rgb;
	//METALLIC = texture(MetallicTexture, UV * MetallicUV).r;
	//EMISSION = ALBEDO * texture(EmissivenessTexture, UV * EmissivenessUV).r;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
