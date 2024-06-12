using Godot;
using System;
using System.Collections.Generic;

public partial class victory_screen : Node2D
{
	public match m;

	//Each player has a set of achievements they can make each round. By making them static, 
	//They can be changed elsewhere, not even in the scene, but can be accessed here
	//(they also don't need to be saved between sessions, so making them false upon initialization is fine)
	public static int achNumber = 5;
	public static string[] achievementNames = {"Close Call", "No Shield", "No Fist", "No Coin", "DEBUG"};
	//exp per achievement
	private int[] exp = {100, 150, 150, 50, 0};
	private int matchPoints = 0;
	//a dictionary as an array. What a time we live in.
	public Dictionary<string, bool>[] playerAchs;
	public bool tie = false;
	public playerBase winner;
	public int winnerNum;

	#nullable enable
	public void SetUpVictoryScreen(Dictionary<string, bool>[] p, playerBase? w, int wN, match ma) {
	#nullable disable
		playerAchs = p;
		winner = w;
		winnerNum = wN;
		m = ma;
		
		SetPlayerText(p1L, 1);
		SetPlayerText(p2L, 2);
		SetPlayerText(p3L, 3);
		SetPlayerText(p4L, 4);

		winnerLabel.Text = CreateWinners(winner);

		//reset their scores (after round ends)
		SetAchToEmpty();
	}

	Label p1L;
	Label p2L;
	Label p3L;
	Label p4L;
	Label winnerLabel;

	AnimatedSprite2D pVAnim;

	
	public override void _Ready()
	{

		p1L = (Label)GetNode("Player1AchLabel");
		p2L = (Label)GetNode("Player2AchLabel");
		p3L = (Label)GetNode("Player3AchLabel");
		p4L = (Label)GetNode("Player4AchLabel");

		winnerLabel = (Label)GetNode("VictorLabel");

		pVAnim = (AnimatedSprite2D)GetNode("PlayerVictorySprite");
		//((ShaderMaterial)(pVAnim.Material)).SetShaderParameter("a", Global.colors[winnerNum]);

		tie = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void SetPlayerText(Label label, int playerNum) {
		playerNum--;

		string text = "";

		for (int i = 0; i < achNumber; i++) {
			//accesses the bool for each achievement in the particular player's dictionary
			if (playerAchs[playerNum][achievementNames[i]]) { //error here
				text = text + achievementNames[i] + "\n";
				matchPoints += exp[i];
			}
		}
		
		if (text != "") {
			label.Text = "Player" + playerNum + " (" + matchPoints + ")\nAchivements:\n" + text;
		} else {
			label.Text = "Player" + playerNum + " (" + matchPoints + ")\nAchivements:\nNone (get better, bozo)";
		}
		
		if (playerNum - 1 == GameManager.ids.IndexOf(Multiplayer.GetUniqueId())) {
			//Global.SetWonOrLost(winnerNum == playerNum, matchPoints); //if this method is being called by the person
																	  //who did win, set their data
		}
	}
	public void SetAchToEmpty() {
		foreach (Dictionary<string, bool> d in playerAchs) {
			for (int i = 0; i < achNumber; i++) {
				d[achievementNames[i]] = false;
			}
		}
	}
	public void InitAch (out Dictionary<string, bool>[] a) {
		if (m.playerCount == 0) {
			m.playerCount = 2; //default
		}
		a = new Dictionary<string, bool>[m.playerCount];

		for (int i = 0; i < m.playerCount; i++) {
			//create new dict for each player
			a[i] = new Dictionary<string, bool>();
			for (int j = 0; j < achNumber; j++) {
				//make each dict full of the ach's and falses
				a[i].Add(achievementNames[j], false);
			}
		}
	}

	#nullable enable
	public string CreateWinners (playerBase? w) {
	#nullable disable
		string text = "";
		
		if (w != null)
		{
			text = "VICTORY TO " + w.name;
		}

		return text;
	}
}
