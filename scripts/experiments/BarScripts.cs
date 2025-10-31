using Godot;

public partial class BarScripts : Node2D
{
    public Vector2 startingPos;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        startingPos = Position;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //NOTE: make all of the Node2D initializers done on ready instead of every frame, doofus!
        float radius = 48;
        Node2D slider = (Node2D)GetNode("../SlidingPoint");
        Node2D drivingConnector = (Node2D)GetNode("../DrivingBar/ConnectionPoint");


        switch (this.Name)
        {
            case "ConnectionBar":
                Position = new Vector2(startingPos.X + radius * Mathf.Cos(wheels.rotVal), startingPos.Y + radius * Mathf.Sin(wheels.rotVal));
                break;
            case "CrankBar":
                Node2D connector = (Node2D)GetNode("../ConnectionBar");
                //looks same, but y is divided by 2
                Position = new Vector2(startingPos.X + radius * Mathf.Cos(wheels.rotVal), startingPos.Y + radius * Mathf.Sin(wheels.rotVal) / 2);
                Rotation = Mathf.Atan2(connector.Position.Y - startingPos.Y,
                                      (connector.Position.X + 256) - (startingPos.X + 336)); //336 is half of scale
                break;
            case "OffsetBar":
                radius = 36;
                Position = new Vector2(startingPos.X + radius * Mathf.Cos(wheels.rotVal - Mathf.Pi / 4), startingPos.Y + radius * Mathf.Sin(wheels.rotVal - Mathf.Pi / 4));
                Rotation = wheels.rotVal + Mathf.Pi / 4;
                break;
            case "DrivingBar":
                Position = new Vector2(startingPos.X + radius * Mathf.Cos(wheels.rotVal), startingPos.Y);
                break;
            case "JohnsonBar":
                Rotation = -Mathf.Cos(wheels.rotVal - Mathf.Pi / 3) * 0.8f - Mathf.Pi / 16;
                break;
            case "CalibrationBar":
                Node2D c1 = (Node2D)GetNode("../OffsetBar/ConnectionPoint");
                Node2D c2 = (Node2D)GetNode("../JohnsonBar/ConnectionPoint");
                GlobalPosition = (c1.GlobalPosition + c2.GlobalPosition) / 2; //new Vector2((c1.GlobalPosition.X + c2.GlobalPosition.X) / 2, (c1.GlobalPosition.Y + c2.GlobalPosition.Y) / 2);
                Rotation = Mathf.Atan2(c2.GlobalPosition.Y - c1.GlobalPosition.Y, c2.GlobalPosition.X - c1.GlobalPosition.X);
                break;
            case "SlidingPoint":
                Position = new Vector2(startingPos.X + Mathf.Cos(wheels.rotVal - Mathf.Pi / 3) * 44.589229f, startingPos.Y); ;
                break;
            case "BackBar":

                Node2D c = (Node2D)GetNode("../JohnsonBar/ConnectionPoint2");
                Node2D cThis = (Node2D)GetNode("ConnectionPoint"); //do you cThis?
                Position = (c.GlobalPosition + slider.GlobalPosition) / 2;
                Rotation = Mathf.Atan2(c.GlobalPosition.Y - slider.GlobalPosition.Y, c.GlobalPosition.X - slider.GlobalPosition.X) + Mathf.Pi; //bc otherwise its flipped
                break;
            case "PistonBar":
                float distance = -3.4f * radius * Mathf.Cos(wheels.rotVal - Mathf.Pi / 5) * (slider.Position.Y - Position.Y) / (slider.Position.Y - drivingConnector.Position.Y);
                Position = new Vector2(startingPos.X + distance, startingPos.Y);
                break;
            case "LeadBar":
                Position = (slider.Position + drivingConnector.GlobalPosition) / 2;
                Rotation = Mathf.Atan2(slider.Position.Y - drivingConnector.GlobalPosition.Y, slider.Position.X - drivingConnector.GlobalPosition.X) + Mathf.Pi / 2;
                break;

        }
    }
}
