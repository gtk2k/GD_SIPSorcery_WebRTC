shader_type spatial;
uniform sampler2D _tex;

void fragment() {
	vec4 col = texture(_tex, UV);
	col.rgb = pow(col.rgb,vec3(2.2));
	ALBEDO = vec3(col.b, col.g, col.r);
}
