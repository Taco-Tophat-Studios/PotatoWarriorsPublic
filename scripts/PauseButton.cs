using Godot;
using System;

public partial class PauseButton : Button
{
	match m;
	private bool open = false;
	private Control pauseMenu;
	private Button exitButton;
	private Sprite2D icon;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		m = (match)GetNode("../../../World");
		pauseMenu = (Control)GetNode("../PauseMenu");
		exitButton = (Button)GetNode("../ExitButton");
		exitButton.Visible = false;
		pauseMenu.Visible = false;

		icon = (Sprite2D)GetNode("IconSprite");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) {
			OpenOrClose();
		}
	}
	private void _on_button_down()
	{
		OpenOrClose();
	}
	private void OpenOrClose() {
		//NOTE: may want to delegate this to a method that goes through the children of the control node
		//and activates/deactivates them
		if (!open) {
			pauseMenu.Visible = true;
			exitButton.Visible = true;
			icon.Texture = (AtlasTexture)GD.Load("res://Misc/playIconAtlas.tres");
			open = true;
			m.paused = true;
		} else {
			pauseMenu.Visible = false;
			exitButton.Visible = false;
			icon.Texture = (AtlasTexture)GD.Load("res://Misc/pauseIconAtlas.tres");
			open = false;
			m.paused = false;
		}
	}
}



