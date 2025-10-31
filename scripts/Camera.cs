using Godot;
using System;

public partial class Camera : Camera2D
{
    float scale = 2;
    float[] offsetConstants = { 0, 0, 0, 0, 0 };
    //because the above go back and forth at different rates, they might need to be reversed
    float[] offsetIterators = { 1, 1, 0, 1, 0 };
    float[] offsetIncrements = { 17.5f, 75f, 11.25f, 0.75f, 17.5f };
    float[,] offsetMaxMins = { { -1, 1 }, { -1, 1 }, { 0, 2 * Mathf.Pi }, { 1, 3 }, { 0, 2 * Mathf.Pi } };
    float r = 0;
    match m;
    FendOffMatch f;
    //for dynamic camera limiting
    public Node2D LimitUL;
    public Node2D LimitDR;
    [Export]
    playerInherited player;
    float targetRot = 0;
    float zoomEffectAccTime = 0;
    float zoomEffectScale = 0;
    Node2D camPivotCenter;
    ParallaxBackground PB;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        try
		{
			f = (FendOffMatch)GetTree().Root.GetNode("World");
            LimitUL = f.CBUL;
            LimitDR = f.CBLR;
            PB = (ParallaxBackground)f.GetNode("ParallaxBackground");
			//isSingleSpecifically should be true but just in case: ||| isSingleSpecifically = true;
		}
		catch (InvalidCastException e)
		{
			if (!e.Message.Contains("match"))
			{
				GD.Print("invalid cast exception ISNT match (Camera.cs, Ready)"); //had to do something with the I. C. E. variable lol
			}
			m = (match)GetTree().Root.GetNode("World");
            PB = (ParallaxBackground)m.GetNode("ParallaxBackground");
            LimitUL = m.CBUL;
            LimitDR = m.CBLR;
		}
        camPivotCenter = (Node2D)GetParent();
        this.LimitTop = (int)LimitUL.GlobalPosition.Y;
        this.LimitLeft = (int)LimitUL.GlobalPosition.X;
        this.LimitBottom = (int)LimitDR.GlobalPosition.Y;
        this.LimitRight = (int)LimitDR.GlobalPosition.X;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (player.shaking)
        {
            ShakeCamera();
        }

        targetRot = 0.05f*Mathf.Clamp(player.VelocityWithEffVel.X, -BalancedValues.UNIT_EFFECT_VELOCITY * 2, BalancedValues.UNIT_EFFECT_VELOCITY * 2) / BalancedValues.UNIT_EFFECT_VELOCITY; //kinda wack but whatever

        camPivotCenter.Rotation = targetRot; //Godot has a built in system for rotation smoothing
        PB.Rotation = -targetRot;
        
        if (targetRot != 0)
        {
            DoZoom(1, delta);
        }
        else
        {
            DoZoom(-1, delta);
        }
    }
    private void DoZoom(int sign, double d) {
        zoomEffectAccTime = Mathf.Clamp(zoomEffectAccTime + sign*(float)d, 0, 0.2f);
        zoomEffectScale = 0.05f * (Mathf.Atan(20*zoomEffectAccTime - 2) + 1.1f);
        Zoom = new Vector2(player.defaultCamZoom - zoomEffectScale, player.defaultCamZoom - zoomEffectScale);
    }
    private void ShakeCamera()
    {
        Vector2 offsetPos;
        for (int i = 0; i < 5; i++)
        {
            if (offsetMaxMins[i, 1] != 2 * Mathf.Pi)
            {
                //if its above max, make it go the other way. If its below min, make it 
                if (offsetConstants[i] > offsetMaxMins[i, 1])
                {
                    offsetIterators[i] = -1;
                }

                if (offsetConstants[i] < offsetMaxMins[i, 0])
                {
                    offsetIterators[i] = 1;
                }
                //either reversed or not
                offsetConstants[i] += offsetIterators[i] * offsetIncrements[i] * (float)GetProcessDeltaTime();
            }
            else
            {
                offsetConstants[i] += offsetIncrements[i] * (float)GetProcessDeltaTime();
                //if they are angle - based and need to loop instead
                if (offsetConstants[i] > offsetMaxMins[i, 1])
                {
                    offsetConstants[i] = offsetMaxMins[i, 0]; //should be 0, but just in case
                }
            }
        }
        r = offsetConstants[0] + (offsetConstants[1] * Mathf.Sin(offsetConstants[3] * (offsetConstants[4] + offsetConstants[3])));
        offsetPos = new Vector2(Mathf.Cos(offsetConstants[4]) * r, Mathf.Sin(offsetConstants[4]) * r);
        this.Offset = offsetPos * scale;
    }
}
