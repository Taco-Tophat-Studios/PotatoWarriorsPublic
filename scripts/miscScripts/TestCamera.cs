using Godot;

public partial class TestCamera : Camera2D
{
    [Export]
    float camSpeed;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("ui_up"))
        {
            Position = new Vector2(Position.X, Position.Y - camSpeed * (float)GetProcessDeltaTime());
        }
        if (Input.IsActionPressed("ui_down"))
        {
            Position = new Vector2(Position.X, Position.Y + camSpeed * (float)GetProcessDeltaTime());
        }

        if (Input.IsActionPressed("ui_left"))
        {
            Position = new Vector2(Position.X - camSpeed * (float)GetProcessDeltaTime(), Position.Y);
        }
        if (Input.IsActionPressed("ui_right"))
        {
            Position = new Vector2(Position.X + camSpeed * (float)GetProcessDeltaTime(), Position.Y);
        }
    }
}
