using Godot;
using System;
using System.Collections.Generic;

public partial class playerInherited : EntityBase
{
    //MDICC = Must Do In Child Class
    protected WorldBase w;
    public bool isAuthority = true;
    public int playerIndex = 0;
    public int faceIndex = 0;
    public int[] toolIndices;
    public PlayerToolUniv tool;
    public bool hitTerrainFirst = false; //for the case of the player and the tool hitting terrain at the same time (and for fist's "pounding")
    protected const float effectDecceleration = 0.99f;
    public const float Speed = 1000.0f;
    protected Vector2 defaultScale = new(1, 1);
    public const float JumpVelocity = -1750.0f;
    public Area2D DamageableArea;
    protected const float rollAccel = 50;
    public bool rollingFast = false;
    public bool rollingVertically = false;
    public bool rolling = false;
    public bool rollStopper = false; //used to stop roll for external stuff
    public const float rollMax = 3200;
    protected AnimatedSprite2D rollBufferSpr;
    public float startingHealth = 3600;
    protected AudioStreamPlayer a;
    public CollisionShape2D bodyCol;
    public CollisionShape2D damageCol;
    protected AnimatedSprite2D bodySpr;
    public Transform2D bodySprTransform;
    public Face faceSpr;
    public List<AnimatedSprite2D> effects = new();
    protected TextureProgressBar rollCooldownSpr;
    protected Texture2D StartRollCooldownOver;
    protected Texture2D EndRollCooldownOver;
    protected Timer trc; //timer roll cooldown
    public Vector2 lastNonZeroDirectionVector = Vector2.Zero;
    public Timer th; //timer hang
    protected Timer tCoyote; //coyote time
    public Camera cam;
    public Vector2 lastCameraGlobalPosition; //WARNING: used exclusively by PlayerToolUniv, so it should not be delta'd in the player classes
    public float lastCameraGlobalRotation = 0;
    public bool shaking = false;
    public bool stunned;
    public Timer ts;
    public Vector2 StartingScale;
    [Export]
    public AnimatedSprite2D ShieldSlideSprite;
    [Export]
    public AnimatedSprite2D GeneralEffectSprite;
    [Export]
    public float defaultCamZoom = 1.5f;
    [Export]
    public AnimatedSprite2D JetpackSprite; //justification for moving about in space station
    private void PlayHealedEffect(float _)
    {
        GeneralEffectSprite.Position = new Vector2(tool.Position.X, tool.Position.Y);
        GeneralEffectSprite.Play("Heal");
    }
    private void PlayDamagedEffect(float _)
    {
        GeneralEffectSprite.Position = new Vector2(tool.Position.X, tool.Position.Y);
        GeneralEffectSprite.Play("Hurt");
    }
    // Called when the node enters the scene tree for the first time.
    public void BaseReady()
	{
        w = (WorldBase)GetTree().Root.GetNode("World");

        cam = (Camera)GetNode("CamPivotCenter/PlayerCam");       
        ts = new Timer(1.5f, false, this);
        a = (AudioStreamPlayer)GetNode("SFXMaker");

        trc = new Timer(0.66f, true, this);
        th = new Timer(0.75f, false, this); //WARNING: it doesnt seem like it might, but because this is inherited
        //the "this" for constructing timers may not reference an object in game (although I guess inheritance
        //just makes the code available, still in reference to the child)
        //trc.TimerFinished += RollCooldownFinished; MDICC
        tCoyote = new Timer(0.1f, false, this, false); //NOTE: times this low may cause issues with lag or low framerates

        rollBufferSpr = (AnimatedSprite2D)GetNode("RollBuffer");
        rollBufferSpr.Visible = false;
        EndRollCooldownOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldownDone.png");
        StartRollCooldownOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/RollCooldown.png");

        EffectVelocity = Vector2.Zero;
        pastPosition = Position;

        bodyCol = (CollisionShape2D)GetNode("BodyCol");
        damageCol = (CollisionShape2D)GetNode("DamageArea/DamageCol");
        bodySpr = (AnimatedSprite2D)GetNode("BodySprite");
        bodySpr.Scale = new Vector2(2, 2);
        bodySprTransform = new Transform2D(new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, 0));
        faceSpr = (Face)GetNode("FaceSprite");
        faceSpr.Frame = faceIndex;

        rollCooldownSpr = (TextureProgressBar)GetNode("GUICanvas/CooldownSprites").GetNode("RollCooldownSprite");

        //shaking = false; MDICC

        this.Velocity = Vector2.Zero;
        this.Position = new Vector2(this.Position.X, this.Position.Y - 64);
        this.StartingScale = new Vector2(Scale.X, Scale.Y);

        ShieldSlideSprite.Visible = false;
        ShieldSlideSprite.Stop();

        this.Damaged += PlayDamagedEffect;
        this.Healed += PlayHealedEffect;

        tool = (PlayerToolUniv)GetNode("PlayerTools");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    protected void BaseHandleRoll(Vector2 dir, bool auth = true)
    {
        //Roll
        rollCooldownSpr.Value = trc.timerVal / trc.threshHold * 100; //even if not auth, it isnt visible anyway
        //me when 5 quintivigillion short-circuit ands:
        if (auth && !rolling && Input.IsActionPressed("roll") && dir != Vector2.Zero && trc.done && !IsOnWall() && !stunned) //all the ands in the world
        {
            tool.SetActive(false);

            rolling = true;
        }
        else if (rolling && Input.IsActionPressed("roll") && !trc.active && !stunned && !rollStopper)
        {
            if (Velocity.X != 0)
            {
                HandleRollHV(Vector2.Right, Vector2.Down, auth);

            } /*else if (Velocity.Y < 0 && floorAndWallStatus[1] && (!rollingFast || rollingVertically)) {
                //roll vertically
                //WARNING: up is (0, -1), although this will probably work better than (0, 1);
                HandleRollHV(Vector2.Down, Vector2.Right);
            }*/
        }
        else if (rolling || rollStopper)
        {
            bodySpr.Rotation = 0;
            faceSpr.Rotation = 0;
            tool.SetActive(true);

            rolling = false;
            rollingFast = false;
            rollingVertically = false;
            rollBufferSpr.Visible = false;
            rollBufferSpr.Stop();

            trc.Start();
            rollCooldownSpr.TextureOver = StartRollCooldownOver;

            rollStopper = false;
        }
    }
    

    //precond: dir is either {1, 0} or {0, 1} (same with dirOther, just the other than what dir is)
    private void HandleRollHV(Vector2 dir, Vector2 dirOther, bool a) {
        float vel = velocity.Dot(dir);
        float dS = dir.X == 0 ? 1 : lastNonZeroDirectionVector.Dot(dir); //for "directional sign"
        rollingVertically = dir.Equals(Vector2.Up);

        if (a && Mathf.Abs(vel) < rollMax && !rollingFast)
        {
            velocity += dS * rollAccel * dir; //keeps velocity y or x constant depending on roll direction
            velocity += new Vector2(0, dir.Y*gravity*(float)GetProcessDeltaTime()); //counterract gravity if rolling up
        }
        else if (!rollingFast)
        {
            rollingFast = true;
            rollBufferSpr.Position = 96 * dS * dir;
            //rollBufferSpr.Scale = new Vector2(dS * 3, 3);
            rollBufferSpr.Scale = dS * 3 * dir + 3 * dirOther;
            rollBufferSpr.Rotation = -(dir.Y *Mathf.Pi / 2);
            rollBufferSpr.Visible = true;
            rollBufferSpr.Play();
        }
        float rollAdder = dS * Mathf.Abs(vel) / (Mathf.Pi * 32) * (float)GetProcessDeltaTime();
        bodySpr.Rotation += rollAdder * dir.X; //so the body and face start rotating the other way when
        faceSpr.Rotation += rollAdder * dir.X; //starting to reverse
    }   

    protected void BaseHandleCoyoteTime() {
        //jeez
        if (!floorAndWallStatus[0] && wasOnFloorLastFrame && velocity.Y >= 0 && /*!floorAndWallStatus[2] && */!tCoyote.active) {
            tCoyote.Start();
        } else if (Velocity.Y < 0) {
            tCoyote.Stop();
        }
    }
    public virtual void RollCooldownFinished() {}

    //TODO: move this to entityBase
    protected void BaseHandleTerrainNorms(CollisionShape2D col)
    {
        Vector2I[] intersects = w.FindIntersectingTiles(col);
        currentTerrainNorms = w.FindNormalsOfTiles(intersects);
    }
    public override void Die()
    {
        GD.Print("D I E");
        bodySpr.Play("death");
        bodySpr.SpeedScale = 1;
    }
}
