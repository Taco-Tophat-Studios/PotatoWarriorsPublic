shader_type canvas_item;
uniform vec4 o : source_color = vec4(0.0, 0.0, 0.1, 1.0);
uniform vec4 n : source_color = vec4(0.0, 0.0, 0.1, 1.0);

void fragment() {
	vec4 o_p = texture(TEXTURE, UV);
	if (o_p == o )
	{
		COLOR = n;
	}
	else 
	{
		COLOR = o_p;
	}
}