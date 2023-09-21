shader_type spatial;
uniform sampler2D _tex;

void fragment() {
	vec4 col = texture(_tex, UV);
	ALBEDO = col.rgb;
}
