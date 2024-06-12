using Godot;
using System;

public partial class coinScript : CharacterBody2D
{
	//this exists in addition to match.coining so one player doesn't coin while the other player is
	public bool isCoining = false;
	[Export]
	public bool heads;
	//player can only coin once
	public bool hasCoined = false;
	private float gravity = 800f;
	private float boost = 250;
	Timer t;
	private AnimatedSprite2D coinSprite;
	private CollisionShape2D coinCollider;
	private Label coinLabel;
	private match m;
	AudioStreamPlayer a;

	public playerBase player;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		t = new Timer(1.5f, false, this);
		
		m = (match)GetNode("../../World");
		this.Visible = false;
		coinSprite = (AnimatedSprite2D)GetNode("CoinSprite");
		coinCollider = (CollisionShape2D)GetNode("CoinCollider");
		coinCollider.Disabled = true;
		coinLabel = (Label)GetNode("../UI/CoinLabel");
		coinLabel.Text = "";
		a = (AudioStreamPlayer)GetNode("../SFXMakerWorld");

		t.TimerFinished += EndCoin; //I stressed over this for 10 minutes. Turns out, you don't include the parentheses. Dude.

		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//continue
		if (isCoining && !IsOnFloor()) {
			//grav is 1500
			Velocity = new Vector2(Velocity.X, Velocity.Y + gravity * (float)GetProcessDeltaTime());
		}

		//end
		if (IsOnFloor() && isCoining) {
			EndCoin();
        }
		MoveAndSlide();
	}
	public void StartCoin() {
		t.Start();
		m.coining = true;
		m.freeze = true;
		isCoining = true;

		this.Visible = true;
		coinSprite.Play();
		coinCollider.Disabled = false;

		if (player.isAuthority) {
			Position = GetGlobalMousePosition();
		}
		
		Velocity = new Vector2(Velocity.X, Velocity.Y - boost);

		//use some if statement of something for the separate coins
		((Sprite2D)GetNode("../UI/Coin1Sprite")).Visible = false;
		//((Sprite2D)GetNode("../UI/Coin2Sprite")).Visible = false;

		//a.Stream = Global.sfx[2];
		a.Play();
	}
	public void EndCoin() {
		isCoining = false;
		hasCoined = true;

		Random r = new Random();
		int ht = r.Next(0, 2);
		if (ht == 1) {
			coinLabel.Text = "HEADS";
		} else {
			coinLabel.Text = "TAILS";
		}

		this.Visible = false;
		coinCollider.Disabled = true; 
		coinSprite.Stop();
		coinSprite.Frame = 0;

		//a.Stream = Global.sfx[1];
		a.Play();
	}
}
