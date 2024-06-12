using Godot;
using System;

public partial class CreditButton : Area2D
{
	CreditsText cont;
	int lr;

	Timer t;
	bool vis = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		t = new Timer(1, false, this);
		
		cont = (CreditsText)GetNode("../CreditsText");
		
		if (this.Name == "LeftArrow")
		{
			lr = -1;
		} else {
			lr = 1;
		}
		t.TimerFinished += SetVisible;
	}
	private void _on_area_entered(Area2D area)
	{
		if (area == (Area2D)GetNode("../SimpleSword") && t.done)
		{
			cont.SetText(lr);
			((Sprite2D)GetNode("ArrowSprite")).Visible = false;
			t.Start();
		}
	}
	private void SetVisible() {
		((Sprite2D)GetNode("ArrowSprite")).Visible = true;
	}
	
}

