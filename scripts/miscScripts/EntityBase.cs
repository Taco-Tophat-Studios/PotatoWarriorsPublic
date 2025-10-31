using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class EntityBase : CharacterBody2D
{
    public bool dead = false;
    public bool invulnerable = false;
    [Export]
    public bool frozen = false;
    public string state = "";
    public float gravity = 3000f;
    [Export]
    public float health;
    public float maxHealth;
    [Export]
    public Vector2 velocity;
    private Vector2 effVel;
    public Vector2 RawEffVel => effVel;
    public Vector2 EffectVelocity
    {
        get
        {
            var sum = effVel;
            foreach (var comp in effVelComponents)
            {
                if (comp.Item3) // only include components that should be counted
                    sum += comp.Item2;
            }

            return sum;
        }
        set
        {
            effVel = value;
            effVelComponents.Clear(); //clear the components when effectVelocity is set, so that it can be rebuilt
        }
    }
    /// <summary>
    /// Generate an EntityEffVelInfo object from THIS ENTITY's info.
    /// </summary>
    /// <returns>The object. Duh.</returns>
    protected EntityEffVelInfo GenerateEEVI()
    {
        try
        {
            EntityEffVelInfo eEVI = new();
            eEVI.effVel = effVel;

            eEVI.eVC_Names = new string[effVelComponents.Count];
            eEVI.eVC_Velocities = new Vector2[effVelComponents.Count];
            eEVI.eVC_IncludeInTotals = new bool[effVelComponents.Count];
            for (int i = 0; i < effVelComponents.Count; i++)
            {
                eEVI.eVC_Names[i] = effVelComponents[i].Item1;
                eEVI.eVC_Velocities[i] = effVelComponents[i].Item2;
                eEVI.eVC_IncludeInTotals[i] = effVelComponents[i].Item3;
            }
            return eEVI;
        } catch (Exception e)
        {
            GD.Print("ERROR: Could not generate EEVI: " + e.Message);
            return null;
        }
        
    }
    /// <summary>
    /// Set THIS ENTITY'S effect velocity info to whatever is held in the parameter
    /// </summary>
    /// <param name="info">The EEVI with which to set the entity's effect velocity information</param>
    protected void UnGenerateEEVI(EntityEffVelInfo info)
    {
        effVel = info.effVel;
        effVelComponents.Clear();

        for (int i = 0; i < info.eVC_Names.Length; i++)
        {
            effVelComponents.Add(new Tuple<string, Vector2, bool>(info.eVC_Names[i], info.eVC_Velocities[i], info.eVC_IncludeInTotals[i]));
        }
    }
    //REMINDER | NOTE: to serialize, use GD.VarToBytesWithObjects() and GD.BytesToVarWithObjects()
    //DON'T DELETeE: There may be no results in searching the project, but it is used in the player's MultiplayerSynchronizer Node
    [Export]
    public string SerializedEffVelInfo
    {
        get
        {
            return JsonSerializer.Serialize(GenerateEEVI());
        }
        set
        {
            var info = JsonSerializer.Deserialize<EntityEffVelInfo>(value);
            if (info != null)
            {
                UnGenerateEEVI(info);
            }
            else
            {
                GD.Print("ERROR: Failed to deserialize EntityEffVelInfo from bytes.");
            }
        }
    }
    //you know what? sure. We're keeping "mofidifer"
    public void ModifyEVComponentWise(Func<string, Vector2, bool, Vector2> mofidifer, bool includeEffVel = true)
    {
        Tuple<string, Vector2, bool> temp;
        for (int i = 0; i < effVelComponents.Count; i++)
        {
            temp = effVelComponents[i];
            effVelComponents[i] = new Tuple<string, Vector2, bool>(temp.Item1, mofidifer(temp.Item1, temp.Item2, temp.Item3), temp.Item3);
        }

        if (includeEffVel)
        {
            //WARNING: if anything else uses "EFFVEL", bad things will happen! (probably)
            effVel = mofidifer("EFFVEL", effVel, true);
        } 
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Network_ModifyEVComponentWise(Node2D caller, string mofidifer, bool includeEffVel = true, float scaler = 1f)
    {
        SyncedCallables SC = new();
        Func<string, Vector2, bool, Vector2> mofidiferWrapper = (string n, Vector2 eV, bool sBC) =>
        {
            return (Vector2)SC.Call(mofidifer, caller, n, eV, sBC) * scaler;
        };
        ModifyEVComponentWise(mofidiferWrapper, includeEffVel);
    }

    //                                                                          //tag, velocity, should be counted?
    public List<Tuple<string, Vector2, bool>> effVelComponents = new List<Tuple<string, Vector2, bool>>(); //used for storing the components of effectVelocity, so they can be selectively removed later
                                                                                                           //because tags are meant to be searched with string methods, an example would be "obj_manv_launcher interact_entity", specifying that it is a launcher, is a maneuvering object, can be interacted with by entities, etc.
                                                                                                           //TODO: repeat this for effectAcceleration, ad idem


    ///<summary>
    ///Contribute to Effect Velocity
    ///</summary>
    ///<param name="combineMode">the way to combine the new value with an already existant one with the same name, if it exists.<br/>0 = replace, 1 = add, 2 = clear, 3 = maximum components, 4 = minimum components, and anything else defaults to replace</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void CTEV(string name, Vector2 vel, int combineMode = 0 /*see below*/, bool sBC = true)
    {   //CTEV = Contribute to Effect Velocity
        // find existing component by name
        int foundIndex = -1;
        for (int i = 0; i < effVelComponents.Count; i++)
        {
            if (effVelComponents[i].Item1 == name)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex != -1)
        {
            Tuple<string, Vector2, bool> t = effVelComponents[foundIndex];
            switch (combineMode)
            {
                case 0: //replace
                        //Don't do anything (it replaces t anyway)
                    break;
                case 1: //add
                    vel += t.Item2;
                    break;
                case 2: //clear
                    vel = Vector2.Zero;
                    effVelComponents.Remove(t);
                    return; //just remove it
                            //break;
                case 3: //take max
                    vel = vel.Length() > t.Item2.Length() ? vel : t.Item2;
                    break;
                case 4: //take min
                    vel = vel.Length() < t.Item2.Length() ? vel : t.Item2;
                    break;
                default: //default is replace
                    vel = t.Item2;
                    break;
            }
            effVelComponents.RemoveAt(foundIndex); //faster than remove
        }
        effVelComponents.Add(new Tuple<string, Vector2, bool>(name, vel, sBC));
    }
    public Vector2 lastEffectVelocity;
    public List<Tuple<string, Vector2, bool>> lastEffVelComponents = new List<Tuple<string, Vector2, bool>>();
    public Vector2 effectAcceleration; //for instances where velocity should be smooth
    //TOTALVELOCITY: if you're control-f'ing for a total velocity, this is the right property VVVV
    public Vector2 VelocityWithEffVel;
    protected Vector2 pastPosition;
    public Vector2 positionDiff; //used for calculating the velocity of the entity in the previous frame
    public bool bouncingX = false;
    public bool bouncingY = false;
    public bool bouncing => bouncingX || bouncingY;
    public const float bounceDampening = 0.75f;
    public const float floorFriction = 0.5f; //friction on the floor (per second)
    public bool experienceFloorFriction = true;
    public float minBounceEffVel = BalancedValues.UNIT_EFFECT_VELOCITY * 0.5f; //minimum effect velocity to bounce
    public bool wasOnFloorLastFrame = false; //(primarily for coyote time)
    public bool[] floorAndWallStatus = {false, false, false}; //floor, wall, ceiling
    public Vector2I[] currentTerrainNorms = new Vector2I[4]; //Up, Right, Down, Left, each either the corresponding normal or Vector2I.Zero
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void Damage(float h, string cause = "NO CAUSE GIVEN")
    {
        if (invulnerable)
        {
            return; //probably dont need to log this, even with debug tag true
        }
        if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:entity_damage_info"]) GD.Print("damaged for " + h + " from cause " + cause);
        health -= h;
        if (health <= 0 && !dead)
        {
            dead = true;
            health = 0;
            Die();
        } else
        {
            Damaged?.Invoke(h);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)] //usually only heal self, but in case ever implemented
    public virtual void Heal(float h, bool exceed = false)
    {
        if (invulnerable)
        {
            return; //probably dont need to log this, even with debug tag true
        }
        GD.Print("healed for " + h);
        health += h;
        if (!exceed && health > maxHealth)
        {
            h = maxHealth - health; //for raising healed event; how much it was healed
            health = maxHealth;
            GD.Print("set health to maxHealth: " + maxHealth);
        }
        Healed?.Invoke(h);
    }

    protected event Action<float> Damaged;
    protected event Action<float> Healed;
    //NOTE: the following methods are listed in the approximate order in which they should be called (in the entity-specific classes)
    /// <summary>
    /// Calls start update velocities, manage gravity, whatever the intermediate is, and then end update velocities
    /// </summary>
    /// <param name="delta">time between frames</param>
    /// <param name="Intermediate">any intermediate function to call</param>
    protected void EntityPhysicsPackage1(float delta, Action<float> Intermediate = null)
    {
        EntityStartUpdateVelocities();
        EntityManageGravity(delta);
        Intermediate?.Invoke(delta);
        EntityEndUpdateVelocities(delta);
    }
    protected void EntityStartUpdateVelocities()
    {
        velocity = new Vector2(Velocity.X, Velocity.Y);

        floorAndWallStatus[0] = IsOnFloor();
        floorAndWallStatus[1] = IsOnWall();
        floorAndWallStatus[2] = IsOnCeiling();
    }
    protected void EntityManageGravity(float d) //not always applicable
    {
        if (!floorAndWallStatus[0])
        {
            velocity.Y += gravity * (float)d;
        }
    }

    protected void EntityEndUpdateVelocities(float d, bool damageOnBounce = true, bool stopOnFloor = false) {
        if (!frozen)
        {
            positionDiff = GlobalPosition - pastPosition;
            pastPosition = new Vector2(GlobalPosition.X, GlobalPosition.Y);
            lastEffectVelocity = new Vector2(EffectVelocity.X, EffectVelocity.Y);
            lastEffVelComponents.Clear();
            foreach (var comp in effVelComponents)
            {
                lastEffVelComponents.Add(new Tuple<string, Vector2, bool>(comp.Item1, comp.Item2, comp.Item3));
            }
            effVel.LimitLength(9600);
            effVel += effectAcceleration * d;

            //Note for below: note for below "effectVelocity" is actually effVel here, but I'm too lazy to change it (lelz)
            /*
                The velocity system works as such:
                1. velocity (lowercase v): this variable is used for player velocity within the process block, and any code run from the process block
                2. Velocity (uppercase V): this variable is the same, but outside the process block
                3. effectVelocity: this variable is used for any velocity that is not the player's, such as a knockback or a push

                velocity starts at Velocity, at the beginning of the process block, and then is modified to then eventually change Velocity.
                Meanwhile, effectVelocity is added to Velocity for MoveAndSlide. Then, if Velocity is 0 in some component, AFTER HAVING BEEN INCREASED BY EFFECTVELOCITY,
                this can only happen if the player is blocked in some way (e. g., by a wall), and thus, effectVelocity should be reset so it doesn't try and push the player
                up against the same wall, ceiling, etc. (or push them towards it upon escape). Then, it is removed from Velocity afterward, so that it can be referenced normally
            */
            if (floorAndWallStatus[0])
            {
                Func<string, Vector2, bool, Vector2> slowerDowner = (string n, Vector2 eV, bool sBC) =>
                {
                    if (!stopOnFloor)
                    {
                        if (!(n.Contains("arena") || n.Contains("env")) && sBC && experienceFloorFriction)
                        { //if caused by something not applicable to the general area
                            return new Vector2(eV.X * Mathf.Pow(floorFriction, d), eV.Y);
                        }
                        else
                        {
                            return eV;
                        }
                    } else
                    {
                        return sBC ? Vector2.Zero : eV; //if it's not being counted right now, it might later
                    }
                };
                ModifyEVComponentWise(slowerDowner);
            }

            Velocity = velocity + EffectVelocity; //because the effectVelocity get handles the sum of the components and effVel
            VelocityWithEffVel = new Vector2(Velocity.X, Velocity.Y);

            MoveAndSlide();


            //Bounce or Reset

            Vector2 bounceEffVel = Vector2.Zero;

            if (Mathf.IsZeroApprox(Velocity.X) && EffectVelocity.X != 0)
            {
                if (Mathf.Abs(EffectVelocity.X) >= minBounceEffVel && !bouncingX)
                {
                    bounceEffVel = new Vector2(EffectVelocity.X, bounceEffVel.Y); //y component shopuld be zero (x is assigned first), but this is done in case this gets moved around
                    if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:effvelX_bounce_reset"]) { GD.Print("effvelX bounce"); }
                    ResetOrBounceComponents(Vector2.Right, true);
                    bouncingX = true;
                }
                else
                {
                    if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:effvelX_bounce_reset"]) { GD.Print("effvelX reset or already bouncing"); }
                    bouncingX = false;
                    ResetOrBounceComponents(Vector2.Right, false);
                }
            }
            else
            {
                bouncingX = false; //reset when not being stopped at all
            }

            if (Mathf.IsZeroApprox(Velocity.Y) && EffectVelocity.Y != 0)
            {
                if (Mathf.Abs(EffectVelocity.Y) >= minBounceEffVel && !bouncingY)
                {
                    bounceEffVel = new Vector2(bounceEffVel.X, EffectVelocity.Y);
                    if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:effvelY_bounce_reset"]) { GD.Print("effvelY bounce"); }
                    ResetOrBounceComponents(Vector2.Down, true);
                    bouncingY = true;
                }
                else
                {
                    if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:effvelY_bounce_reset"]) { GD.Print("effvelY reset or already bouncing"); }
                    bouncingY = false;
                    ResetOrBounceComponents(Vector2.Down, false);
                }
            }
            else
            {
                bouncingY = false;
            }

            //Damage if bouncing too hard
            if (damageOnBounce && !bounceEffVel.IsZeroApprox())
            {
                if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:bounce_damage_info"])
                {
                    string debugString = "";
                    
                    foreach (var comp in effVelComponents)
                    {
                        debugString += "||||| " + comp.Item1 + " | " + comp.Item2;
                    }
                    GD.Print("bounce EV: " + bounceEffVel + " | Frame: " + Engine.GetPhysicsFrames() + " | components: " + debugString);
                }
                Damage(bounceEffVel.Length() / BalancedValues.UNIT_EFFECT_VELOCITY * BalancedValues.UNIT_DAMAGE * (this is playerInherited ? 0.25f : 0.75f), "Entity bounce");
            }
            

            //reset effVel and effVelComponents if they are too small
            if (effVel.Length() < BalancedValues.UNIT_EFFECT_VELOCITY * 0.25f)
            {
                effVel = Vector2.Zero;
            }
            for (int i = 0; i < effVelComponents.Count; i++)
            {
                if (effVelComponents[i].Item2.Length() < BalancedValues.UNIT_EFFECT_VELOCITY * 0.25f && effVelComponents[i].Item3) //if it isn't being counted, it probably serves a later purpose
                {
                    effVelComponents.RemoveAt(i);
                    i--;
                }

            }
            //DONT clear components if effectVelocity is zero, as it may only be due to the net addition of opposite components (which could be removed individually at some point)
            Velocity -= EffectVelocity; //remove effectVelocity from Velocity, so that it can be used normally
            wasOnFloorLastFrame = floorAndWallStatus[0];
        }
    }

    private void ResetOrBounceComponents(Vector2 dir, bool bounce, float d = bounceDampening)
    {
        d = bounce ? d : 0;
        effVel -= (1 + d) * dir * effVel;
        //TODO: change this into a ModifyEVComponentWise call
        for (int i = 0; i < effVelComponents.Count; i++)
        {
            if (effVelComponents[i].Item3 && !(effVelComponents[i].Item1.Contains("arena") || effVelComponents[i].Item1.Contains("env")))
            {
                Vector2 comp = effVelComponents[i].Item2;
                comp -= (1 + d) * dir * comp;
                effVelComponents[i] = new Tuple<string, Vector2, bool>(effVelComponents[i].Item1, comp, effVelComponents[i].Item3);
            }
            
        }
    }
    public virtual void Die() { }
    public virtual void Stun() {}
}
