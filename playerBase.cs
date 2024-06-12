using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class playerBase : CharacterBody2D
{
	//'cause you can't make field virtual without getters and setters,
	//and getters and setters crash godot...
	public long id = 1;
	public bool isAuthority;

	public match m;
	private const float effectDecceleration = 0.99f;
	private Vector2 pastPosition;
	public const float Speed = 600.0f;
	private Vector2 defaultScale = new(1, 1);
	public const float JumpVelocity = -1350.0f;

	public swordBase sword;
	private Shield shield;
	public Fist fist;
	[Export]
	public bool comboActive = false;

	//Get the gravity from the project settings to be synced with RigidBody nodes.

	public const float gravity = 1500;
	
	private const float rollAccel = 10;
	public bool rollingFast = false;
	private bool rolling = false;
	private const float rollMax = 1200;

	private AnimatedSprite2D rollBufferSpr;

	public float startingHealth = 120; 
	public float health = 120;
	public void SetHealths(float max, float begin) {
		if (max == 0) {
			startingHealth = 3600; //because it has so many factors
		} else {
			startingHealth = max;
		}

		//if beginning health is in an accaptable range
		if (begin <= max && begin != 0) {
			health = begin;
		} else {
			health = max;
		}
	}
	public string name = "goober (TEST)"; //add whatever the username is in the player files
	// Called when the node enters the scene tree for the first time.
	public Vector2 effectVelocity;
	[Export]
	public bool comboHit = false;
	[Export]
	public float preComboDamage = 0;
	
	public float preComboDamageRequirement;
	private Control comboBarMask;
	private Sprite2D comboSlider;
	//in seconds, the time window you have for each successive combo hit
	private float[] comboTimes = { 1.5f, 1f, 0.75f, 0.5f };
	Timer tc;
	Timer trc;
	[Export]
	private int currentComboIndex = 0;
	private playerBase comboTarget;
	private Fist comboFist;
	
	private HSlider preComboSlider;
	private Label comboLabel;
	private AudioStreamPlayer a;

	//because  IsOnWall() gets called multiple times, so only bounce (reverse X vel) on
	private bool bouncing = false;

	//achievements
	public bool hasUsedShield = false;
	public bool hasUsedFist = false;
	public bool hasUsedCoin = false;

	private CollisionShape2D bodyCol;
	private CollisionShape2D damageCol;
	private AnimatedSprite2D bodySpr;
	public Transform2D bodySprTransform;
	public Face faceSpr;

	private TextureProgressBar rollCooldownSpr;

	public Camera2D cam;

	MultiplayerSynchronizer ms;
	public bool stunned;
	[Export]
	public bool frozen = false;
	[Export]
	public bool shaking = false;
	public Timer ts;
	public coinScript coin;

	public bool dead = false;
	public Vector2 StartingScale;
	public override void _Ready()
	{
		ms = (MultiplayerSynchronizer)GetNode("MultSync");
		ms.SetMultiplayerAuthority((int)id);

		isAuthority = this.id == Multiplayer.GetUniqueId();

		sword = (swordBase)GetNode("Sword");
		shield = (Shield)GetNode("Shield");
		fist = (Fist)GetNode("Fist");

        cam = (Camera2D)GetNode("PlayerCam");
		tc = new Timer(0, false, this);
		trc = new Timer(2.5f, true, this);
		trc.TimerFinished += RollCooldownFinished;
		ts = new Timer(1.5f, false, this);
		
		a = (AudioStreamPlayer)GetNode("SFXMaker");
		m = (match)this.GetParent();

		m.players.Add(this);

		
		rollBufferSpr = (AnimatedSprite2D)GetNode("RollBuffer");
		rollBufferSpr.Visible = false;
		
		effectVelocity = Vector2.Zero;
		pastPosition = Position;

		comboBarMask = (Control)GetNode("Combo/DarkBarMask");
		comboSlider = (Sprite2D)GetNode("Combo/Slider");
		preComboDamageRequirement = Mathf.Round(startingHealth / 10);
		((Node2D)GetNode("Combo")).Visible = false;
		preComboSlider = (HSlider)GetNode("PreComboSlider");
		preComboSlider.MaxValue = preComboDamageRequirement;
		comboLabel = (Label)GetNode("ComboLabel");
		comboLabel.Text = "";

		tc.TimerFinished += EndCombo;
		ts.TimerFinished += StopDisability;

		bodyCol = (CollisionShape2D)GetNode("BodyCol");
		damageCol = (CollisionShape2D)GetNode("DamageArea/DamageCol");
		bodySpr = (AnimatedSprite2D)GetNode("BodySprite");
		bodySpr.Scale = new Vector2(2, 2);
		bodySprTransform = new Transform2D(new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, 0));
		faceSpr = (Face)GetNode("FaceSprite");

		rollCooldownSpr = (TextureProgressBar)GetNode("GUICanvas/CooldownSprites").GetNode("RollCooldownSprite");

		bodySpr.Frame = m.players.IndexOf(this);

        GD.Print("Player number " + (m.players.IndexOf(this) + 1) + " is authority (true/false) " + isAuthority + ", as player id is " + this.id + " and multiplayer id is " + Multiplayer.GetUniqueId());

        if (!isAuthority)
		{
			((TextureProgressBar)GetNode("GUICanvas/CooldownSprites").GetNode("ShieldCooldownSprite")).Visible = false;
            ((TextureProgressBar)GetNode("GUICanvas/CooldownSprites").GetNode("PunchCooldownSprite")).Visible = false;
			rollCooldownSpr.Visible = false;

			preComboSlider.Visible = false;

			((Sprite2D)GetNode("Caret")).Visible = false;

			((Control)GetNode("SwingMeterSprites")).Visible = false;
        } else {
			rollCooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldown.png");
		}

		shaking = false;

		this.Velocity = Vector2.Zero;
		this.Position = new Vector2(this.Position.X, this.Position.Y - 64);
		this.StartingScale = new Vector2(Scale.X, Scale.Y);

		//NOTE: make an interface or something for this, although I have no idea where to put it. Maybe a separate script file?
		sword.AfterReady();
		shield.AfterReady();
		fist.AfterReady();
	}

	public void StartDisability() 
	{
       
        Random r = new Random();

        ts.Start();
        stunned = true;

        faceSpr.Play("stun");
        faceSpr.Frame = r.Next(0, 4);

		bodySpr.Frame = 4;

		if (rolling) {
			StopRolling();
		}
		
    }

	public void StopDisability() {
		stunned = false;
		faceSpr.Play("idle");
		faceSpr.Frame = faceSpr.faceSpriteIndex;

        bodySpr.Frame = m.players.IndexOf(this);
    }
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	//DON'T use this. use playerProcess instead, because this is called IN ADDITION to any children's _Process function, such that any of them calling this will run it twice
	public override void _Process(double delta)
	{
		if (isAuthority && !dead) //NOTE: this is checked here, so don't do it in PlayerProcess()
		{
			PlayerProcess(delta);
		}
		bodySpr.Transform = bodySprTransform;
		if (Input.IsActionJustPressed("genericTest1"))
		{
			faceSpr.Visible = false;
			bodySpr.Play("slash");
		}
		if (Input.IsActionJustPressed("genericTest2"))
		{
			sword.Lock();
		}
	}
	protected void PlayerProcess(double delta) 
	{

        if (preComboDamage < preComboDamageRequirement && !comboActive)
        {
            preComboSlider.Value = preComboDamage;
        }
        else
        {
            preComboSlider.Value = preComboDamageRequirement;
        }

        if (comboActive)
        {
            Combo();
        }

        rollCooldownSpr.Value = Mathf.RoundToInt(100 * trc.timerVal / trc.threshHold);

		Vector2 velocity = Velocity;

		if (!Input.IsActionPressed("ui_down") && !stunned) {
			if (IsOnFloor())
			{
				bodySprTransform = new Transform2D(new Vector2(2, 0), new Vector2(-0.25f * Mathf.Sign(velocity.X), 2), Vector2.Zero);
			} else
			{
				bodySprTransform = new Transform2D(new Vector2(2, -0.5f * Mathf.Sign(velocity.X) * (velocity.Y+effectVelocity.Y) / (JumpVelocity)), new Vector2(-0.25f * Mathf.Sign(velocity.X+effectVelocity.X), 2), Vector2.Zero);
			}
		}
		Position = new Vector2(Position.X + effectVelocity.X * (float)GetProcessDeltaTime(), Position.Y + effectVelocity.Y * (float)GetProcessDeltaTime());

		if (!IsOnFloor()) {
			//NOTE: somehow velocity is being called twice, so the * 2 * for EFFECT velocity has to be * 4 *
			effectVelocity.Y += gravity * 4 * (float)delta;
		}

		//If player is stuck on ceiling or floor, cut effectVelocity.Y (longitudal equivalent of below)
		
		if (IsOnCeiling() && effectVelocity.Y < 0)
		{
			
			effectVelocity = new Vector2(effectVelocity.X, 0);

		} 
		if (IsOnFloor() && effectVelocity.Y >= 0) {
			//friction my beloved
			effectVelocity = Vector2.Zero;
		}
		//if the player is stuck on a wall, cut the effectVelocity.X (lateral equivalent of above version)
		if (pastPosition.X == Position.X)
		{
			effectVelocity = new Vector2(0, effectVelocity.Y);

		}
		//if the effectVel is really small, just cut it
		if (effectVelocity.Length() >= 750)
		{
			effectVelocity = new Vector2(effectVelocity.X * effectDecceleration, effectVelocity.Y * effectDecceleration);
		}
		else if (effectVelocity.Length() > 0)
		{
			effectVelocity = Vector2.Zero;
			
		}

		effectVelocity.LimitLength(4800);

		

		//MOVEMENT
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------

        //bounce the player
        if (IsOnWall() && !rolling && !IsOnFloor())
        {
            if (Mathf.Abs(effectVelocity.X) >= 400 && !bouncing)
            {
                effectVelocity = new Vector2(-effectVelocity.X, effectVelocity.Y);

                bouncing = true;
            }
        } else if (bouncing)
        {
            bouncing = false;
        } else if (rolling && rollingFast)
		{
			StartDisability();
		}


        //NOTE: This is because, while gravity is on, velocity fights effectVelocity, as when effectVelocity
        //is cut at say, the ceiling, the stored Y component of velocity will shoot it down 
        if (!IsOnFloor() && effectVelocity.Y == 0)
        {
            velocity.Y += gravity * 2 * (float)delta;
        }
        else if (velocity.Y < 1300 || !shield.t.active)
        {
            velocity = new Vector2(velocity.X, 0);
        } else {
			//bounce off of floor if shield is active
			velocity = new Vector2(velocity.X, velocity.Y * -1);
		}
        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }


        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (direction != Vector2.Zero && !rolling && !stunned)
        {
            velocity.X = direction.X * Speed;
            effectVelocity = new Vector2(0, effectVelocity.Y);
        }
        else if (!rolling)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }


        //Roll
        if (Input.IsActionJustPressed("roll") && !IsOnWall() && trc.done && !stunned)
        {
            sword.active = false;
            shield.active = false;
            fist.pubActive = false;

            rolling = true;
        }
        else if (Input.IsActionPressed("roll") && !IsOnWall() && !ts.active && !stunned)
        {
            if (Mathf.Abs(velocity.X) < rollMax && !rollingFast)
            {
                velocity = new Vector2(velocity.X + direction.X * rollAccel, velocity.Y);
            }
            else if (!rollingFast)
            {
                rollingFast = true;
                rollBufferSpr.Position = new Vector2(96 * direction.X, 0);
                rollBufferSpr.Scale = new Vector2(direction.X * 3, 3);
                rollBufferSpr.Visible = true;
                rollBufferSpr.Play();
            }
            bodySpr.Rotation += (direction.X * Mathf.Abs(velocity.X) / (Mathf.Pi * 32)) * (float)GetProcessDeltaTime(); //so the body and face start rotating the other way when
            faceSpr.Rotation += (direction.X * Mathf.Abs(velocity.X) / (Mathf.Pi * 32)) * (float)GetProcessDeltaTime(); //starting to reverse

        }
        else if (Input.IsActionJustReleased("roll"))
        {
            StopRolling();
        }

		//coin
		if (Input.IsActionJustPressed("coin")) {
			coin.player = this;
			coin.StartCoin();
		}


        //Crouch
        if (Input.IsActionJustPressed("ui_down") && !ts.active && !stunned)
        {
            Vector2 newScale = new(1, 0.5f);
            bodyCol.Scale = newScale;
            damageCol.Scale = newScale;
            bodySpr.Scale = newScale;
            faceSpr.Scale = newScale * 2; //because its scaled upwards in editor
                                          //crouch downwards if travelling downwards, crouch upwards if travelling upwards
            if (Velocity.Y <= 0)
            {
                Position = new Vector2(Position.X, Position.Y - GlobalScale.Y * 0.5f);
            }
            else
            {
                Position = new Vector2(Position.X, Position.Y + GlobalScale.Y * 0.5f);
            }

        }
        if (Input.IsActionJustReleased("ui_down"))
        {
            bodyCol.Scale = defaultScale;
            damageCol.Scale = defaultScale;
            bodySpr.Scale = defaultScale;
            faceSpr.Scale = defaultScale * 2;
        }

        if (!frozen)
        {
            pastPosition = Position;

            Velocity = velocity;
            MoveAndSlide();
        }
    }

	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------

	private void StopRolling() {
		bodySpr.Rotation = 0;
		faceSpr.Rotation = 0;
		sword.active = true;
		shield.active = true;
		fist.pubActive = true;

		rolling = false;
		rollingFast = false;

		rollBufferSpr.Visible = false;
		rollBufferSpr.Stop();

		trc.Start();
		rollCooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldown.png");
	}

	private void RollCooldownFinished() {
		rollCooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldownDone.png");
	}
	
	public void Damage(Fist f, swordBase sw, playerBase damager, float dmgMult = 1, bool megaHit = false)
	{
		float damageConstant = 0.01f;

		sw.swingVal = 0;
		sw.swingMaxReached = false;
        sw.SMBG.Play("inactive");

        health -= Mathf.Floor( Mathf.Abs( sw.tipVel + sw.swingVal * sw.swingValMultiplier ) * damageConstant * dmgMult);
		if (!damager.comboActive)
		{
			damager.preComboDamage += (Mathf.Abs(sw.tipVel) + sw.swingVal * sw.swingValMultiplier) * damageConstant * dmgMult;
		}

		if (f.active)
		{
			//do extra damage if the fist is out
			health -= Mathf.Abs(sw.tipVel) * 0.5f * damageConstant * dmgMult;
		}
		//SFX
		if (!megaHit) {
			//a.Stream = Global.sfx[11];
			a.Play();
		} else {
			Random r = new();
			//a.Stream = Global.sfx[r.Next(3, 5)];
			a.Play();
		}
		

		if (this.health <= 0)
		{
			dead = true;
            bodySpr.Play("death");
			if (damager.isAuthority)
			{
				damager.Scale = new Vector2(damager.StartingScale.X * Mathf.Sign(damager.GlobalPosition.X - this.GlobalPosition.X), damager.Scale.Y);
				damager.bodySpr.Play("slash");
				damager.faceSpr.Visible = false;
			}

            m.PlayerDies(this);
		}
	}
	public void Damage(int dmg) {
		health -= dmg;
	}

	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------


	public void StartCombo(playerBase target, Fist targetFist)
	{
		if (isAuthority) {
			//do visual fx, like zoom in and whatnot
			tc.Start(comboTimes[0]);
			currentComboIndex = 0;
			comboActive = true;
			comboTarget = target;
			comboFist = targetFist;

			sword.angVel *= 0.25f;
		}
		
		((Node2D)GetNode("Combo")).Visible = true;
		comboLabel.Text = "Hits: 0/4";

		shaking = true;
		frozen = true;
		target.shaking = true;
		target.frozen = true;
		
	}
	protected void Combo()
	{
		if (comboHit && isAuthority)
		{
			//If this is right at the end. Its meant to be 90/96 because that is where the end sprite is
			if (tc.timerVal > 0.9f * comboTimes[currentComboIndex])
			{
				comboTarget.Damage(comboFist, this.sword, this, 6);
			} else
			{
				//if still in the time window but not at the end of it like above
				comboTarget.Damage(comboFist, this.sword, this, 3);
			}

			comboHit = false;
			currentComboIndex++;

			if (currentComboIndex == comboTimes.Length)
			{
				EndCombo();
				return;
			}
		} else if (comboHit) {
			tc.Start(comboTimes[currentComboIndex]);
			comboLabel.Text = "Hits: " + currentComboIndex + "/4";
		}
		//NOTE: it would normally scale down to the left, but i rotated it 180 so it instead scales to the right
		comboBarMask.Size = new Vector2((1-(tc.timerVal / comboTimes[currentComboIndex]))*96, comboBarMask.Size.Y);
		comboSlider.Position = new Vector2(tc.timerVal / comboTimes[currentComboIndex] * 96 - 48, comboSlider.Position.Y);
		//if it goes past
		if (comboBarMask.Size.X < 0)
		{
			comboBarMask.Size = new Vector2(0, comboBarMask.Size.Y);
		}
		
	}
	protected void EndCombo ()
	{
		tc.Stop();

		comboActive = false;

		comboBarMask.Size = new Vector2(96, comboBarMask.Size.Y);
		comboSlider.Position = new Vector2(-48, comboSlider.Position.Y);

		((Node2D)GetNode("Combo")).Visible = false;
		preComboDamage = 0;

		comboLabel.Text = "";

		shaking = false;
		frozen = false;
		comboTarget.shaking = false;
		comboTarget.frozen = false;

		comboTarget = null;

	}

	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------

	protected void SwordDetect(swordBase sw, Fist playerFist, playerBase otherPlayer) /*otherPlayer is the one DOING the damage*/{
		if (sw != this.sword) {
			Damage(playerFist, sw, otherPlayer); //so you can hit youself without dealing max damage for the boost
		}
		
		if ((Mathf.Abs(sw.angVel) >= swordBase.max / 2) && (otherPlayer.preComboDamage >= otherPlayer.preComboDamageRequirement) && !otherPlayer.comboActive)
		{
			otherPlayer.StartCombo(this, playerFist);
		} else if (otherPlayer.comboActive)
		{
			otherPlayer.comboHit = true;
		}

		//knockback (multiplying by the tipVel should give a sort of accelerating affect as tipVel2d increases)
		if (sw.swingMaxReached) {
			if (sw != this.sword)
			{
				StartDisability();
			}
			
			float angle = sw.tipVelocity2d.Angle();

			Vector2 newEffVel = new Vector2(-(sw.tipVelocity2d.X + Mathf.Cos(angle)*swordBase.swingLaunchMult), -(sw.tipVelocity2d.Y + Mathf.Sin(angle)*swordBase.swingLaunchMult));
			if (sw == this.sword) {
				this.effectVelocity = newEffVel / 3; //so you can launch yourself :D
			}
			this.effectVelocity = newEffVel;
		}
	}
	public void Heal(float heal) {
		if (health + heal <= startingHealth) {
			health += heal;
		} else {
			health = startingHealth;
		}
	}

	public void SetCameraAsMain(long ID)
	{
		if (ID == this.id)
		{
			cam.MakeCurrent();
		}
		
	}
	
	//----------------------------------------------------------------------------------------------------
	//----------------------------------------------------------------------------------------------------
	//----------------------------------------------------------------------------------------------------
	private void _on_damage_area_area_entered(Area2D area)
	{
		for (int i = 0; i < m.players.Count; i++) {
			if (area == (Area2D)m.players[i].GetNode("Sword")) 
			{
				swordBase otherSword = (swordBase)m.players[i].GetNode("Sword");
				SwordDetect(otherSword, fist, m.players[i]);
			}
			if (area == (Area2D)m.players[i].GetNode("RollArea") && m.players[i].rolling) {
				Damage(Mathf.RoundToInt(startingHealth / 20));
			}
		}
	}
	private void _on_body_sprite_animation_finished()
	{
		if (dead)
		{
			this.Visible = false;
			sword.swordCol.Disabled = true;
			this.damageCol.Disabled = true;
			this.bodyCol.Disabled = true;
		} else //doing a megahit
		{
			if (isAuthority)
			{
				faceSpr.Visible = true;
				Scale = StartingScale;
				bodySpr.Play("default");
			}
			
			bodySpr.Frame = m.players.IndexOf(this);
		}
		
	}
	
}



