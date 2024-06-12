using Godot;
using System;
using System.Collections.Generic;

public partial class match : Node2D
{
	//match is essentially the class for eveything local

	// Called when the node enters the scene tree for the first time.
	//In seconds. Add 1 so it starts at, say 5:00 and not 4:59
	victory_screen v;
	Timer t;
	private Label timerLabel;
	//used for the timer at the end right before transition
	private bool end = false;
	Timer te;
	[Export]
	public bool coining = false;
	public List<playerBase> players = new List<playerBase>();
	private Label[] pLabels;
	[Export]
	public bool freeze = false;
	
	public int playerCount = 2;
	private Dictionary<string, bool>[] achs = new Dictionary<string, bool>[] {};
	//doesn't do anything so far
	public bool coinAllowed = true;

	public float playerMaxHealths = 3600;
	public float playerCurrentHealths = 3600;
	public List<Area2D> swordAreas = new List<Area2D>();
	public bool paused = false;

	playerBase w;

	public delegate void AfterReadyEventHandler();
	public event AfterReadyEventHandler AfterReadyEvent;
	public host_join hj;

	//to be used as a constructor, because I dont trust their implementation
	//(getting and setting already crashes Godot upon start)
	public void SetUpMatch(float ti, int p, bool c, float mH, float cH) {
		t = new Timer(ti, true, this);
		playerCount = p;
		coinAllowed = c;
		playerMaxHealths = mH;
		playerCurrentHealths = cH;
		foreach (playerBase pl in players) {
			pl.SetHealths(playerMaxHealths, playerCurrentHealths);
		}
		
	}

	public override void _Ready()
	{
		t = new Timer(301, true, this);
		te = new Timer(3, false, this);

		t.TimerFinished += MatchTimerFinishedEventHandler;
		te.TimerFinished += MatchEndTimerFinishedEventHandler;

		((AudioStreamPlayer)GetTree().Root.GetNode("IntroMusic")).Stop();

        v = new victory_screen
        {
            m = this
        };

        //time = matchTime;
        timerLabel = (Label)GetNode("UI/TimerLabel");
	
		pLabels = new Label[] {(Label)GetNode("UI/P1Label"), (Label)GetNode("UI/P2Label"), (Label)GetNode("UI/P3Label"), (Label)GetNode("UI/P4Label") };

		playerCount = players.Count;

		//initialize each achievement dictionary
		v.InitAch(out achs);

		this.CallDeferred("CallAfterReady");
	}
	private void CallAfterReady() {
		AfterReadyEvent?.Invoke();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		for (int i = 0; i < players.Count; i++) {
			pLabels[i].Text = players[i].name + "'s health: " + Mathf.RoundToInt(players[i].health); //HERE
		}
		//shows player1 winning
		if(Input.IsActionJustPressed("endMatchTest")) {
			achs[0]["DEBUG"] = true;
			EndGame("DEBUG", null);
		}
		if (!end) {
			timerLabel.Text = Mathf.Floor((t.threshHold - t.timerVal) / 60) + ":";
			if ((t.threshHold - t.timerVal) % 60 < 10)
			{
				timerLabel.Text += "0";
			}
			timerLabel.Text += Mathf.Floor((t.threshHold - t.timerVal) % 60);
		}
		
		/*for the short time between when it says END and really ends
		*/
		if (end && !te.active) { 
			te.Start();
		} 

	}
	public void MatchTimerFinishedEventHandler() { //mfw
		EndGame("TIME!", null);
	}
	public void MatchEndTimerFinishedEventHandler() {
		PackedScene arena = (PackedScene)GD.Load("res://Screens/victory_screen.tscn");
		Node2D inst = (Node2D)arena.Instantiate();
		GetTree().Root.AddChild(inst);

		v = (victory_screen)GetTree().Root.GetNode("VictoryScreen");

		v.SetUpVictoryScreen(achs, w, players.IndexOf(w) + 1, this);

		Node2D arenaWorld = (Node2D)GetNode("../World");

		arenaWorld.Hide();
		arenaWorld.QueueFree();
	}

	#nullable enable
	private void EndGame(string cause, playerBase? winner)
	{
	#nullable disable
		timerLabel.Text = cause;
		
		v.m = this;

		if (winner != null) {
			w = winner;
		} else if (cause.Equals("DEBUG")) {
			v.tie = true;
			GD.Print("ended match with debug");
		} else if (cause.Equals("TIME!")) {
			v.tie = true;
			GiveCloseCall();
		} else {
			GD.PrintErr("No cause? (match.cs @ EndGame())");
		}

		for (int i = 0; i < players.Count; i++) {
			//NOTE: Clean this mess up
			if (!players[i].hasUsedShield) {
				achs[i]["No Shield"] = true;
			}
			if (!players[i].hasUsedFist) {
				achs[i]["No Fist"] = true;
			}
			if (!players[i].hasUsedCoin) {
				achs[i]["No Coin"] = true;
			}
		}

		//achs should be a reference variable anyway, but just in case...
		v.playerAchs = achs;

		//Wait for 2-3 seconds
		end = true;
		//Global.StoreData();
		
	}
	private void GiveCloseCall() {
        for (int j = 0; j < players.Count; j++)
        {
            if (players[j].health <= players[j].startingHealth * 0.1f && !achs[j]["Close Call"])
            {
                //getting an element with a string? Can they do that?
                achs[j]["Close Call"] = true;
                GD.Print(j);
            }
        }
    }
	
	//called by event
	public void PlayerDies(playerBase player) //womp womp
	{
		players.Remove(player);
		if (players.Count == 1)
		{
			freeze = true;
			EndGame("MATCH", players[0]);
		}
	} 
	
}
