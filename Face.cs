using Godot;
using System;

public partial class Face : AnimatedSprite2D
{
	match m;
	Vector2 diff;
	playerBase parent;
	float angle;
	float distance;
	float lastDistance;
	[Export]
	public int faceSpriteIndex = 0;
	Vector2 startScale;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	parent = (playerBase)this.GetParent();
		distance = 20;

		startScale = new Vector2(this.Scale.X, this.Scale.Y);

		m = (match)GetNode("../../../World");
	}
	public void AfterReady() {
		if (parent.isAuthority) {
			//this.Frame = (int)Global.GetLocalPlayerProperty("faceIndex");
		}
		this.Frame = faceSpriteIndex;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!parent.sword.locked)
		{
			foreach (playerBase p in m.players)
			{
				diff = p.GlobalPosition - parent.GlobalPosition;

                if (lastDistance > diff.Length())
				{
					angle = diff.Angle();
				}
				lastDistance = diff.Length();
			}
		} else
		{
			angle = (parent.sword.GlobalPosition - parent.GlobalPosition).Angle();
		}

		if (Mathf.Abs(angle) > Mathf.Pi / 2)
		{
			this.Scale = new Vector2(startScale.X * -1, startScale.Y);
		} else
		{
			this.Scale = startScale;
		}

		Position = new Vector2(Mathf.Cos(angle)*distance, Mathf.Sin(angle)*distance);
	}
}
