using Godot;
using System;

public partial class Soldier : enemyBase
{
    public bool running = true;
    //[Export]
    //public 

    public override void _Ready()
	{
        Behaviour.Add("startrun", new Response(new Response.ResponseDel[] {BeginRun}));
    }
    public override void _Process(double delta)
	{
        
    }
    public void BeginRun(params Variant[] args) {

    }
    private bool PlayerOnSight(TileMap tm) {
        bool result;

        RayCast2D r = new RayCast2D();
        r.Transform = new Transform2D(new Vector2(0, 1), new Vector2(1, 0), this.GlobalPosition);
        r.TargetPosition = player.GlobalPosition;
        foreach(enemyBase e in otherEnemies) {
            r.AddException(e);
        }
        r.AddException(this);

        //NOTE: possibly change this, using an error as a condition seems less than ideal
        try {
            if((singlePlayerBase)r.GetCollider() == player) {
               result = true;
            }
        } finally {
            result = false;
        }
        return result;

        
    }
    
}
