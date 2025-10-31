using Godot;

public partial class buttonBaseClass : Button
{
	[Export]
	public string scene;
	protected void Transfer()
	{
		GetTree().CallDeferred("change_scene_to_file", scene);
	}
	private void _on_button_down()
	{
		Transfer();
	}
	private void _on_button_up()
	{
		Transfer();
	}
}
