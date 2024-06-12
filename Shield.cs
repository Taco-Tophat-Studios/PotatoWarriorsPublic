using Godot;
using System;

public partial class Shield : Area2D, IPlayerTools
{
	public Timer tc;
	[Export]
	public Timer t;
	public bool active = false;
	private Vector2 pos;
	private float angle;
	private const float dist = 100;
	private AnimatedSprite2D shieldSpr;
	private CollisionShape2D shieldCol;
	private playerBase potato;
	private swordBase ownSword;
	private match m;
	private TextureProgressBar cooldownSpr;
	public bool authority;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		potato = (playerBase)this.GetParent();
		
		t = new Timer(3, false, this);

		t.TimerFinished += Reset;

		m = (match)GetNode("../../../World");

        ownSword = (swordBase)GetNode("../Sword");
		cooldownSpr = (TextureProgressBar)GetNode("../GUICanvas/CooldownSprites").GetNode("ShieldCooldownSprite");

        shieldSpr = (AnimatedSprite2D)GetNode("ShieldSprite");
		shieldCol = (CollisionShape2D)GetNode("ShieldCollider");
		

		shieldSpr.Visible = false;
		shieldCol.Disabled = true;
		
    }
	public void AfterReady() {
		if (potato.isAuthority) {
			authority = true;

			tc = new Timer(2, true, this);
			tc.TimerFinished += ShieldCooldownDone;
			cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/ShieldCooldown.png");

			Global.LoadCharData();
			shieldSpr.Frame = (int)Global.GetLocalPlayerProperty("shieldIndex");
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (authority) {
			cooldownSpr.Value = Mathf.RoundToInt(100*tc.timerVal / tc.threshHold);
			//activate
			if (Input.IsActionJustPressed("shield") && tc.done) {
				SetVars(true);
			}
			//deactivate
			if (Input.IsActionJustReleased("shield") && t.active && active) {
				SetVars(false);
			}

			if (t.active) {

				//I refuse to use variables for silly things like "making code readable". This statement is perfectly fine and not long at all
				angle = Mathf.Atan2(GetGlobalMousePosition().Y - potato.GlobalPosition.Y, GetGlobalMousePosition().X - potato.GlobalPosition.X);
				shieldCol.Rotation = angle;
				shieldSpr.Rotation = angle;

				pos = new Vector2(Mathf.Cos(angle)*dist, Mathf.Sin(angle)*dist);
				pos += potato.GlobalPosition;
				shieldCol.GlobalPosition = pos;
				shieldSpr.GlobalPosition = pos;

				if (this.GlobalScale != new Vector2(1, 1))
				{
					this.GlobalScale = new Vector2(1, 1);
				}
			
			}
		}
		
	}
	private void ShieldCooldownDone() {
		cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/ShieldCooldownDone.png");
	}
	public void Reset() {
		SetVars(false);
	}
	//either turn what's on off or turn what's off on (set is true when called upon activation,
	//false upon reset
	private void SetVars (bool set) {
		active = set;
		shieldSpr.Visible = set;
		shieldCol.Disabled = !set;
		t.Stop();
        ownSword.Visible = !set;
        
		if (!set) {
			tc.Start();
			cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/ShieldCooldown.png");
		} else {
			t.Start();
		}
    }
	private void _on_area_entered(Area2D area)
	{
		if (active) {
			for (int i = 0; i < m.swordAreas.Count; i++) {
				if (area == m.swordAreas[i] && m.swordAreas[i] != ownSword) {
					//heal player a LITTLE bit
					potato.Heal(32);
					swordBase s = (swordBase)m.players[i].GetNode("Sword");
                    s.CallDeferred("Lock");

                    AudioStreamPlayer a = (AudioStreamPlayer)GetNode("../SFXMaker");
					a.Stream = Global.sfx[7];
					a.Play();

					//only set this to true if the shield has interacted with anything, seeing as it doesn't
					//do anything otherwise
					potato.hasUsedShield = true;
        		} else if (area == (Area2D)(m.players[i].GetNode("Fist")))
				{
					CallDeferred("SetVars", false);
					((Fist)(m.players[i].GetNode("Fist"))).CallDeferred("ResetPunch");
    	    	    ((Fist)(m.players[i].GetNode("Fist"))).active = false;

					potato.hasUsedShield = true;
				}
			}
        
    	}
	}
		
}



