using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;

public partial class playerBase : playerInherited
{
    //'cause you can't make field virtual without getters and setters,
    //and getters and setters crash godot...
    public long id = 1;
    public match m;
    [Export]
    public int points = 0;
    public void SetHealths(float max = 3600, float begin = 3000)
    {
        //if beginnign health is in an accaptable range
        if (begin <= max && begin != 0)
        {
            health = begin;
        }
        else
        {
            health = max;
        }

        startingHealth = health;
        maxHealth = max;
    }
    public string name = "goober (TEST)"; //add whatever the username is in the player files
    
    [Export]
    public Vector2 JetpackSyncedInfo;
    public MultiplayerSynchronizer ms;

    public string[] hostHeldSEI = new string[4]; //to hold SEI from each player on host
   
    //To only be called by host as both the player and the peer, but because it references EffVelInfo, it has to be called from host player, for each non-host player's client (and thus must be anypeer)
    //cause rpc means to call X method on Y node, so each client has to call it on the host player's node, which wont always have authority
    /// <summary>
    /// Each client calls this on the host player's node to send their SEI to the host. Then, the host will trigger SyncEffVelInfoToEveryone to update everybo0dy's information.
    /// </summary>
    /// <param name="seiData">the JSON string encoded with the player's EntityEffVelInfo object</param>
    /// <param name="playerInd">the index of the player sending the data (0 based)</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncEffVelInfoToHost(string seiData, int playerInd)
    {
        if (playerInd == 0) return; //take care of it later

        //EntityEffVelInfo sei = JsonSerializer.Deserialize<EntityEffVelInfo>(seiData);

        //this gives host node on host peer the SEI for each player (RPC'd to host on player 1 from each client)

        //host is always first player
        if (hostHeldSEI[0] == null || hostHeldSEI[0] == "")
        {
            hostHeldSEI[0] = JsonSerializer.Serialize(GenerateEEVI());
        }
        
        hostHeldSEI[playerInd] = seiData;

        //when the host-held SEI array is filled
        if (!hostHeldSEI.Contains(null))
        {
            //send info for each player to all players
            for (int i = 0; i < GameManager.players.Count; i++)
            {
                //send to self as well
                RpcId(GameManager.players[i].id, "SyncEffVelInfoToEveryone", hostHeldSEI, false, i);
            }
            hostHeldSEI = new string[4]; //reset host-held SEI
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncEffVelInfoToEveryone(string[] seiArrData) {
        EntityEffVelInfo[] seiArr = seiArrData.Select((data, _) => { return JsonSerializer.Deserialize<EntityEffVelInfo>(data); }).ToArray();
        //WARNING: always called from the first physical player in a match!
        
        //when receiving from host, set each SEVI to what is given
        for (int i = 0; i < m.players.Count; i++)
        {
            m.players[i].UnGenerateEEVI(seiArr[i]);
        }
    }

    public override void _Ready()
    {
        base.BaseReady();
        ms = (MultiplayerSynchronizer)GetNode("MultSync");
        ms.SetMultiplayerAuthority((int)id);

        isAuthority = this.id == Multiplayer.GetUniqueId();

        m = (match)this.GetParent();
        GD.Print("PLAYER INDEX: " + playerIndex + "------------------- " + id);
        bodySpr.Frame = playerIndex;

        if (!isAuthority)
        {
            rollCooldownSpr.Visible = false;
            ((Sprite2D)GetNode("Caret")).Visible = false;
        }
        else
        {
            trc.TimerFinished += RollCooldownFinished;
        }
        shaking = false;


        //NOTE: make an interface or something for this, although I have no idea where to put it. Maybe a separate script file?
        //nvm
        tool.AfterPlayerReady();
        tool.SetupSpriteIndices(toolIndices);

        m.players.Add(this);
        m.activePlayers.Add(this);
        if (isAuthority) { m.authorityPlayer = this; }
        m.tools.Add(tool);

        if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:player_number_authority"]) { GD.Print("Player number " + (playerIndex + 1) + " is authority (true/false) " + isAuthority + ", as player id is " + this.id + " and multiplayer id is " + Multiplayer.GetUniqueId()); }

        SetHealths(m.playerMaxHealths, m.playerCurrentHealths);

        JetpackSprite.Visible = m.isLowGravity;

        rollCooldownSpr.Visible = isAuthority;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    //DON'T use this. use playerProcess instead, because this is called IN ADDITION to any children's _Process function, such that any of them calling this will run it twice (bruh)
    public override void _Process(double delta)
    {
        
    }
    public override void _PhysicsProcess(double delta)
    {
        //Only send to the host, and only call it for their player (so host-held sei stays consistent)
        if (isAuthority && !Multiplayer.IsServer())
        {
            m.players[0].RpcId(1, "SyncEffVelInfoToHost", JsonSerializer.Serialize(GenerateEEVI()), playerIndex);
        }
        //NOTE: if the player is dead, their ghost movement controls are handled here, so don't prevent PlayerProcess from running here!
        //(ironic that one of the longest-lived comments was by me, saying to myself that death is checked here, so it shouldn't be done 
        //in player process "like a dum dum")
        PlayerProcess(delta);
        if (!stunned)
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
    }
    protected void PlayerProcess(double delta)
    {
        Vector2 direction= Vector2.Zero;
        if (isAuthority)
        {
            //yo if the movement is weird I might have changed the effectvelocity stuff without testing it lol
            EntityStartUpdateVelocities();

            if (!Input.IsActionPressed("ui_down") && !stunned && !dead)
            {
                if (floorAndWallStatus[0])
                {
                    bodySprTransform = new Transform2D(new Vector2(2, 0), new Vector2(-0.25f * Mathf.Sign(Velocity.X), 2), Vector2.Zero);
                }
                else
                {
                    bodySprTransform = new Transform2D(new Vector2(2, -0.5f * Mathf.Sign(Velocity.X) * Velocity.Y / JumpVelocity), new Vector2(-0.25f * Mathf.Sign(Velocity.X), 2), Vector2.Zero);
                }
            }

            BaseHandleTerrainNorms(bodyCol);

            //MOVEMENT
            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            //----------------------------------------------------------------------------------------------------------------------------------------------------------------

            BaseHandleCoyoteTime();

            if (!floorAndWallStatus[0] && !th.active && !tCoyote.active && !dead)
            {
                velocity.Y += gravity * (float)delta;
            }

            // Handle Jump.
            //for some ungodly reason ui_"""""accept"""" is space and not enter (I know I can change it, but i dont wanna)
            if (Input.IsActionPressed("ui_accept") && (floorAndWallStatus[0] || tCoyote.active))
            {
                velocity.Y = JumpVelocity;
            }

            // Get the input direction and handle the movement/deceleration.
            // As good practice, you should replace UI actions with custom gameplay actions.
            direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            lastNonZeroDirectionVector = !direction.IsEqualApprox(Vector2.Zero) ? direction : lastNonZeroDirectionVector;
            if (!m.isLowGravity && !dead)
            {
                if (direction != Vector2.Zero && !rolling && !stunned)
                {
                    velocity.X = direction.X * Speed;
                }
                else if (!rolling)
                {
                    velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                }
            }
            else if (!dead)//do it with the badass jetpack
            {
                if (direction != Vector2.Zero && !rolling && !stunned)
                {
                    velocity += direction * Speed * 2f * (float)delta; //I know it shouldnt be speed, but it should scale just the same
                    if (JetpackSprite.Animation != "Fly") JetpackSprite.Play("Fly"); //dont try playing the animatione very frame
                    JetpackSprite.Rotation = direction.Angle() + Mathf.Pi / 2;
                }
                else
                {
                    JetpackSprite.Play("Still");
                }
            }
            else
            {
                velocity = direction * Speed; //just float around, as the ghost
                bodySpr.Scale = new Vector2(2 * direction.X, 2);
            }

            JetpackSyncedInfo = new Vector2(JetpackSprite.Rotation, JetpackSprite.Animation == "Fly" ? 1 : 0);


        }
        else
        {
            JetpackSprite.Play(JetpackSyncedInfo.Y == 1 ? "Fly" : "Still");
            JetpackSprite.Rotation = JetpackSyncedInfo.X;
        }
        //Roll
        if (!dead)
        {
            BaseHandleRoll(direction, isAuthority);
        }
        
        if (isAuthority)
        {
            EntityEndUpdateVelocities((float)delta, !tool.toolPreventingTerrainDamage, EffectVelocity.Length() < Speed * 1.3f);
        }
        
    }

    //--------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void StopRolling()
    {
        rollStopper = true;
    }

    public override void RollCooldownFinished()
    {
        rollCooldownSpr.TextureOver = EndRollCooldownOver;
    }

    //--------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------

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
        //Should be handled by PlayerToolUniv.cs (unless something else can damage the player [not implemented yet])
    }
    private void _on_damage_area_body_entered(Node2D body) {
        if (body == m.tm && !tool.toolPreventingTerrainDamage)
        {
            hitTerrainFirst = true;
            //Don't damage: done in EntityEndUpdateVelocities
        }
    }

    private void _on_damage_area_body_exited(Node2D body)
    {
        hitTerrainFirst = false;
    }
    private void _on_body_sprite_animation_finished()
    {
        if (dead) //the death animation
        {
            m.PlayerDies(this);
            HandleFinalDeath();
        }
        else
        {
            GD.Print("Somehow the body sprite animation finished??? Printed from method \"_on_body_sprite_animation_finished\" in playerBase.cs");
        }

    }
    private void HandleFinalDeath()
    {
        GD.Print("handling final death");
        tool.SetActive(false);
        this.damageCol.Disabled = true;
        this.bodyCol.Disabled = true;
        faceSpr.Visible = false;
        JetpackSprite.Visible = false;

        bodySpr.Play("ghost");
    }

}



