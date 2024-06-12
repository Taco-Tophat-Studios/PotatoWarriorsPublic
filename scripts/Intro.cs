using Godot;
using System;

public partial class Intro : Node2D
{
	AnimatedSprite2D cardSprite;
	Timer timer;
	int stage = 1;
	float[] vals = new float[3] {3, 4, 3};
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		cardSprite = (AnimatedSprite2D)GetNode("IntroCard");
		timer = new Timer(vals[0], true, this);
		timer.TimerFinished += TFinished;

        /*Global.LoadCharData();
        if (Global.volSliderValues.IsEmpty())
        {
            Global.volSliderValues = new float[] { 0, 0, 0 };
            Global.StoreData();
        }

        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Global.volSliderValues[0]);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Global.volSliderValues[1]);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Global.volSliderValues[2]);*/
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (stage == 1) {
			cardSprite.Frame = Mathf.RoundToInt(20*(timer.timerVal / timer.threshHold));
		} else if (stage == 2) {
			cardSprite.Frame = 20;
		} else {
			cardSprite.Frame = Mathf.RoundToInt(20 - 20*(timer.timerVal / timer.threshHold));
		}
	}
	public void TFinished() {
		if (stage == 1) {
			timer.threshHold = vals[1];
			stage = 2;
			timer.Start();
		} else if (stage == 2) {
			timer.threshHold = vals[2];
			stage = 3;
			timer.Start();
		} else {
			GetTree().ChangeSceneToFile("res://Screens/Menus/MainMenu.tscn");
		}

	}
}
