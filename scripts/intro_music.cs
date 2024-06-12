using Godot;
using System;

public partial class intro_music : AudioStreamPlayer
{
	public void _on_finished ()
	{
		Play();
	}
}
