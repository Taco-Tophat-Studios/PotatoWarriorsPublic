using Godot;
using System;

public partial class Fist : Area2D, IPlayerTools
{
    private const float punchDistance = 175;
    private const float punchTime = 0.15f;
    Timer t;
    private const float punchSpeed = punchDistance/punchTime;
    public float distance; //no need to export, do position  and angle instead
    [Export]
    public bool active = false; //IS punchING

    public bool pubActive = true; //CAN punch
    private CollisionShape2D fistCol;
    private Sprite2D fistSpr;
    private float angle;
    private playerBase potato;
    private swordBase sword;
    private TextureProgressBar cooldownSpr;

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        potato = (playerBase)this.GetParent();
       

		/*Ah, but the speed is pixels/second, and the delta time is in seconds, so I'll just have to multiply
		this by GetProcessDeltaTime() until it equals 100! The perfect plan...*/

		fistCol = (CollisionShape2D)GetNode("FistCollider");
		fistSpr = (Sprite2D)GetNode("FistSprite");
        cooldownSpr = (TextureProgressBar)GetNode("../GUICanvas/CooldownSprites").GetNode("PunchCooldownSprite");
		
		fistCol.Disabled = true;
		fistSpr.Visible = false;

		distance = 0;

        sword = (swordBase)GetNode("../Sword");
	}

    public void AfterReady() {
        if (potato.isAuthority) {
            t = new Timer(1.5f, true, this);
            t.TimerFinished += PunchCooldownDone;
            cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/PunchCooldown.png");
            cooldownSpr.Visible = true;
        } else
        {
            cooldownSpr.Visible = false;
        }
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (potato.isAuthority) {
            cooldownSpr.Value = Mathf.RoundToInt(100*t.timerVal / t.threshHold);
        }
        

        //in case the player moves while the animation occurs
        Rotation = Mathf.Atan2(Position.Y, Position.X);

        if (potato.isAuthority) {
            //activate
            if (Input.IsActionJustPressed("punch") && t.done && pubActive)
            {
                active = true;
                fistCol.Disabled = false;
                fistSpr.Visible = true;
                //Note: startMousePos is LOCAL
                angle = Mathf.Atan2(GetLocalMousePosition().Y, GetLocalMousePosition().X);
                sword.Visible = false;
                sword.active = false;
                
                if (this.GlobalScale != new Vector2(1, 1))
                {
                    this.GlobalScale = new Vector2(1, 1);
                }
            }
            
            //update pos
            if (active && distance < punchDistance)
            {
                //set position based on distance
                GlobalPosition = potato.GlobalPosition + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
                //set distance
                distance += punchSpeed * (float)GetProcessDeltaTime();

                //deactivate
            }
            else if (active && distance >= punchDistance)
            {
                ResetPunch();
            }
        }
        
    }
    private void PunchCooldownDone() {
        cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/PunchCooldownDone.png");
    }

    public void ResetPunch()
    {
        fistCol.Disabled = true;
        fistSpr.Visible = false;

        Position = new Vector2(0, 0);
        angle = 0;
        Rotation = 0;

        active = false;
        distance = 0;

        t.Start();
        cooldownSpr.TextureOver = (Texture2D)GD.Load("res://sprites/GUI/CooldownIcons/PunchCooldown.png");

        sword.Visible = true;
        sword.active = true;
    }
}
