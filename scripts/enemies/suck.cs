using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class suck : enemyBase
{
	private const float jumpVel = 1280;
	private const float runSpeed = 512f;
	private int directionToPlayer; //misleading: he's just meant to switch directions every time he jumps off a wall, not go to the player (except in Ready())
	private Vector2 relPlayerPos;
	[Export]
	private AnimatedSprite2D CharSprite;
	Vector2I tilePos;
	Vector2 feetPos;

	public Neuron Run0 = new(), Run1 = new(), Jump0 = new(), Jump1 = new(), Jump2 = new(), Turn0 = new(), Turn1 = new(), Turn2 = new(), Shoot0 = new(), Shoot1 = new(), Shoot2 = new(), Bash0 = new(), Bash1 = new(), Bash2 = new(), Stun0 = new(), Stun1 = new();
	public Connection R0R1 = new(), R1J0 = new(), J0J1 = new(), J1J2 = new(), J2R0 = new(), R1T0 = new(), T0T1 = new(), T1T2 = new(), T2R0 = new(), T2S0 = new(), S0S1 = new(), S1S2 = new(), S2R0 = new(), R1B0 = new(), B0B1 = new(), B1B2 = new(), B2R0 = new(), St0St1 = new(), St1R0 = new();
	//YO if these neurons and stuff are throwing null whatsits, just do what was done for Soldier.cs lol
	public Timer StickTimer;
	[Export]
	RayCast2D fireRay;
	[Export]
	RayCast2D r; //detect player (not shooting, but checking for shooting)
	[Export]
	Line2D fireLine;
	[Export]
	ShapeCast2D JumpDetector;
	private bool shootConditions;
	private bool bashConditions;
	[Export]
	public CollisionShape2D DamageCol;
	bool scope = false;
	Vector2 startGunPosition; //keeping track of where the fire raycast should originate from
	Vector2 tempPlayerPos;

	public override void _Ready() {
		jumpDetectionFrameDelay = 3;
		health = 150;
		StickTimer = new Timer(1, false, this);
		state = "rolling"; //why worry about continuity of state names when you've got the sickest enemy ever?
		f = (FendOffMatch)GetTree().Root.GetNode("World");
		f.enemies.Add(this);
		player = f.potato;
		//directionToPlayer = Mathf.Sign(player.GlobalPosition.X - GlobalPosition.X); //set later
		SpritesStartScale = new Vector2(CharSprite.Scale.X, CharSprite.Scale.Y);

		//12 frames at 20 fps to hit wall going 512 px/s means the wall sensing box has to be
		int highJumpDistance = (int)(runSpeed * 12 / (float)CharSprite.SpriteFrames.GetAnimationSpeed("turnStart")) + 64;
		JumpColDimensions = new Dictionary<JumpKind, Tuple<Vector2, Vector2>>
		{   //											VVVV	Size	VVVV  | VVVV	Pos 	VVVV
			{ JumpKind.Low, new Tuple<Vector2, Vector2>(new Vector2(228, 192), new Vector2(116, 0)) },
			{ JumpKind.LowCeiling, new Tuple<Vector2, Vector2>(new Vector2(164, 192), new Vector2(82, -192)) },
			{ JumpKind.High, new Tuple<Vector2, Vector2>(new Vector2(highJumpDistance, 192), new Vector2(highJumpDistance / 2, -192)) },
			{ JumpKind.HighCeiling, new Tuple<Vector2, Vector2>(new Vector2(highJumpDistance-64, 192), new Vector2(highJumpDistance / 2 - 32, -384)) }
		};

		RectangleShape2D jumpRect = (RectangleShape2D)JumpDetector.Shape;
		jumpRect.Size = new Vector2(highJumpDistance, 192);
		JumpDetector.Position = new Vector2(highJumpDistance / 2, -192);

		//RUNNING--------------------------------------------------------------------------
		R0R1.ConstructConnection(Run1, () => { return state == "rolling"; });
		Run0.ConstructNeuron(Run, 0, new List<Connection> { R0R1 }, this);
		Run0.specialDebugFlag = "Run 0";

		R1J0.ConstructConnection(Jump0, () => { return CheckJumpType().Equals("low"); });
		R1T0.ConstructConnection(Turn0, () => { return CheckJumpType().Equals("high"); });
		R1B0.ConstructConnection(Bash0, () => { return bashConditions; });
		Run1.ConstructNeuron(Run, 1, new List<Connection> { R1J0, R1T0, R1B0 }, this);
		Run1.specialDebugFlag = "Run 1";
		//JUMPING--------------------------------------------------------------------------
		J0J1.ConstructConnection(Jump1, () => { return CharSprite.Frame >= 6; });
		Jump0.ConstructNeuron(Jump, 0, new List<Connection> { J0J1 }, this);
		Jump0.specialDebugFlag = "Jump 0";

		J1J2.ConstructConnection(Jump2, () => { return IsOnFloor(); });
		Jump1.ConstructNeuron(Jump, 1, new List<Connection> { J1J2 }, this);
		Jump1.specialDebugFlag = "Jump 1";

		J2R0.ConstructConnection(Run0, (Action a) => { CharSprite.AnimationFinished += a; });
		Jump2.ConstructNeuron(Jump, 2, new List<Connection> { J2R0 }, this);
		Jump2.specialDebugFlag = "jump 2";
		//TURNING--------------------------------------------------------------------------
		T0T1.ConstructConnection(Turn1, () => { return CharSprite.Frame >= 7;});
		Turn0.ConstructNeuron(Turn, 0, new List<Connection> { T0T1 }, this);
		Turn0.specialDebugFlag = "turn 0";

		T1T2.ConstructConnection(Turn2, (Action a) => { CharSprite.AnimationFinished += a; });
		Turn1.ConstructNeuron(Turn, 1, new List<Connection> { T1T2 }, this);
		Turn1.specialDebugFlag = "turn 1";

		T2R0.ConstructConnection(Run0, () => { return IsOnFloor(); });
		T2S0.ConstructConnection(Shoot0, () => { return shootConditions; });
		Turn2.ConstructNeuron(Turn, 2, new List<Connection> { T2R0, T2S0 }, this);
		Turn2.specialDebugFlag = "turn 2";
		//SHOOTING--------------------------------------------------------------------------
		S0S1.ConstructConnection(Shoot1, () => { return CharSprite.Frame >= 2; });
		Shoot0.ConstructNeuron(Shoot, 0, new List<Connection> { S0S1 }, this);
		Shoot0.specialDebugFlag = "shoot 0";

		S1S2.ConstructConnection(Shoot2, () => { return CharSprite.Frame >= 5; });
		Shoot1.ConstructNeuron(Shoot, 1, new List<Connection> { S1S2 }, this);
		Shoot1.specialDebugFlag = "shoot 1";

		S2R0.ConstructConnection(Run0, () => { return IsOnFloor(); });
		Shoot2.ConstructNeuron(Shoot, 2, new List<Connection> { S2R0 }, this);
		Shoot2.specialDebugFlag = "shoot 2";
		//BASHING--------------------------------------------------------------------------
		B0B1.ConstructConnection(Bash1, () => { return CharSprite.Frame >= 9; });
		Bash0.ConstructNeuron(Bash, 0, new List<Connection> { B0B1 }, this);
		Bash0.specialDebugFlag = "bash 0";

		B1B2.ConstructConnection(Bash2, () => { return CharSprite.Frame >= 19; }); //when it sorta stops in the anim
		Bash1.ConstructNeuron(Bash, 1, new List<Connection> { B1B2 }, this);
		Bash1.specialDebugFlag = "bash 1";

		B2R0.ConstructConnection(Run0, (Action a) => { CharSprite.AnimationFinished += a; }); //reminder: what you plug in is the condition for the NEXT neuron to fire
		Bash2.ConstructNeuron(Bash, 2, new List<Connection> { B2R0 }, this);
		Bash2.specialDebugFlag = "bash 2";
		//STUNNING--------------------------------------------------------------------------
		St0St1.ConstructConnection(Stun1, (Action a) => { CharSprite.AnimationFinished += a; });
		Stun0.ConstructNeuron(Stun, 0, new List<Connection> { St0St1 }, this);

		St1R0.ConstructConnection(Run0, (Action a) => { CharSprite.AnimationFinished += a; });
		Stun1.ConstructNeuron(Stun, 1, new List<Connection> { St1R0 }, this);

		directionToPlayer = Mathf.Sign(player.GlobalPosition.X - GlobalPosition.X);

		currentNeuron = Run0;
		currentNeuron.MethodToExecute(0);

		startGunPosition = new Vector2(fireRay.Position.X, fireRay.Position.Y);
		AccessibleDamageArea = (Area2D)DamageCol.GetParent();
	}

	public override void _Process(double delta)
	{
		relPlayerPos = player.GlobalPosition - this.GlobalPosition;
		
		if (state != "stunned")
		{
			currentNeuron.CheckConnections();
			if (state != "onWall" && state != "shooting") {
				velocity = new Vector2(velocity.X, velocity.Y + gravity * (float)GetProcessDeltaTime());
			} else if (state == "onWall" || state == "shooting") {
				velocity = new Vector2(velocity.X, velocity.Y + 1.0f / 2.0f * gravity * (float)GetProcessDeltaTime());
			}
		}
		
		if (state == "rolling") {
			velocity = new Vector2(runSpeed * directionToPlayer, velocity.Y);
		}
		if (scope) {
			fireLine.ClearPoints();
			fireLine.AddPoint(new Vector2(startGunPosition.X * directionToPlayer, startGunPosition.Y));
			fireLine.AddPoint(player.GlobalPosition - fireLine.GlobalPosition);
		}

		if (CharSprite.Animation == "turnStart" && CharSprite.Frame >= 12) {
			tempPlayerPos = new Vector2(player.GlobalPosition.X, player.GlobalPosition.Y);
		}

		bashConditions = false;//IsOnFloor() && PlayerOnSight() && Mathf.Abs(relPlayerPos.X) <= 512;
		shootConditions = PlayerOnSight(r); //doesn't matter the range, this guy is a SNIPER

		if (directionToPlayer != 0) {
			CharSprite.Scale = new Vector2(SpritesStartScale.X * directionToPlayer, SpritesStartScale.Y);
		}

		Velocity = velocity;
		MoveAndSlide();
		if (Velocity.Y == 0 && velocity.Y > 0) { //to prevent from inifitely (or worse, overflowing) velocity because only Velocity is set to 0 when colliding with ground
			velocity = new Vector2(velocity.X, 0);
		}
	}

	public void Run (int phase) {
		switch (phase) {
			case 0 :
				velocity = new Vector2(runSpeed * directionToPlayer, velocity.Y);
				state = "rolling";
				CharSprite.Play("ride");
				break;
			case 1:
				break;
			//probably don't need two run neurons, but just in case...
		}
	}
	public void Jump (int phase) {
		switch (phase) {
			case 0 :
				state = "jumping";
				CharSprite.Play("jumpStart"); //You know what? screw it. This guy's cool enough to do the bash as his normal jump
				break;
			case 1:
				velocity = new Vector2(velocity.X, -2*jumpVel);
				CharSprite.Play("jumpLoop");
				break;
			case 2:
				velocity = new Vector2(velocity.X, 0);
				CharSprite.Play("jumpEnd");
				break;
		}
	}
	public void Turn (int phase) {
		switch (phase) {
			case 0 :
				state = "turning";
				CharSprite.Play("turnStart");
				break;
			case 1:
				velocity = new Vector2(velocity.X, velocity.Y - jumpVel);
				break;
			case 2:
				state = "onWall";
				velocity = new Vector2(0, 0);
				CharSprite.Play("turnWithoutShoot");
				scope = true;
				directionToPlayer *= -1; //to switch direcitons | NOTE: make sure to negate this in Shoot before velocity set at shoot 2
				break;
		}
	}
	public void Shoot (int phase) {
		switch (phase) {
			case 0 :
				state = "shooting";
				CharSprite.Play("turnWithShoot");
				break;
			case 1:
				fireRay.Enabled = true;
				scope = false;

				fireRay.AddException(this);
				fireRay.Position = new Vector2(startGunPosition.X * -directionToPlayer, startGunPosition.Y);
				fireRay.TargetPosition = tempPlayerPos - this.GlobalPosition; //- gunOffset

				fireRay.ForceRaycastUpdate();
				fireLine.ClearPoints();
				fireLine.AddPoint(new Vector2(startGunPosition.X * -directionToPlayer, startGunPosition.Y));

				if (fireRay.IsColliding()) {
					//to account for sword, shield, and fist collisions
					if (fireRay.GetCollider() == player)
					{
						player.Damage(150, "S. U. C. K. Fire ray"); //i dunno
						fireLine.AddPoint(fireRay.GetCollisionPoint());
					}
					else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Sword) && player.tool.angVel >= PlayerToolUniv.maxAngVel / 3f)
					{
						//reflect if sword moving toward raycast
						AnimatedSprite2D reflectEffect = (AnimatedSprite2D)player.VFXTemp.Duplicate();
						player.AddToVFXTemp(reflectEffect, player.tool.tipRelPos, new Vector2(7, 7), "effect");
						this.Die();
						//Reflect back into that enemy
						fireLine.AddPoint(player.tool.tipGlobalPos - this.GlobalPosition);
						//can just set the point to the random offset, because fireLine's origin is at S. U. C. K.
						fireLine.AddPoint(new Vector2(rand.Next(-24, 25), rand.Next(-24, 25))); //TODO: make this decay with death
						
					} else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Shield)) {
						player.Heal(100);
					} else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Fist)) {
						player.tool.StealFire(200, 200);
					}
				}
				 else {
					fireLine.AddPoint(fireRay.TargetPosition);
				}
				scope = false;
				break;
			case 2:
				state = "inAir";
				fireLine.ClearPoints();
				velocity = new Vector2(runSpeed * directionToPlayer, velocity.Y); //normally you'd do this in Run, but to make him jump OFF the wall, do this
				break;
		}
	}

	public void Bash (int phase) {
		switch (phase) {
			case 0:
				state = "bashing";
				CharSprite.Play("bash");
				break;
			case 1:
				DamageCol.Disabled = false;
				break;
			case 2:
				DamageCol.Disabled = true;
				break;
		}
	}

	public void Stun(int phase) {
		switch (phase)
		{
			case 0:
				if (rand.Next(0, 2) == 0)
				{
					CharSprite.Animation = "stun1";
				}
				else
				{
					CharSprite.Animation = "stun2";
				}
				CharSprite.Play();
				state = "stunned";
				break;
			case 1:
				state = "recovering";
				CharSprite.Play("melee_idle");
				break;
		}
	}
	public override void Stun() {
		Stun(0);
	}

	

	public override void Die()
	{
		StickTimer.Stop();
		CharSprite.Play("die");
		fireLine.ClearPoints();
		state = "dead";
		f.enemies.Remove(this);
	}

	//if it is a high wall, jump and stick to it. If a normal wall, just jump normally. If none, just keep on keepin'
	public string CheckJumpType() {
		if (CheckToJump(JumpKind.High)) {
				return "high";
		}
		if (CheckToJump(JumpKind.Low)) { //check low first bc it includes all cases of high and excludes all cases of not high
			
			return "low";
		}
		return "none";
	}
	private bool CheckToJump(JumpKind jumpConfig) //forward from player, upward from ground (starting at 1 for each)
	{
		//TODO: Reconfigure this to make sure they jump when there is a SOLID wall

		//recurse to check for ceiling first. You can recurse because it is checking for the normal JumpKind enums, then doing this with the Ceiling JumpKind enums
		if (jumpConfig == JumpKind.Low && CheckToJump(JumpKind.LowCeiling)) {
			return false;
		} else if (jumpConfig == JumpKind.High && CheckToJump(JumpKind.HighCeiling)) {
			return false;
		}
		//resize jump colliders for normal and ceiling
		RectangleShape2D jumpRect = (RectangleShape2D)JumpDetector.Shape;
		Tuple<Vector2, Vector2> temp = JumpColDimensions[jumpConfig];
		jumpRect.Size = new Vector2(temp.Item1.X, temp.Item1.Y);
		JumpDetector.Position = new Vector2(temp.Item2.X * directionToPlayer, temp.Item2.Y);

		JumpDetector.ForceShapecastUpdate(); //May impact performance (although its checking in just a signle point, so it should only run once)
		if (JumpDetector.IsColliding()) {
			for (int i = 0; i < JumpDetector.GetCollisionCount(); i++) {
				if (JumpDetector.GetCollider(i) == f.tm) {
					return true;
				}
			}
		}

		return false;        
	}
	public override void _on_char_sprite_animation_finished() //WARNING: DOES NOT CALL IF AN ANIMATION IS LOOPING
	{
		if (state == "dead")
		{
			this.QueueFree();
		}
	}
	private void _on_damage_area_body_entered(Node2D body)
	{
		if (body == f.potato) {
			f.potato.Damage(100, "S. U. C. K. Damage Area");
		}
	}
}