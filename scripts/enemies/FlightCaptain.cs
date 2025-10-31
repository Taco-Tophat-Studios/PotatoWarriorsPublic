using Godot;
using System;
using System.Collections.Generic;

public partial class FlightCaptain : enemyBase
{
	[Export]
	public AnimatedSprite2D CharSprite;
	[Export]
	public RayCast2D LineOfSight;
	private bool inSight = false;
	private Timer StandbyTimer = new();
	private bool bombOrDive; //BOMB is TRUE and DIVE is FALSE
	private bool inPlayerZone;
	private const float flySpeed = 512f;
	private float distanceFromPlayer;
	private Vector2 distanceFromPlayer2D;
	private float diveTimeParameter = 0;
	private float fuelLevel = 1; //'cause he can run out of fuel lol (goes from 1 - 0)
	private bool outOfFuel = false; //can't use state because it may be overriden
	public List<FCBomb> bombs = new();
	private const float diveTime = 1.5f; //time for full dive (seconds)

	public Neuron Flight0 = new(), Flight1 = new(), Flight2 = new(), Choose0 = new(), Bomb0 = new(), Bomb1 = new(), Bomb2 = new(), Bomb3 = new(), Dive0 = new(), Dive1 = new(), Dive2 = new(), Dive3 = new();
	public Connection F0F1 = new(), F1F2 = new(), F2C0 = new(), C0B0 = new(), B0B1 = new(), B1B2 = new(), B2B3 = new(), B3F0 = new(), C0D0 = new(), D0D1 = new(), D1D2 = new(), D2D3 = new(), D3F0 = new();
    private bool inFlight = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		health = 200;
		f = (FendOffMatch)GetTree().Root.GetNode("World");
		f.enemies.Add(this);
		player = f.potato;

		StandbyTimer = new Timer(1, false, this);

		//FLYING (and choosing)----------------------------------------------------------------
		F0F1.ConstructConnection(Flight1, (Action a) => {CharSprite.AnimationFinished += a;});
		Flight0.ConstructNeuron(Flight, 0, new List<Connection> {F0F1}, this);

		F1F2.ConstructConnection(Flight2, () => {return true;});
		Flight1.ConstructNeuron(Flight, 1, new List<Connection> {F1F2}, this);
		Flight1.specialDebugFlag = "Flight1";

		F2C0.ConstructConnection(Choose0, (Action a) => {StandbyTimer.TimerFinished += a;});
		Flight2.ConstructNeuron(Flight, 1, new List<Connection> {F2C0}, this);

		C0B0.ConstructConnection(Bomb0, () => { return bombOrDive; });
		C0D0.ConstructConnection(Dive0, () => { return !bombOrDive; });
		Choose0.ConstructNeuron(Choose, 0, new List<Connection> {C0B0, C0D0}, this);
		//BOMBING------------------------------------------------------------------------------
		B0B1.ConstructConnection(Bomb1, (Action a) => { CharSprite.AnimationFinished += a; });
		Bomb0.ConstructNeuron(Bomb, 0, new List<Connection> {B0B1}, this);

		B1B2.ConstructConnection(Bomb2, () => { return inPlayerZone && distanceFromPlayer2D.Y < 0; });
		Bomb1.ConstructNeuron(Bomb, 1, new List<Connection> {B1B2}, this);

		B2B3.ConstructConnection(Bomb3, () => { return CharSprite.Frame == 1; /*REPLACE WHEN ANIM DONE*/ });
		Bomb2.ConstructNeuron(Bomb, 2, new List<Connection> {B2B3}, this);

		B3F0.ConstructConnection(Flight0, (Action a) => { CharSprite.AnimationFinished += a; });
		Bomb3.ConstructNeuron(Bomb, 3, new List<Connection> {B3F0}, this);
		//DIVING------------------------------------------------------------------------------
		D0D1.ConstructConnection(Dive1, (Action a) => { CharSprite.AnimationFinished += a; });
		Dive0.ConstructNeuron(Dive, 0, new List<Connection> {D0D1}, this);

		D1D2.ConstructConnection(Bomb2, () => { return inPlayerZone && inSight && distanceFromPlayer2D.Y < 0; });
		Dive1.ConstructNeuron(Dive, 1, new List<Connection> {D1D2}, this);

		D2D3.ConstructConnection(Bomb3, () => { return distanceFromPlayer <= 96; });
		Dive2.ConstructNeuron(Dive, 2, new List<Connection> {D2D3}, this);

		D3F0.ConstructConnection(Flight0, () => { return diveTimeParameter >= 1; });
		Dive3.ConstructNeuron(Dive, 3, new List<Connection> {D3F0}, this);

		currentNeuron = Flight0;
		state = "flight_start";
	}
	public override void _Process(double delta)
	{
		if ((IsOnWall() || IsOnFloor()) && (velocity.Length() >= 64 /*REPLACE WITH ACCURATE*/ || outOfFuel)) {
			Die();
		} else if (state != "stunned") {
			currentNeuron.CheckConnections();
		}
		if (!outOfFuel) {
			if (fuelLevel > 0) {
				fuelLevel -= 1/120 * (float)delta; //replace denominator with seconds until fuel run-out, with 120 it should be 2 minutes
			} else {
				outOfFuel = true;
				GD.Print("Loser's outta gas lol");
				state = "falling";
			}
			CharSprite.Scale = new Vector2(Mathf.Sign(distanceFromPlayer2D.X) * Mathf.Abs(CharSprite.Scale.X), CharSprite.Scale.Y);
		} else {
			velocity = new Vector2(velocity.X, velocity.Y + gravity * (float)delta);
		}

		if (state == "flying") {
			inPlayerZone = false;
			if (Mathf.Abs(distanceFromPlayer2D.X) > 1280) {
				velocity = new Vector2(flySpeed * Mathf.Sign(distanceFromPlayer2D.X), velocity.Y);
				inPlayerZone = true;
			}
			if (Mathf.Abs(distanceFromPlayer2D.Y) > 1280) {
				velocity = new Vector2(velocity.Y, flySpeed * Mathf.Sign(distanceFromPlayer2D.Y));
				inPlayerZone = true;
			}

			if (distanceFromPlayer < 640) {
				velocity = distanceFromPlayer2D.Normalized() * flySpeed;
				inPlayerZone = false;
			}
		}

		inSight = PlayerOnSight(LineOfSight);
		distanceFromPlayer2D = GlobalPosition - player.GlobalPosition;
		distanceFromPlayer = distanceFromPlayer2D.Length();
	}
	public void Flight(int phase) {
		switch (phase) {
			case 0:
				state = "start_flight";
				CharSprite.Play("flight_start");
				break;
			case 1:
				CharSprite.Play("fly");
				StandbyTimer.Start();
				break;
			case 2:
				inFlight = true;
				state = "flying";
				break;
		}
	}
	public void Choose (int phase) {
		bombOrDive = Convert.ToBoolean(new Random().Next(0, 2));
	}
	public void Bomb(int phase) {
		switch (phase) {
			case 0:
				state = "start_bomb";
				CharSprite.Play("bomb_start");
				break;
			case 1:
				state = "wait_bomb";
				CharSprite.Play("bomb_anticipation");
				break;
			case 2: //start drop
				state = "bombing";
				CharSprite.Play("bomb_drop");
				break;
			case 3: //throw bomb
				FCBomb b = new();
				b.Setup(f, this);
				break;
		}
	}
	public void Dive(int phase) {
		switch (phase) {
			case 0:
				state = "start_dive";
				CharSprite.Play("dive_start");
				break;
			case 1:
				state = "wait_dive";
				CharSprite.Play("dive_anciticpation");
				break;
			case 2:
				state = "dive_down";
				CharSprite.Play("dive_down");
				//go to where the player is upon eval (with some upwards offset)
				//then convert to velocity by dividing by time (half)
				velocity = (distanceFromPlayer2D + new Vector2(0, 32)) / (diveTime / 2);
				break;
			case 3:
				state = "dive_up";
				CharSprite.Play("dive_up");
				velocity = new Vector2(velocity.X, -velocity.Y);
				break;
		}
	}
}
