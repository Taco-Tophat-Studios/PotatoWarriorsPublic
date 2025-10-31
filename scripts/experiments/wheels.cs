using Godot;

public partial class wheels : Node2D
{
    public static float rotVal = 0;
    public static float rotScale = 8f;
    private Sprite2D[] wheelSprites;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        wheelSprites = new Sprite2D[] { (Sprite2D)GetNode("Wheel1"), (Sprite2D)GetNode("Wheel2"), (Sprite2D)GetNode("Wheel3"), (Sprite2D)GetNode("Wheel4"), };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            rotScale -= 0.05f;
        }
        else if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            rotScale += 0.05f;
        }

        rotVal += rotScale * (float)delta;
        if (rotVal >= 2 * Mathf.Pi)
        {
            rotVal = 0;
        }
        foreach (Sprite2D s in wheelSprites)
        {
            s.Rotation = rotVal;
        }
    }

}
