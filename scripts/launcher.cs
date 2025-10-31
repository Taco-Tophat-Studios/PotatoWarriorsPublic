using System.Transactions;
using Godot;

public partial class launcher : Area2D
{
    //TODO: convert this to work in both single and multiplayer
    match m;
    playerBase enteringPlayer;
    bool active;
    [Export]
    float boost = 2000;
    Sprite2D keyOverlay;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        m = (match)this.GetParent();
        active = false;

        keyOverlay = (Sprite2D)GetNode("KeyOverlay");
        keyOverlay.Visible = false;

        keyOverlay.GlobalRotation = 0;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (active && Input.IsActionJustPressed("objectInteract"))
        {
            enteringPlayer.Velocity = Vector2.Zero;
            //set velocity becuase the launcher is just like that
            //enteringPlayer.EffectVelocity = new Vector2(Mathf.Cos(Rotation - Mathf.Pi / 2) * boost, Mathf.Sin(Rotation - Mathf.Pi / 2) * boost);
            enteringPlayer.CTEV("launcher_boost", new Vector2(Mathf.Cos(Rotation - Mathf.Pi / 2) * boost, Mathf.Sin(Rotation - Mathf.Pi / 2) * boost));
            if (enteringPlayer.rolling) {
                //enteringPlayer.rollStopper = true;
                RpcId(enteringPlayer.id, "StopRolling");
            }

            active = false;
            keyOverlay.Visible = false;
        }
    }
    private void _on_body_entered(Node2D body)
    {
        if (body is playerBase player)
        {
            enteringPlayer = player;
            active = true;
            keyOverlay.Visible = false;
        }
    }
    //TODO: unify this behavior into one method
    private void _on_body_exited(Node2D body)
    {
        if (body is playerBase player)
        {
            active = false;
            keyOverlay.Visible = false;
            //dont need to set enteringPlayer to null or something because it will be set anyway
        }
    }
}



