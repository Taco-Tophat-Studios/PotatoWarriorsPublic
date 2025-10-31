using Godot;

public partial class Portal : Node2D
{
    [Export]
    public string destination = "";
    [Export]
    public Node2D portalExit;
    match m;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        m = (match)this.GetParent();
        //possibly implement the classic fendoffmatch try-catch here
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    private void _on_portal_area_body_entered(Node2D body)
    {
        if (body is playerInherited player)
        {
            if (destination.Contains("res://"))
            {
                GetTree().CallDeferred("change_scene_to_file", destination); //For scenarios where this is used to explo da world
            }
            else
            {
                //string[] coords = destination.Split('|');
                player.GlobalPosition = portalExit.GlobalPosition;
            }
        }
    }
}



