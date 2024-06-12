using Godot;
using System;

public partial class simple_player : CharacterBody2D
{
	//this script exists for no other purpose than for a player that does nothing but moves (literally me), for interactive sequences and stuff.

	//I think everything has to be doubled because the player is normally scaled by x2 or something idk I'm not a 
	public const float Speed = 12f;

    public override void _Ready()
    {
        Velocity = new Vector2(40, -20);
    }


    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;


		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity += direction * Speed;
		}
		

		Velocity = velocity;
		MoveAndSlide();
	}
}
