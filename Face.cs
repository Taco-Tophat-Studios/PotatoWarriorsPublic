using Godot;
using System;

public partial class Face : AnimatedSprite2D
{
    match m;
    FendOffMatch fM;
    bool singlePlayer = false;
    Vector2 diff;
    playerInherited parent;
    float angle;
    float distance;
    float lastDistance = -1; //start at negative one and check it later because otherwise it is saved between frames and leads to weird glitches
    [Export]
    public int faceSpriteIndex = 0;
    Vector2 startScale;
    bool isAuthority = false;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        try
        { 
            m = (match)GetTree().Root.GetNode("World");
            parent = (playerBase)GetParent();
        }
        catch (InvalidCastException e)
        {
            if (!e.Message.Contains("FendOffMatch"))
            {
                GD.Print("ICE doesn't case to either (Face.cs, Ready)"); //had to do something with the I. C. E. variable lol
            }
            fM = (FendOffMatch)GetTree().Root.GetNode("World");

            parent = (singlePlayerBase)GetParent();

            singlePlayer = true;
        }

        distance = 20;

        startScale = new Vector2(this.Scale.X, this.Scale.Y);
    }
    public void AfterReady()
    {
        if (!singlePlayer && parent.isAuthority) 
        {
            this.Frame = (int)Global.GetLocalPlayerProperty("faceIndex");
        } else
        {
            this.Frame = 0;
        }
        this.Frame = faceSpriteIndex;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!singlePlayer)
        {
            foreach (playerBase p in m.players)
            {
                UpdateAngle(p);
            }
            
        } else
        {
            foreach (enemyBase e in fM.enemies)
            {
                UpdateAngle(e);
            }
        }
        lastDistance = -1;
        if (Mathf.Abs(angle) > Mathf.Pi / 2)
        {
            this.Scale = new Vector2(startScale.X * -1, startScale.Y);
        }
        else
        {
            this.Scale = startScale;
        }

        Position = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }
    private void UpdateAngle(Node2D obj)
    {
        diff = obj.GlobalPosition - parent.GlobalPosition;

        if (lastDistance > diff.Length() || lastDistance == -1)
        {
            angle = diff.Angle();
        }
        lastDistance = diff.Length();
    }
}
