using Godot;
using System;

public partial class VentFanBlowAreaManager : Area2D
{
	private Vector2 accelerationFactor = new(0, -10000f); //negative because positive Y is down
	[Export]
	match m;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void _on_body_entered(Node2D body) {
		if (body is playerBase player)
        {
			player.effectAcceleration += accelerationFactor;
        }
	}

	private void _on_body_exited(Node2D body) {
		if (body is playerBase player)
        {
			player.effectAcceleration -= accelerationFactor;
        }
	}
}
