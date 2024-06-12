using Godot;
using System;

public partial class MainMenu : Node2D
{
	private float spinVal = 1.5f;
	private float loop = 0;
	private Sprite2D spinner;
	private Sprite2D titleLeft;
	private Sprite2D titleRight;
	private float startingTitleY;
	private Sprite2D swordArt;
	private float startingSwordY;
	private AudioStreamPlayer IM;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        /*Global.LoadCharData();
        if (Global.volSliderValues.IsEmpty())
        {
            Global.volSliderValues = new float[] { 0, 0, 0 };
            Global.StoreData();
        }

        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Global.volSliderValues[0]);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Global.volSliderValues[1]);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Global.volSliderValues[2]);*/

        IM = (AudioStreamPlayer)GetTree().Root.GetNode("IntroMusic");
		if (!IM.Playing)
		{
			IM.Play();
		}

		spinner = (Sprite2D)GetNode("Spinner");

		titleLeft = (Sprite2D)GetNode("LogoLeft");
		titleRight = (Sprite2D)GetNode("LogoRight");
		startingTitleY = titleLeft.GlobalPosition.Y;

		swordArt = (Sprite2D)GetNode("SwordArt");
		startingSwordY = swordArt.Position.Y;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//4 and not 2 because the swordArt takes half of the loop as the input to oscillate slower
		if (loop <= 4 * Mathf.Pi) {
			loop += 8f * (float)GetProcessDeltaTime();
		} else {
			loop = loop % (4*Mathf.Pi);
		}

		spinner.Rotation += spinVal * (float)GetProcessDeltaTime();

		titleLeft.Position = new Vector2(titleLeft.Position.X, startingTitleY + Mathf.Sin(loop)*7.5f);
		titleRight.Position = new Vector2(titleRight.Position.X, startingTitleY + Mathf.Sin(loop - Mathf.Pi/4)*7.5f);

		swordArt.Position = new Vector2(swordArt.Position.X, startingSwordY + Mathf.Cos(loop / 2) * 10f);
	}
}
