using Godot;
using System;

public partial class ExitButton : buttonBaseClass
{
	private void _on_button_down()
	{
		scene = "res://Screens/Menus/MainMenu.tscn";

		if (!GetTree().Root.HasNode("IntroMusic"))
		{
			PackedScene arena = (PackedScene)GD.Load("res://Misc/intro_music.tscn");
			AudioStreamPlayer inst = (AudioStreamPlayer)arena.Instantiate();
			GetTree().Root.AddChild(inst);
		}
		if (HasNode("../../../Lobby")) {
			GD.Print("Lobby Recognized! " + Multiplayer.GetUniqueId());
		}
        

        Transfer();

		if (HasNode("../../VictoryScreen")) {
			GetNode("../../VictoryScreen").QueueFree();
		}
    }
	

}



