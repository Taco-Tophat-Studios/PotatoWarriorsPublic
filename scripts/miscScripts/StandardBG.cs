using Godot;
using System;

public partial class StandardBG : ParallaxLayer
{
	private Vector2 scrollVel = new Vector2(192, 72);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		MotionOffset += scrollVel * (float)GetProcessDeltaTime();
	}
}
