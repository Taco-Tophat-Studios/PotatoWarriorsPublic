using Godot;
using System;

public partial class buttonBaseClass : Button
{
    [Export]
    protected string scene;
    protected void Transfer()
    {
        GetTree().ChangeSceneToFile(scene);
    }
    private void _on_button_down() {
        Transfer();
    }
}
