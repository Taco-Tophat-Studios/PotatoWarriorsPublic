using Godot;

public partial class Header : AnimatedSprite2D
{
    bool alreadyPlayed = false; /*because it can't just check if there are 3 or 4 players and then
	                            play upon animation finish, because it would then finish that, animation, 
                                and do it again, over and over*/
    match m;
    Label p1L;
    Label p2L;
    Label p3L;
    Label p4L;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        m = (match)GetNode("../../../World");
        p1L = (Label)GetNode("../P1Label");
        p2L = (Label)GetNode("../P2Label");
        p3L = (Label)GetNode("../P3Label");
        p4L = (Label)GetNode("../P4Label");

        p1L.Visible = true;
        p2L.Visible = false;
        p3L.Visible = false;
        p4L.Visible = false;

        this.Play("base");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    private void _on_animation_finished()
    { //ngl this might be the most convoluted sequence of logic i've ever made
        if (m.players.Count == 2)
        {
            p1L.Visible = true;
            p2L.Visible = true;
        }
        else if (m.players.Count == 3)
        {
            if (!alreadyPlayed)
            {
                this.Play("3player");
                alreadyPlayed = true;
            }
            else
            {
                p3L.Visible = true;
            }
        }
        else if (m.players.Count == 4)
        {
            if (!alreadyPlayed)
            {
                this.Play("4player");
                alreadyPlayed = true;
            }
            else
            {
                p3L.Visible = true;
                p4L.Visible = true;
            }
        }

    }

}



