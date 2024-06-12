using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class PlayButton : buttonBaseClass
{
	Node2D lobby;
	string[] scenes = new string[] {"res://arenas/arena1.tscn", "res://arenas/arena2.tscn", "res://arenas/arena3.tscn", "res://arenas/arena4.tscn", "res://arenas/arena5.tscn"};
	public ItemList arenaPicker;
	public int pCount = 1;
	public SpinBox time;
	public SpinBox health;
	public SpinBox health2;
	public CheckBox coin;
	private bool coinIsAllowed = false;
	private host_join hj;

	//use this to store the match stuff, then access this from playbutton to assign the stuff from it to the
	//actual match class
	public match SetUpMatch;
	public match m;
	public bool[] playerReadys;

	private MatchSettings ms;
	public override void _Ready()
	{
		playerReadys = new bool[4] {false, false, false, false};
		lobby = (Node2D)GetNode("../../../Lobby");
		arenaPicker = (ItemList)GetNode("../MatchSettings/ArenaPicker");
		time = (SpinBox)GetNode("../MatchSettings/TimeCount");
		health = (SpinBox)GetNode("../MatchSettings/HealthCount");
		health2 = (SpinBox)GetNode("../MatchSettings/HealthCount2");
		coin = (CheckBox)GetNode("../MatchSettings/CoinAllow");
		hj = (host_join)GetNode("../HostJoinNode");
		ms = (MatchSettings)GetNode("../MatchSettingsButton");
	}
	public void StartGame(long selfId, int rAS)
	{
		if (PlayersReady() && !ms.vis)
		{
			scene = "res://arenas/arena1.tscn";
			if (arenaPicker.GetSelectedItems().Length == 0)
			{
				arenaPicker.Select(0);
			}

			if (GetTree().Root.HasNode("IntroMusic"))
			{
				GetTree().Root.GetNode("IntroMusic").QueueFree();
			}
			

			PackedScene arena;

			arena = (PackedScene)GD.Load(scenes[rAS]);
			
			Node2D inst = (Node2D)arena.Instantiate();
			GetTree().Root.AddChild(inst);

			m = (match)GetTree().Root.GetNode("World");

			PackedScene player = (PackedScene)GD.Load("res://player_1.tscn");
			playerBase pl;
			Vector2 defScale = new Vector2(0.5f, 0.5f);

			

			for (int i = 0; i < GameManager.players.Count; i++)
			{
				pl = (playerBase)player.Instantiate();
				pl.Name = "Player" + (i+1);
				pl.GlobalPosition = ((Node2D)m.GetNode("SpawnPoint" + (i+1))).GlobalPosition;
				pl.GlobalScale = defScale;
				pl.id = GameManager.ids[i];

				m.AddChild(pl);

				pl.SetCameraAsMain(Multiplayer.GetUniqueId());
			}
			
			//have these be the settings specified before
			m.SetUpMatch((float)time.Value * 60 /*bc its in minutes*/, pCount, coin.ButtonPressed, (float)health.Value, (float)health2.Value);
			m.hj = hj;

			hj.gameStarted = true;

			lobby.Hide();
            //lobby.QueueFree();
        }
		else if (ms.vis) {
			this.Text = "Close Settings!";
		} else {
			GD.Print("ERROR: Players not ready, slow down buckaroo!");
		}
	}
	private bool PlayersReady() {
		bool endVal = true;

		for (int i = 0; i < pCount - 1; i++) {
			endVal = endVal && playerReadys[i];
		}

		return endVal;
	}

	private void _on_player_1_ready_check_toggled(bool toggled_on)
	{
		LeButton(1, toggled_on);
	}
	private void _on_player_2_ready_check_toggled(bool toggled_on)
	{
        LeButton(2, toggled_on);
    }
	private void _on_player_3_ready_check_toggled(bool toggled_on)
	{
		LeButton(3, toggled_on);
	}
	private void _on_player_4_ready_check_toggled(bool toggled_on)
	{
		LeButton(4, toggled_on);
	}
	private void LeButton(int n, bool t) 
	{
		if (GameManager.ids.IndexOf(Multiplayer.GetUniqueId()) == n - 1) {
            hj.Sync("CheckButtons", n, t);
        } else
		{
			((CheckButton)GetNode("../Player" + n + "ReadyCheck")).SetPressedNoSignal(false);
		}
	}
}
