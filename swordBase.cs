using Godot;
using System;

public partial class swordBase : Area2D, IPlayerTools
{
    public int swordNum;
    protected match m;
    [Export]
    public bool active = true;
    public Vector2 tipRelPos;
    public float angle;
    [Export]
    public float angVel;
    public float lastAngVel = 0;
    protected float lAVSpeed;
    protected const float lAVTime = 0.15f;
    protected const float angAcc = 1000f;
    public int stage;
    protected Vector2 mousePos;

    public bool swingMaxReached = false;
    public float swingValMultiplier = 0.1f;
    [Export]
    public float swingVal;
    private const float swingValMax = 3500;
    [Export]
    public const float swingLaunchMult = 200;

    protected Node2D tip;
    public Vector2 tipPos;
    [Export]
    public Vector2 tipVelocity2d;
    [Export]
    public float tipVel;

    protected AnimatedSprite2D overlay;

    public const float max = 1280; //1024 * 1.25

    [Export]
    public Vector2 velocity;
    private Vector2 lastPos;

    const float range = 256;

    protected playerBase player;
    public bool authority = false;
    protected AudioStreamPlayer a;
    Timer tl;
    public bool locked = false;
    public CollisionShape2D swordCol;

    public AnimatedSprite2D SMBG;
    public AnimatedSprite2D SMF;
    public AnimatedSprite2D swordSprite;
    [Export]
    public int swordSpriteIndex;
    private const float MaxVel = 15f;
    private Vector2 lastVelocity;
    private Vector2 temp;
    
    public override void _Ready()
	{ 
        player = (playerBase)GetParent();

        tl = new Timer(1.5f, false, this);
        
        swordNum = 1;
        m = (match)GetNode("../../../World");
        m.swordAreas.Add((Area2D)GetNode("../Sword"));

        a = (AudioStreamPlayer)GetNode("SFXDoer");
        overlay = (AnimatedSprite2D)GetNode("Overlay1");

        tip = (Node2D)GetNode("Tip");
        stage = 0;

        tipPos = tip.GlobalPosition - this.GlobalPosition;
        
        swordSprite = (AnimatedSprite2D)GetNode("SwordSprite");

        UpdateVelocity();
        swordCol = (CollisionShape2D)GetNode("NormalCollider");

        tl.TimerFinished += Unlock;

        SMBG = (AnimatedSprite2D)GetNode("../SwingMeterSprites/SwingMeterBG");
        SMF = (AnimatedSprite2D)GetNode("../SwingMeterSprites/SwingMeterF");
    }
    public void AfterReady() {
        if (player.isAuthority) {
            //swordSpriteIndex = (int)Global.GetLocalPlayerProperty("swordIndex");
        }
        swordSprite.Frame = swordSpriteIndex;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (player.isAuthority && m.players.IndexOf(player) == 0)
        {
            
            UpdateVelocity();

            if ((velocity * (float)GetProcessDeltaTime()).Length() > MaxVel)
            {
                temp = GlobalPosition + velocity.Normalized() * MaxVel;
                GlobalPosition = new Vector2(temp.X, temp.Y);
            } else
            {
                GlobalPosition += velocity * (float)GetProcessDeltaTime();
            }


            KeepSwordInRange();

            //explanation: to make the sword snappier, making it go faster in either direction is normal,
            //but reversing it makes it switch directions almost instantly
            if (Input.IsActionJustPressed("rotSwordLeft") && angVel > 0 && active || Input.IsActionJustPressed("rotSwordRight") && angVel < 0 && active)
            {
                lastAngVel = angVel;
                lAVSpeed = 2 * Mathf.Abs(lastAngVel) / lAVTime;
            }
            if (Input.IsActionPressed("rotSwordLeft") && active)
            {
                if (lastAngVel > 0 && angVel > -1 * lastAngVel)
                {
                    angVel -= lAVSpeed * (float)GetProcessDeltaTime();
                }
                else
                {
                    angVel -= angAcc * (float)GetProcessDeltaTime();
                }
            }
            if (Input.IsActionPressed("rotSwordRight") && active)
            {
                if (lastAngVel < 0 && angVel < -1 * lastAngVel)
                {
                    angVel += lAVSpeed * (float)GetProcessDeltaTime();
                }
                else
                {
                    angVel += angAcc * (float)GetProcessDeltaTime();
                }
            }
        

            if (this.GlobalScale != new Vector2(1, 1))
            {
                this.GlobalScale = new Vector2(1, 1);
            }
            //correct angVel
            if (angVel > max)
            {
                angVel = max;
            }
            else if (angVel < -max)
            {
                angVel = -max;
            }

            //set angle
            angle += angVel * (float)GetProcessDeltaTime();
            RotationDegrees = angle;
        }

            

        //set colliders and overlay
        if (MathF.Abs(angVel) == max)
        {
            overlay.Visible = true;
            overlay.Play();

            overlay.Scale = new Vector2(1, angVel / MathF.Abs(angVel));

            if (Mathf.Sign(angVel) == 1)
            {
                overlay.Position = new Vector2(32, -32);
            }
            else
            {
                overlay.Position = new Vector2(32, 32);
            }

        }
        else
        {
            overlay.Visible = false;
            overlay.Stop();
        }

        if (player.isAuthority) {
            //tip stuff
            tipVelocity2d = (tipPos - tip.GlobalPosition) / (float)GetProcessDeltaTime();

            tipVel = tipVelocity2d.Length();

            swingVal += (Mathf.Abs(tipVel) - 1280) * (float)GetProcessDeltaTime();
            if (swingVal < 0)
            {
                swingVal = 0;
            } else if (swingVal > swingValMax)
            {
                swingVal = swingValMax;
            }
        }
        
        SMF.Frame = Mathf.RoundToInt(31*(swingVal / swingValMax));

        if (swingVal >= swingValMax)
        {
            if (!swingMaxReached)
            {
                swingMaxReached = true;
                SMBG.Play("active");
            }
            
            if (swingVal > swingValMax)
            {
                swingVal = swingValMax;
            }
            
        } else if (swingMaxReached)
        {
            swingMaxReached = false;
            SMBG.Play("inactive");
        }


        tipPos = tip.GlobalPosition;
        tipRelPos = tip.GlobalPosition - this.GlobalPosition;

        lastVelocity = velocity;
        lastPos = this.GlobalPosition;

    }
    public void Lock() {
        locked = true;
        swordCol.Disabled = true;
        
        Random r = new Random();
        swordSprite.Play("locked");
        swordSprite.Frame = swordSpriteIndex;

        player.faceSpr.Play("stun");
        player.faceSpr.Frame = 4;

        tl.Start();

        this.swingVal = 0;
        this.swingMaxReached = false;
        this.SMBG.Play("inactive");
    }
    public void Unlock() {
        locked = false;
        swordCol.Disabled = false;
        this.Visible = true;

        swordSprite.Play("sword");
        swordSprite.Frame = swordSpriteIndex;

        player.faceSpr.Play("idle");
        player.faceSpr.Frame = player.faceSpr.faceSpriteIndex;
    }

    private void PlayOverlay(float speedMult)
    {
        overlay.Visible = true;
        overlay.GlobalRotationDegrees = 0;
        overlay.Play();
        overlay.SpeedScale = speedMult;

        overlay.Scale = new Vector2(angVel / MathF.Abs(angVel), 1);
    }
    public void UpdateVelocity () {
        velocity = (GetGlobalMousePosition() - lastPos) / (float)GetProcessDeltaTime();
    }
    public void BounceSwords(swordBase sw)
    {
        float angVel1;
        float angVel2;

        //the angVel's effective component and the added velocity from swinging it
        angVel1 = SwordBounceExpression(sw.angVel, this.tipRelPos, sw.tipRelPos, sw.velocity);

        angVel2 = SwordBounceExpression(this.angVel, sw.tipRelPos, this.tipRelPos, this.velocity);

        

        this.angVel = angVel1;
        sw.angVel = angVel2;
    }
    //f is angVel, v1 and v2 are this and that tPos's, v4 is that vel's
	protected static float SwordBounceExpression(float f, Vector2 v1, Vector2 v2,Vector2 v4) {
		//v1 and v2 are the tip positions rotated instead of the tip velocities, because the latter can be 0
		//and cause a bunch of headaches. Therefore, keep the angle signed, but the vectors which it compares with
		//tangent and normalized, always in positive, so the angle's sign can affect them
		float ret = f * v1.Rotated(Mathf.Pi/2).Normalized().Dot(v2.Rotated(Mathf.Pi/2).Normalized());
        

		//account for the sword's swing velocity
		ret += v1.Rotated(Mathf.Pi/2).Normalized().Dot(v4) / (1.1519f);
			
		return ret;

	}
    protected void PrepareSwordBounce (swordBase s) {
        if (m.players.IndexOf((playerBase)this.GetParent()) < m.players.IndexOf((playerBase)s.GetParent()))
		{
			BounceSwords(s);
            Random r = new();
		    int ri = r.Next(8, 11);
		    //a.Stream = Global.sfx[ri];
		    a.Play();
		}
        
    }
    protected void KeepSwordInRange() {
        if ((this.GlobalPosition - player.GlobalPosition).Length() > range) {
            float ang = (this.GlobalPosition - player.GlobalPosition).Angle();
            Vector2 newPos = new(
                range * Mathf.Cos(ang) + player.GlobalPosition.X, 
                range * Mathf.Sin(ang) + player.GlobalPosition.Y
            );
            if (!m.paused) {
                this.GlobalPosition = newPos;
            }
            //ElasticVelocity = Vector2.Zero;
            
        }
    }
    //GODOT METHODS-----------------------------------------------------------------
    private void _on_area_entered(Area2D area)
    {
        swordBase sword;

        for (int i = 0; i < m.players.Count; i++)
        {
            if (area == (Area2D)m.players[i].GetNode("Sword"))
            {
                sword = (swordBase)m.players[i].GetNode("Sword");
                PrepareSwordBounce(sword);
            }
        }


    }
}
