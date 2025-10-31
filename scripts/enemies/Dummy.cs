using Godot;
using System;

public partial class Dummy : enemyBase
{
	[Export]
	public Area2D damageCol;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		health = 1;
		invulnerable = true;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
		EntityPhysicsPackage1((float)delta, null);
    }
}
