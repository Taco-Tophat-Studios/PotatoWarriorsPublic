using Godot;
using System;
using System.Collections.Generic;

public partial class FCBomb : Area2D
{
	private const float dramaticSpeedScale = 3; //how much faster the bomb will "tick" when close to finishing
	private FendOffMatch FOM;
	private FlightCaptain captain;
	public enum BombTypes {
		Small,
		Medium,
		Large
	}
	public BombTypes type;
	[Export]
	public AnimatedSprite2D bombSprite;
	[Export]
	public CollisionShape2D BombCol;
	public Timer explosionTimer; //not necessarily a dict because 1. Could change externally with different difficulties and 2. I'm lazy lol
	//public float[,] damageFunctionParameters = new float[3, 3] {{2, -0.009f, 512}, {1, -0.009f, 512}, {1.6f, -0.009f, 384}};
	public Dictionary<BombTypes, float[]> damageFunctionParameters = new() {
		{BombTypes.Small, new float[3]{2, -0.009f, 512}},
		{BombTypes.Medium, new float[3]{1, -0.009f, 512}},
		{BombTypes.Large, new float[3]{1.6f, -0.009f, 384}}
	};
	//store the bomb's anim name
	private Dictionary<BombTypes, string> bombSprites = new() {
		{BombTypes.Small, "smallBomb"},
		{BombTypes.Medium, "mediumBomb"},
		{BombTypes.Large, "largeBomb"},
	};
	//bomb colliders size and explosion sprite scale
	private Dictionary<BombTypes, Vector2I> bombSizes = new() {
		{BombTypes.Small, new Vector2I(16, 4)},
		{BombTypes.Medium, new Vector2I(24, 2)},
		{BombTypes.Large, new Vector2I(32, 1)},
	};
	public void Setup(BombTypes b, FendOffMatch f, FlightCaptain c) {
		type = b;
		explosionTimer.threshHold = (float)((new Random()).NextDouble() * 2 + 4); //4 - 6 seconds
		FOM = f;
		captain = c;

		bombSprite.Play(bombSprites[type]);
		((CircleShape2D)BombCol.Shape).Radius = bombSizes[type].X / 2;

		c.bombs.Add(this);
	}
	public void Setup(FendOffMatch fe, FlightCaptain c) {
		Random r = new();
		double d = r.NextDouble();

		if (d < 0.6) {
			Setup(BombTypes.Small, fe, c);
		} else if (d < 0.9) {
			Setup(BombTypes.Medium, fe, c);
		} else {
			Setup(BombTypes.Large, fe, c);
		}
	}
	public void Explode() {
		//TODO: put a raycast here or something to check for terrain in the way
		
		//resize explosion based on dicts
		bombSprite.Play("explosion");
		bombSprite.Scale = new Vector2I(bombSizes[type].X, bombSizes[type].X);

		float playerDistance = (FOM.potato.GlobalPosition - this.GlobalPosition).Length();
		float[] p = damageFunctionParameters[type];
		int damage = (int)(p[0] * Mathf.Pow(Mathf.E, p[1] * (playerDistance - p[2])) );
		//because otherwise it'd be too insignificant
		if (damage >= 5) {
			FOM.potato.Damage(damage, "Flight Captain Bomb");
		}
	}
	public void FinishExplosion() {
		captain.bombs.Remove(this);
		this.QueueFree();
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//make it "tick" faster (LIKE IN THE MOVIES AHAHAH) if the timer onlyhas a third of the original time left
		if (explosionTimer.timerVal >= 0.6666f * explosionTimer.threshHold && bombSprite.SpeedScale != 3) {
			bombSprite.SpeedScale = dramaticSpeedScale;
		}
	}
}
