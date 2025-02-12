shader_type spatial;

uniform float xrot;
uniform float yrot;
uniform float zrot;

//rot == (phi, theta, psi)
mat3 ProcessRotation(vec3 rot) {
	float phi = rot.x;
	float theta = rot.y;
	float psi = rot.z;
	mat3 PositionalMatrix;

	PositionalMatrix[0][0] = cos(theta)*cos(psi);
	PositionalMatrix[1][0] = cos(phi)*sin(psi) + sin(phi) * sin(theta) * cos(psi);
	PositionalMatrix[2][0] = sin(phi) * sin(psi) - cos(phi)*sin(theta)*cos(psi);
	PositionalMatrix[0][1] = -cos(theta)*sin(psi);
	PositionalMatrix[1][1] = cos(phi)*cos(psi) - sin(phi) * sin(theta) * sin(psi);
	PositionalMatrix[2][1] = sin(phi) * cos(psi) + cos(phi)*sin(theta)*sin(psi);
	PositionalMatrix[0][2] = sin(theta);
	PositionalMatrix[1][2] = -sin(phi)*cos(theta);
	PositionalMatrix[2][2] = cos(phi)*cos(theta);
	return PositionalMatrix;
}

void vertex() {
	//VERTEX = VERTEX * ProcessRotation(vec3(2.3 * TIME * xrot, 3.1 * TIME * yrot, 1.3 * TIME * zrot)) + vec3(0.4,0.3,0.12) * ProcessRotation(vec3(2.0 * -TIME * xrot, 2.0 * -TIME * yrot,  2.0 * -TIME * zrot));
	//NORMAL = NORMAL * ProcessRotation(vec3(2.3 * TIME * xrot, 3.1 * TIME * yrot, 1.3 * TIME * zrot));
	//VERTEX = VERTEX * ProcessRotation(vec3(xrot, yrot, zrot));
	//NORMAL = NORMAL * ProcessRotation(vec3(xrot, yrot, zrot));s
	// Called for every vertex the material is visible on.
}

void fragment() {
	ALBEDO = COLOR.rgb;
}