using Godot;
using System;

public partial class launcher : Area2D
{
	match m;
	playerBase enteringPlayer;
	bool active;
	float boost;
	Sprite2D keyOverlay;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		m = (match)this.GetParent();
        boost = 4000;
		active = false;

		keyOverlay = (Sprite2D)GetNode("KeyOverlay");
		keyOverlay.Visible = false;

		keyOverlay.GlobalRotation = 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (active && Input.IsActionJustPressed("objectInteract")) {
			enteringPlayer.Velocity = Vector2.Zero;
			enteringPlayer.effectVelocity = new Vector2(Mathf.Sin(Rotation) * boost, -Mathf.Cos(Rotation) * boost);
			
			
			active = false;
			keyOverlay.Visible = false;
		}
	}
	private void _on_body_entered(Node2D body)
	{
		foreach (playerBase p in m.players) {
			if (body == (Node2D)p) {
				enteringPlayer = (playerBase)body;
				active = true;
				keyOverlay.Visible = true;
			}
		}
		
	}
	private void _on_body_exited(Node2D body)
	{
		foreach (playerBase p in m.players) {
			if (body == (Node2D)p) {
				active = false;
				keyOverlay.Visible = false;
				//dont need to set enteringPlayer to null or something because it will be set anyway
			}
		}
	}
}



