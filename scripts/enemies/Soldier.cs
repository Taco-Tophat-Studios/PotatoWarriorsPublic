using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public partial class Soldier : enemyBase
{
	public bool running = true;
	public float runSpeed = 384f;
	[Export]
	AnimatedSprite2D CharSprite;
	
	[Export]
	Line2D fireLineVis;
	[Export]
	CollisionShape2D bodyCol;
	[Export]
	CollisionShape2D damageCol;
	[Export]
	Area2D meleeArea;
	[Export]
	CollisionShape2D meleeCollider;

	Vector2 BulletVel;
	Vector2 BulletPos;

	[Export]
	RayCast2D fireRay;

	Timer ShootTimer;
	Timer StunTimer;
	Timer MeleeTimer;

	[Export]
	RayCast2D r;

	Vector2 meleeColliderPos;

	public float xDist;
	bool shootConditions;
	bool meleeConditions;

	const float jumpVel = 550;

	bool shot = false;
	Vector2 tempPlayerPos; //for the delay between staritng to shoot and shooting
	Vector2 relPlayerPos;

	Vector2I tilePos;
	Vector2 feetPos;
	[Export]
	ShapeCast2D JumpDetector;
	bool tilesInJumpPath = false;
	int directionToPlayer;

	public Timer testTimer;
	Vector2 gunOffset;
	bool scope = false;

	/*can't declare these all to be new neurons until _Ready(). Maybe putting it at the end of the whole thing only applies to the last?
	I don't want to do this for each one and test it cause it's not worth the time lol
	UPDATE: apparently you can, but there may be some issues. For example, doing this for multiple random objects
	results in them all having the same seed*/
	public Neuron Run0, Run1, Jump0, Melee0, Melee1, Melee2, Melee3, Melee4, Shoot0, Shoot1, Shoot2, Shoot3, Shoot4, Stun0, Stun1 = new();
	public Connection R0R1, R1J0, J0R0, R1M0, M0M1, M1M2, M2M3, M3M4, M4M1, M1R0, M1S0, R1S0, S0S1, S1S2, S2S3, S3S4, S4S1, S1R0, S1M0, St0St1, St1M1 = new();

	public override void _Ready()
	{
		health = 100;
		Run0 = new Neuron(); Run1 = new Neuron(); Jump0 = new Neuron(); Melee0 = new Neuron(); Melee1 = new Neuron(); Melee2 = new Neuron(); Melee3 = new Neuron(); Melee4 = new Neuron(); Shoot0 = new Neuron(); Shoot1 = new Neuron(); Shoot2 = new Neuron(); Shoot3 = new Neuron(); Shoot4 = new Neuron(); Stun0 = new Neuron(); Stun1 = new Neuron();
		R0R1 = new Connection(); R1J0 = new Connection(); J0R0 = new Connection(); R1M0 = new Connection(); M0M1 = new Connection(); M1M2 = new Connection(); M2M3 = new Connection(); M3M4 = new Connection(); M4M1 = new Connection(); M1R0 = new Connection(); M1S0 = new Connection(); R1S0 = new Connection(); S0S1 = new Connection(); S1S2 = new Connection(); S2S3 = new Connection(); S3S4 = new Connection(); S4S1 = new Connection(); S1R0 = new Connection(); S1M0 = new Connection(); St0St1 = new Connection(); St1M1 = new Connection();
		testTimer = new Timer(1.5f, true, this);
		f = (FendOffMatch)GetTree().Root.GetNode("World");
		f.enemies.Add(this);
		player = f.potato;

		JumpColDimensions = new Dictionary<JumpKind, Tuple<Vector2, Vector2>>
        {
            { JumpKind.Low, new Tuple<Vector2, Vector2>(new Vector2(256, 192), new Vector2(192, 0)) },
            { JumpKind.LowCeiling, new Tuple<Vector2, Vector2>(new Vector2(256, 192), new Vector2(160, -192)) }
        };

		r.AddException(this);
		fireLineVis.Width = 2;
		fireLineVis.DefaultColor = new Color(0, 1, 0);

		ShootTimer = new Timer(3, false, this);
		StunTimer = new Timer(2, false, this);
		MeleeTimer = new Timer(2, false, this);

		state = "running";
		//CharSprite.Animation = "run"; automatically set
		f.InitializeTileExistance();
		//running
		R0R1.ConstructConnection(Run1, (Action a) => { CharSprite.AnimationFinished += a; });
		Run0.ConstructNeuron(Run, 0, new List<Connection> { R0R1 }, this);
		
		R1J0.ConstructConnection(Jump0, () => { return CheckToJump(JumpKind.Low); });
		R1M0.ConstructConnection(Melee0, () => { return meleeConditions; });
		R1S0.ConstructConnection(Shoot0, () => { return shootConditions && !meleeConditions; });
		Run1.ConstructNeuron(Run, 1, new List<Connection> { R1J0, R1M0, R1S0 }, this);
		
		//jumping
		J0R0.ConstructConnection(Run0, () => { return IsOnFloor(); });
		Jump0.ConstructNeuron(Jump, 0, new List<Connection> { J0R0 }, this);
		
		//Melee
		M0M1.ConstructConnection(Melee1, (Action a) => { CharSprite.AnimationFinished += a; });
		Melee0.ConstructNeuron(Melee, 0, new List<Connection> { M0M1 }, this);
		
		M1M2.ConstructConnection(Melee2, (Action a) => { MeleeTimer.TimerFinished += a; });
		M1R0.ConstructConnection(Run0, () => { return !meleeConditions && !shootConditions; });
		M1S0.ConstructConnection(Shoot0, () => { return shootConditions && !meleeConditions; });
		Melee1.ConstructNeuron(Melee, 1, new List<Connection> { M1M2, M1R0, M1S0 }, this);
		
		M2M3.ConstructConnection(Melee3, () => { return CharSprite.Frame >= 7; });
		Melee2.ConstructNeuron(Melee, 2, new List<Connection> { M2M3 }, this);
		
		M3M4.ConstructConnection(Melee4, () => { return CharSprite.Frame >= 10; });
		Melee3.ConstructNeuron(Melee, 3, new List<Connection> { M3M4 }, this);
		
		M4M1.ConstructConnection(Melee1, (Action a) => { CharSprite.AnimationFinished += a; });
		Melee4.ConstructNeuron(Melee, 4, new List<Connection> { M4M1 }, this);
		
		//Shooting
		S0S1.ConstructConnection(Shoot1, (Action a) => { CharSprite.AnimationFinished += a; });
		Shoot0.ConstructNeuron(Shoot, 0, new List<Connection> { S0S1 }, this);
		
		S1S2.ConstructConnection(Shoot2, (Action a) => { ShootTimer.TimerFinished += a; });
		S1R0.ConstructConnection(Run0, () => { return !shootConditions && !meleeConditions; });
		S1M0.ConstructConnection(Melee0, () => { return meleeConditions; }); //not melee and !shoot because melee is prioritized if available
		Shoot1.ConstructNeuron(Shoot, 1, new List<Connection> { S1S2, S1R0, S1M0 }, this);
		
		S2S3.ConstructConnection(Shoot3, () => { return CharSprite.Frame >= 6; });
		Shoot2.ConstructNeuron(Shoot, 2, new List<Connection> { S2S3 }, this);
		
		S3S4.ConstructConnection(Shoot4, () => { return true; });
		Shoot3.ConstructNeuron(Shoot, 3, new List<Connection> { S3S4 }, this);
		
		S4S1.ConstructConnection(Shoot1, (Action a) => { CharSprite.AnimationFinished += a; });
		Shoot4.ConstructNeuron(Shoot, 4, new List<Connection> { S4S1 }, this);

		//Stunning
		St0St1.ConstructConnection(Stun1, (Action a) => { CharSprite.AnimationFinished += a; });
		Stun0.ConstructNeuron(Stun, 0, new List<Connection> { St0St1 }, this);

		St1M1.ConstructConnection(Melee1, (Action a) => { CharSprite.AnimationFinished += a; });
		Stun1.ConstructNeuron(Stun, 1, new List<Connection> { St1M1 }, this);

		currentNeuron = Run0;
		Run0.MethodToExecute(0);

		state = "running";

		meleeColliderPos = meleeCollider.Position;

		AccessibleDamageArea = (Area2D)damageCol.GetParent();
	}

	public override void _Process(double delta)
	{
		relPlayerPos = player.GlobalPosition - GlobalPosition;
		if (state != "stunned")
		{
			currentNeuron.CheckConnections();
			velocity = new Vector2(velocity.X, velocity.Y + gravity * (float)GetProcessDeltaTime());
		} else
		{
			gunOffset = new(92 * directionToPlayer, 8);
		}
		if (scope) {
			//fireLine.Points[1] = player.GlobalPosition - fireLine.GlobalPosition;
			fireLineVis.ClearPoints();
			fireLineVis.AddPoint(gunOffset);
			fireLineVis.AddPoint(player.GlobalPosition - fireLineVis.GlobalPosition);
			//fireLineVis.QueueFree();
		}

		directionToPlayer = Mathf.Sign(relPlayerPos.X);
		//maybe use this.scale if it doesn't affec the way the distance to player and check for tiles is calculated
		CharSprite.Scale = new Vector2(directionToPlayer * 2, 2);
		meleeCollider.Position = new Vector2(meleeColliderPos.X*directionToPlayer, meleeColliderPos.Y);

		xDist = Mathf.Abs(relPlayerPos.X);
		shootConditions = PlayerOnSight() && xDist <= 1536 /*dont check min - this is every frame*/ && !(state == "shooting" || state == "shootIdle");
		meleeConditions = PlayerOnSight() && xDist <= 384;

		Velocity = velocity;
		MoveAndSlide();
		if (Velocity.Y == 0 && velocity.Y > 0) { //to prevent from inifitely (or worse, overflowing) velocity because only Velocity is set to 0 when colliding with ground
            velocity = new Vector2(velocity.X, 0);
        }

		if (state == "running")
		{
			velocity = new Vector2(runSpeed * directionToPlayer, velocity.Y);
		}
	}
	public void Run(int phase)
	{
		switch (phase)
		{
			case 0:
				if (state == "melee" || state == "melee_idle")
				{
					CharSprite.Play("melee_end");
					MeleeTimer.Stop();
				}
				else if (state == "shooting" || state == "shoot_idle")
				{
					CharSprite.Play("shoot_end");
					ShootTimer.Stop();
				} else
				{
					CharSprite.Play("run_start");
				}
				velocity = new Vector2(runSpeed * Mathf.Sign(GlobalPosition.X - player.GlobalPosition.X)*0.5f, velocity.Y);
				state = "running";
				break;
			case 1:
				CharSprite.Play("run");
				velocity = new Vector2(runSpeed * directionToPlayer, velocity.Y);
				break;

		}
		
	}
	//probably should condense the shoot method into this sort of thing as well, but that takes longer than 0 seconds
	public void Melee(int phase)
	{
		switch (phase)
		{
			//enter melee
			case 0:
				state = "melee_start";
				velocity = new Vector2(0, velocity.Y);
				CharSprite.Play("melee_start");
				break;
			//off cooldown
			case 1:
				state = "melee_idle";
				MeleeTimer.threshHold = (float)(rand.NextDouble() * 2 + 1);
				MeleeTimer.Start();
				break;
			case 2:
				state = "melee";
				CharSprite.Play("melee_slash");
				break;
			//every frame
			case 3:
				meleeArea.Monitoring = true;
				break;
			case 4:
				meleeArea.Monitoring = false;
				break;
		}
		
	}
	public void Shoot(int phase)
	{
		switch (phase)
		{
			case 0:
				state = "shoot_idle";
				CharSprite.Play("shoot_start");
				velocity = new Vector2(velocity.X * 0.5f, velocity.Y);
				break;
			case 1:
				//play shoot idle animation
				velocity = new Vector2(0, velocity.Y);
				ShootTimer.Start();
				//fireLine.AddPoint(gunOffset);
				scope = true;
				break;
			case 2:
				state = "shooting";
				CharSprite.Play("shoot_shoot");
				scope = false;
				tempPlayerPos = new Vector2(player.GlobalPosition.X, player.GlobalPosition.Y);
				break;
			case 3:
				fireRay.Enabled = true;
				Vector2 offEnd = new(768, 0); //length of camera
				//Yo, I think fireLine is meant to be the graphical representation, while fireRay is the raycast
				//fireLine.ClearPoints();
				scope = false;

				fireRay.AddException(this);
				fireRay.TargetPosition = tempPlayerPos - this.GlobalPosition - gunOffset;

				fireRay.Position = gunOffset;

				fireRay.ForceRaycastUpdate();
				fireLineVis.ClearPoints();
				fireLineVis.AddPoint(gunOffset);
				if (fireRay.IsColliding()) {
					//to account for sword, shield, and fist collisions
					if (fireRay.GetCollider() == player)
					{
						player.Damage(60, "Soldier Shot"); //i dunno
						fireLineVis.AddPoint(fireRay.GetCollisionPoint());
					}
					else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Sword))
					{
						//reflect if sword moving toward raycast
						if (player.tool.tipVel > 0) { //maybe update it to require a certai speed
							AnimatedSprite2D reflectEffect = (AnimatedSprite2D)player.VFXTemp.Duplicate();
							player.AddToVFXTemp(reflectEffect, player.tool.tipRelPos, new Vector2(7, 7), "effect");
							this.Die();
							return;
						}
						//Reflect back into enemy
						fireLineVis.AddPoint(player.tool.tipGlobalPos - this.GlobalPosition);
						fireLineVis.AddPoint(new Vector2(rand.Next(-24, 25), rand.Next(-24, 25)));
						
					} else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Shield)) {
						player.Heal(50);
					} else if (fireRay.GetCollider() == player.tool && player.tool.Is(PlayerToolUniv.Tool.Shield)) {
						player.tool.StealFire(100, 100);
					}
				}
				 else {
					fireLineVis.AddPoint(fireRay.TargetPosition);
				}
				
				
				//fireLine.Position = gunOffset;
				//fireLine.AddPoint(fireRay.GetCollisionPoint() - this.GlobalPosition);
				break;
			case 4:
				fireLineVis.ClearPoints();
				fireRay.Enabled = false;
				state = "shoot_idle";
				ShootTimer.Start();
				break;
		}
	}
	private bool CheckToJump(JumpKind jumpConfig) //forward from player, upward from ground (starting at 1 for each)
	{
		if (jumpConfig == enemyBase.JumpKind.Low && CheckToJump(enemyBase.JumpKind.LowCeiling)) {
            return false;
        }
		//resize jump colliders for normal and ceiling
        RectangleShape2D jumpRect = (RectangleShape2D)JumpDetector.Shape;
        Tuple<Vector2, Vector2> temp = JumpColDimensions[enemyBase.JumpKind.Low];
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
	public void Stun(int phase)
	{
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
				state = "melee_idle";
				CharSprite.Play("melee_idle");
				break;
		}
	}
	private void Jump(int phase)
	{
		switch (phase)
		{
			case 0:
				state = "jumping";
				CharSprite.Play("jumping");
				velocity = new Vector2(velocity.X, velocity.Y - jumpVel);
				break;
			case 1:
				state = "running";
				CharSprite.Play("run");
				velocity = new Vector2(velocity.X, 0);
				break;
		}
		
	}
	private bool PlayerOnSight()
	{
		r.TargetPosition = player.GlobalPosition - this.GlobalPosition;

		return r.IsColliding() && r.GetCollider() == player; //short cicuit AND because latter term throws an error if first time is false, so dont consider it if so
		//(r.GetCollider() == player || ((Node)r.GetCollider()).GetParent() == player)
	}
	public override void _on_char_sprite_animation_finished() //WARNING: DOES NOT CALL IF AN ANIMATION IS LOOPING
	{
		if (state == "dead")
		{
			this.QueueFree();
		}
	}
    public override void Die()
    {
		GD.Print("starting death sequence");
		//make sure no errors can occur when it is eventually deleted upon animation finish
		meleeArea.SetDeferred("monitorable", false);
		meleeArea.SetDeferred("monitoring", false);
		ShootTimer.Stop();
		MeleeTimer.Stop();
		StunTimer.Stop();
		//fireLine.ClearPoints();
		fireRay.Enabled = false;
		CharSprite.Play("perish");
		state = "dead";
		f.enemies.Remove(this);
    }
	private void _on_melee_area_area_entered(Area2D area)
    {
        //don't do any logic here - its handled in PlayerToolUniv.cs
    }
}


