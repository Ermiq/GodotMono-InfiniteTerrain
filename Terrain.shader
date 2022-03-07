shader_type spatial;

uniform float metallic : hint_range(0.0,1.0) = 0.1;
uniform float roughness : hint_range(0,1) = 1.0;

uniform float slope_factor = 2;

uniform sampler2D tex_flat : hint_albedo;
uniform sampler2D tex_slope : hint_albedo;
uniform float tex_scale = 10.0;

float get_slope_of_terrain(float height_normal) {
	float slope = 1.0 - height_normal;
	return slope * slope_factor;
}

mat3 get_invcam_matrix3(mat4 inv_cam_matrix) {
	vec4 invcamx = inv_cam_matrix[0];
	vec4 invcamy = inv_cam_matrix[1];
	vec4 invcamz = inv_cam_matrix[2];
	vec4 invcamw = inv_cam_matrix[3];

	mat3 invcam = mat3(invcamx.xyz, invcamy.xyz, invcamz.xyz);
	return invcam;
}

void vertex() {
	//UV *= tex_scale;
}

void fragment() {
	vec3 slope_clr = vec3(texture(tex_slope, UV * tex_scale).rgb);
	vec3 flat_clr = vec3(texture(tex_flat, UV * tex_scale).rgb);
	
	// Getting world_normal:
	mat3 invcam3 = get_invcam_matrix3(INV_CAMERA_MATRIX);
	vec3 world_normal = NORMAL * invcam3;
	
	float steepness = get_slope_of_terrain(world_normal.y);
	
	vec3 clr = mix(flat_clr, slope_clr, steepness);
	//vec3 clr = steepness >= slope_factor ? slope_clr : flat_clr;
	
	ALBEDO = clr;
	METALLIC = metallic;
	ROUGHNESS = roughness;
}