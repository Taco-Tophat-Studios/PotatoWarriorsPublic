using Godot;
using System;

public partial class Scorecard : AnimatedSprite2D
{
    float time;
    [Export]
    Label timeLabel;
    [Export]
    Label healthLabel;

    FendOffMatch f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        f = (FendOffMatch)GetTree().Root.GetNode("World");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        time += (float)GetProcessDeltaTime();
        timeLabel.Text = (time - (time % 60)) / 60 + ":";
        if (time % 60 < 10)
        {
            timeLabel.Text += "0";
        }
        timeLabel.Text += "" + Mathf.Floor(time % 60);

        healthLabel.Text = "" + f.potato.health;
    }
}
