using Godot;
using System;

public partial class Camera : Camera2D
{
	float scale = 2;
	float[] offsetConstants = { 0, 0, 0, 0, 0 };
	//because the above go back and forth at different rates, they might need to be reversed
	float[] offsetIterators = { 1, 1, 0, 1, 0 };
    float[] offsetIncrements = { 17.5f, 75f, 11.25f, 0.75f, 17.5f };
    float[,] offsetMaxMins = { {-1, 1 }, {-1, 1 }, {0, 2*Mathf.Pi }, {1, 3 }, {0, 2*Mathf.Pi } };

	float r = 0;

	match m;

	//for dynamic camera limiting
	Node2D LimitUL;
	Node2D LimitDR;

	playerBase player;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		m = (match)GetNode("../../../World");

		LimitUL = (Node2D)GetNode("../../CamBorderUL");
		LimitDR = (Node2D)GetNode("../../CamBorderDR");
		this.LimitTop = (int)LimitUL.Position.Y;
		this.LimitLeft = (int)LimitUL.Position.X;
		this.LimitBottom = (int)LimitDR.Position.Y;
		this.LimitRight = (int)LimitDR.Position.X;

		player = (playerBase)this.GetParent();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (player.shaking)
		{
			ShakeCamera();
		}
	}
	private void ShakeCamera()
	{
		Vector2 offsetPos;
		for (int i = 0; i < 5; i++)
		{
			if (offsetMaxMins[i, 1] != 2*Mathf.Pi)
			{
				//if its above max, make it go the other way. If its below min, make it 
				if (offsetConstants[i] > offsetMaxMins[i, 1])
				{
				    offsetIterators[i] = -1;
				} 

				if (offsetConstants[i] < offsetMaxMins[i, 0])
				{
				    offsetIterators[i] = 1;
				}
				//either reversed or not
				offsetConstants[i] += offsetIterators[i]*offsetIncrements[i]*(float)GetProcessDeltaTime();
			} else
			{
				offsetConstants[i] += offsetIncrements[i] * (float)GetProcessDeltaTime();
                //if they are angle - based and need to loop instead
                if (offsetConstants[i] > offsetMaxMins[i, 1])
				{
					offsetConstants[i] = offsetMaxMins[i, 0]; //should be 0, but just in case
				}
			}
		}
		r = offsetConstants[0] + (offsetConstants[1] * Mathf.Sin(offsetConstants[3] * (offsetConstants[4] + offsetConstants[3])));
		offsetPos = new Vector2(Mathf.Cos(offsetConstants[4]) * r, Mathf.Sin(offsetConstants[4]) * r);
		this.Offset = offsetPos * scale;
	}
}
