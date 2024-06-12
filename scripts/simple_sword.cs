using Godot;
using System;

public partial class simple_sword : Area2D
{
    // Called when the node enters the scene tree for the first time.
    const float multiplier = 15f; 
    AnimatedSprite2D overlay;
    float angVel = 0;
    const float max = 1280;
    public override void _Ready()
    {
        overlay = (AnimatedSprite2D)GetNode("Overlay1");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();

        if (Input.IsMouseButtonPressed(MouseButton.Left) && angVel > -max)
        {
            //left
            angVel -= (3f - (Mathf.Abs(angVel) / max)) * multiplier;
            SlowFaster(true);
        }
        else if (Input.IsMouseButtonPressed(MouseButton.Right) && angVel < max)
        {
            //right
            angVel += (3f - (Mathf.Abs(angVel) / max)) * multiplier;
            SlowFaster(false);
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
        RotationDegrees += angVel * (float)GetProcessDeltaTime();

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
    }

    private void SlowFaster(bool left)
    {
        //technically the addition of angAcc is called thrice: twice here and once before method
        if (angVel > 0 && left)
        {
            angVel -= multiplier * 2.5f;
        }
        else if (angVel < 0 && !left)
        {
            angVel += multiplier * 2.5f;
        }

    }

   
}

