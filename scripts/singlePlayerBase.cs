using Godot;
using System;

public partial class singlePlayerBase : CharacterBody2D
{
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
		//if max not set (???)
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

	//because IsOnWall() gets called multiple times, so only bounce (reverse X vel) on
	private bool bouncing = false;

	private CollisionShape2D bodyCol;
	private CollisionShape2D damageCol;
	private AnimatedSprite2D bodySpr;
	public Transform2D bodySprTransform;
	public Face faceSpr;

	private TextureProgressBar rollCooldownSpr;

	public Camera2D cam;

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
        sword = (swordBase)GetNode("Sword");
		shield = (Shield)GetNode("Shield");
		fist = (Fist)GetNode("Fist");

        cam = (Camera2D)GetNode("PlayerCam");
		tc = new Timer(0, false, this);
		trc = new Timer(2.5f, true, this);
		trc.TimerFinished += RollCooldownFinished;
		ts = new Timer(1.5f, false, this);
		
		a = (AudioStreamPlayer)GetNode("SFXMaker");

		
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

		bodySpr.Frame = 1;
        
		rollCooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldown.png");
		

		shaking = false;

		this.Velocity = Vector2.Zero;
		this.Position = new Vector2(this.Position.X, this.Position.Y - 64);
		this.StartingScale = new Vector2(Scale.X, Scale.Y);
		
		sword.AfterReady();
		shield.AfterReady();
		fist.AfterReady();
    }

    // Get the gravity from the project settings to be synced with RigidBody nodes.

    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	public void RollCooldownFinished() {

	}
	public void StartCombo() {

	}
	public void Combo() {
		
	}

	public void EndCombo() {

	}
		public void StartDisability() {

	}
	public void StopDisability() {

	}
}
