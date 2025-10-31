using Godot;
using System;

public partial class singlePlayerBase : playerInherited
{
    public FendOffMatch f;
    [Export]
    public Area2D damageArea;
    [Export]
    public AnimatedSprite2D VFXTemp;
    public void AddToVFXTemp(AnimatedSprite2D anim, Vector2 pos, Vector2 scale, string animName) {
        VFXTemp.AddChild(anim);
        anim.Position = pos;
        anim.Scale = scale;
        anim.Play(animName);
        anim.AnimationFinished += () => {effects.Remove(anim); anim.QueueFree();}; //should delete the animation after animaiton finish
        GD.Print("New anim added:" + anim.ToString());
    }
    public override void _Ready()
    {
        base.BaseReady();
        f = (FendOffMatch)GetTree().Root.GetNode("World");
        Random r = new();

        trc.TimerFinished += RollCooldownFinished;

        bodySpr.Frame = r.Next(0, 4); //TODO: allow customization, or literally anything better than random selection

        shaking = false;

        tool = (PlayerToolUniv)GetNode("PlayerTools");
        tool.AfterPlayerReady();

        cam.LimitRight = (int)f.CBLR.GlobalPosition.X;
        cam.LimitBottom = (int)f.CBLR.GlobalPosition.Y;

        health = 3000;
        startingHealth = health;
        isAuthority = true;
        cam.Zoom = new Vector2(0.5f, 0.5f);
    }

    public override void _Process(double delta)
    {
        if (!dead) //NOTE: this is checked here, so don't do it in PlayerProcess() like a dum dum
        {
            PlayerProcess(delta);
        }
        if (!Input.IsActionPressed("ui_down") && !stunned)
        {
            if (IsOnFloor())
            {
                bodySprTransform = new Transform2D(new Vector2(2, 0), new Vector2(-0.25f * Mathf.Sign(Velocity.X), 2), Vector2.Zero);
            }
            else
            {
                bodySprTransform = new Transform2D(new Vector2(2, -0.5f * Mathf.Sign(Velocity.X) * VelocityWithEffVel.Y / (JumpVelocity)), new Vector2(-0.25f * Mathf.Sign(VelocityWithEffVel.X), 2), Vector2.Zero);
            }
        }
        bodySpr.Transform = bodySprTransform;
        if (Input.IsActionJustPressed("zoomOut")) {
            defaultCamZoom = 0.5f;
        } else if (Input.IsActionJustReleased("zoomOut")) {
            defaultCamZoom = 1;
        }
    }
    private void PlayerProcess(double delta) //holy crap this thing alone is 200 lines of code. Thank it all for Visual Studio's text collapse feature!
    {
        EntityStartUpdateVelocities();

        //MOVEMENT
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------

        BaseHandleCoyoteTime();

        if (!floorAndWallStatus[2] && EffectVelocity.Y == 0 && !th.active && !tCoyote.active)
        {
            velocity.Y += gravity * (float)delta;
        }

        // Handle Jump.
        //for some ungodly reason ui_"""""accept"""" is space and not enter. I know I can change it but i dont wanna
        //NOTE: this is difficult to test during coyote time, so it may be working anyway (How the hell am I bad at my own game?)
        if (Input.IsActionPressed("ui_accept") && (floorAndWallStatus[0] || tCoyote.active))
        {
            velocity.Y = JumpVelocity;
        }

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        //set LNZD to, well, the last non-zero direction vector
        lastNonZeroDirectionVector = !direction.IsEqualApprox(Vector2.Zero) ? direction : lastNonZeroDirectionVector;
        
        if (direction != Vector2.Zero && !rolling && !stunned)
        {
            velocity.X = direction.X * Speed;
        }
        else if (!rolling)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }

        //Roll

        BaseHandleRoll(direction);

        EntityEndUpdateVelocities((float)delta);
    }
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public override void RollCooldownFinished()
    {
        rollCooldownSpr.TextureOver = EndRollCooldownOver;
    }

    //------------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void _on_damage_area_body_entered(Node2D body)
    {
        float EVL = EffectVelocity.Length();
        float ratio = EVL / BalancedValues.UNIT_EFFECT_VELOCITY;
        if (body == f.tm && !tool.toolPreventingTerrainDamage)
        {
            hitTerrainFirst = true;
            if (ratio > 1)
            {
                //damage from terrain collision
                Damage(ratio * BalancedValues.UNIT_DAMAGE, "Single Player Terrain Collision");
            }
        }
    }

    private void _on_damage_area_body_exited(Node2D body)
    {
        hitTerrainFirst = false;
    }

}