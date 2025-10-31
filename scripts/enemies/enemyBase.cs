using Godot;
using System;
using System.Collections.Generic;

public partial class enemyBase : EntityBase
{
    protected readonly Random rand = new();
    protected FendOffMatch f;
    protected enum JumpKind
    {
        Low,
        LowCeiling,
        High,
        HighCeiling
    }
    protected Dictionary<JumpKind, Tuple<Vector2, Vector2>> JumpColDimensions;
    protected ulong jumpDetectionFrameDelay = 1; //supposed to be greater than 1. the number of frames where jump detection happens will only occur ever <this> frames for performance.
    //        ^^^^^ because you would have to use the % operator with that and Godot's Engine.GetPhysicsFrames(), which is in a ulong
    protected singlePlayerBase player;
    protected enemyBase[] otherEnemies;
    public string lastState;
    public bool startOfState = true;
    public List<Area2D> damageAreas;
    public List<Vector2> damageAreaVelocities; //Length 0 for static ones (like melee attacks), positive length for projectiles
    protected Vector2 SpritesStartScale;
    //public List<string> stateBuffer;
    //public List<string> animationBuffer;
    public Neuron currentNeuron = new();
    public Area2D AccessibleDamageArea;
    public float maxEffVelBeforeDamage = 500;

    public void SetMainAnimation(AnimatedSprite2D spr, string anim)
    {
        if (anim != spr.Animation)
        {
            spr.Play(anim);
        }
    }

    public void SetMainState(string st)
    {
        lastState = this.state;

        if (st != state)
        {
            state = st;
            startOfState = true;
        }
        else
        {
            startOfState = false;
        }
    }

    public virtual void _on_char_sprite_animation_finished() { }

    protected bool PlayerOnSight(RayCast2D line)
    {
        line.TargetPosition = player.GlobalPosition - this.GlobalPosition;

        return line.IsColliding() && line.GetCollider() == player; //short cicuit AND because latter term throws an error if first time is false, so dont consider it if so
    }
    
    //NOTE: don't handle player tool interaction here (done in PTU C# file)
}
