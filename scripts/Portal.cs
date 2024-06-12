using Godot;
using System;

public partial class Portal : Node2D
{
	[Export]
	public string destination = "";
	[Export]
	public Node2D portalExit;
	match m;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		m = m = (match)this.GetParent();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void _on_portal_area_body_entered(Node2D body)
	{
		foreach (playerBase p in m.players) {
			if (body == (Node2D)p) {
				if (destination.Contains("res://")) {
					GetTree().CallDeferred("change_scene_to_file", destination); //bruh
				} else {
					string[] coords = destination.Split('|');
					p.GlobalPosition = portalExit.GlobalPosition;
					//maybe do something with p.pastPosition? 
				}
			}
		}
	}
}



