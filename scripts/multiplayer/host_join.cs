using Godot;
using System;
using System.Collections.Generic;

public partial class host_join : Node2D
{
	public bool gameStarted = false;

	[Export]
	private int port = 8913;
	[Export]
	private string address = "127.0.0.1"; //supposedly the "local host adapater" or whatever that means

	Control lc;

	Label cFLabel;
	Node2D hostJoinMenu;
	
	PlayButton pb;
	public ENetMultiplayerPeer peer;

	ENetConnection.CompressionMode CM = ENetConnection.CompressionMode.RangeCoder;

	List<ENetMultiplayerPeer> peers = new();

	Label[] playerLabels = new Label[4];
	public CheckButton[] playerChecks = new CheckButton[4];

	public long ownId;

    [Export]
    public LineEdit titleEdit;
    [Export]
    public Label titleLabel;
    [Export]
    public LineEdit codeEdit;
    [Export]
    public Label codeLabel;

	[Export]
	public LANMultiplayer LanInstance;

	private string chosenCode;

	public match m;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Show();

		lc = (Control)GetNode("../../LobbyControl");
		lc.Hide();

		cFLabel = (Label)GetNode("../../HostJoin").GetNode("CFLabel");
		hostJoinMenu = (Node2D)GetNode("../../HostJoin");

		pb = (PlayButton)GetNode("../PlayButton");

		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;


		for (int i = 0; i < 4; i++)
		{
			playerLabels[i] = (Label)GetNode("../Player" + (i + 1) + "Label");
			playerChecks[i] = (CheckButton)GetNode("../Player" + (i + 1) + "ReadyCheck");
			playerChecks[i].Visible = false;
		}

		this.TreeExiting += Disintegrate;
	}

	//SYNC METHODS -------------------------------------------------------------------------------------

	public void Sync(string method, params Variant[] p)
	{
		foreach (long i in GameManager.ids.ToArray())
		{
			RpcId(i, method, p);
		}
	}
	public void Sync(string method)
	{
		foreach (long i in GameManager.ids.ToArray())
		{
			RpcId(i, method);
		}
	}

	//CONNECTION METHODS -------------------------------------------------------------------------------------

	//runs only on client
	private void ConnectionFailed()
	{
		cFLabel.Text = "Connection Failed!";
		GD.Print("Failed to connect");
	}
	//runs only on client
	private void ConnectedToServer()
	{
		ownId = Multiplayer.GetUniqueId();

		GD.Print("succeeded to connect as " + ownId);
		lc.Show();
		hostJoinMenu.Hide();

		Global.LoadCharData();
		RpcId(1, "SendPlayerInformation", Global.name, Global.desc, Global.faceIndex, Global.swordIndex, Global.shieldIndex, Global.fistIndex, ownId, false); //why run it on the authority here and not declare it as Rpc.Authority? because I'm following some tutorial and they did this, even though they said the other way was good. I also trust this because supposedly, rpcmode.authority doesn't call locally, which is dumb as hell
	}
	//runs on all peers, id is who disconnected
	public void PeerDisconnected(long id)
	{
		GD.Print("Player " + id.ToString() + " disconnected");
		DeletePlayerInfo(id);

		playerBase p;

		//delete player in the game
		foreach (Node n in GetTree().GetNodesInGroup("Player"))
		{
			p = (playerBase)n;
			if (p.id == id)
			{
				p.m.players.Remove(p);
				n.QueueFree();
			}
		}
	}
	//runs on all peers, id is who disconnected
	private void PeerConnected(long id)
	{
		GD.Print("Player " + id.ToString() + " connected");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SendPlayerInformation(string n, string d, int faceI, int swordI, int shieldI, int fistI, int ID, bool r)
	{
		//create a new player, add to gamemanager's list, and if you're the server, tell the clients to do the same
		PlayerInfo pi = new()
		{
			name = n,
			desc = d,
			faceIndex = faceI,
			swordIndex = swordI,
			shieldIndex = shieldI,
			fistIndex = fistI,
			id = ID,
			ready = r
		};
		if (!GameManager.players.Contains(pi))
		{
			GameManager.players.Add(pi);
			GameManager.ids.Add(ID);
			pb.pCount++;
			UpdateLabels();
			pb.UpdateKickButtons();
			//GD.Print(GameManager.ToString());
		}
		//don't replace this because it needs each player's name, desc, etc, and not the id
		if (Multiplayer.IsServer() && ID != 1)
		{
			//send other players' info to the new player
			foreach (PlayerInfo p in GameManager.players)
			{
				RpcId(ID, "SendPlayerInformation", p.name, p.desc, p.faceIndex, p.swordIndex, p.shieldIndex, p.fistIndex, p.id, p.ready); //sends it back to the original, as this already calls for everybody except the guy who joined
			}
			//send new player's info to other players
			foreach (long i in GameManager.ids)
			{
				if (i != 1 && i != ID)
				{
					RpcId(i, "SendPlayerInformation", n, d, faceI, swordI, shieldI, fistI, ID, r);
				}
			}
		}
	}
	private void DeletePlayerInfo(long id)
	{
		int i = -1;
		if (GameManager.ids.Contains(id))
		{
			i = GameManager.ids.IndexOf(id);

			GameManager.players.RemoveAt(i);
			GameManager.ids.Remove(id);
		}

		if (!gameStarted)
		{
			pb.pCount--;
			UpdateLabels();
			pb.UpdateKickButtons();
		} else if (i > 0)
        {
			m.players.RemoveAt(i);
        }

	}

	//LOBBY MANAGEMENT METHODS -------------------------------------------------------------------------------------
	
	public void PreJoin(string code, out string err)
	{
		if (code == "")
		{
			GD.Print("ERROR: No code entered");
			err = "No code entered";
			return;
		}
		else if (LanInstance.GetServerIPFromCode(code) == "")
		{
			//GD.Print("ERROR: No server found with code " + code);
			cFLabel.Text = err = "No server has code " + code;
			return;
		} else
		{
			chosenCode = code;
			err = "";
		}
	}
	public void OnJoin(/*string code*/)
	{
		lc.Show();
		hostJoinMenu.Hide();
		LanInstance.bindStatusLabel.Hide();
		LanInstance.StopListening();

		peer = new ENetMultiplayerPeer();
		peer.CreateClient(LanInstance.GetServerIPFromCode(chosenCode), port);
		peer.Host.Compress(CM);

		Multiplayer.MultiplayerPeer = peer; //set ourself as a peer

		GD.Print("CLIENT STATUS: joined game");
		((Control)GetNode("../../TutorialButton")).Hide();

		titleLabel.Text = LanInstance.GetSIFromCode(chosenCode).serverName;
		codeLabel.Text = "Lobby Code: " + chosenCode;
	}
	public void OnHost()
	{
		//pb.pCount = 1; because sendplayerinformation is called, which increments pCount itself

		lc.Show();
		hostJoinMenu.Hide();
		LanInstance.bindStatusLabel.Hide();

		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, 4); //max of 4 players
		if (error != Error.Ok)
		{
			GD.Print("ERROR: Hosting is wacky (" + error.ToString() + ")");
			return;
		}
		peer.Host.Compress(CM);

		Multiplayer.MultiplayerPeer = peer; //set ourself as a peer

		GD.Print("LOBBY STATUS: Waiting for Players");
		//codeLabel.Text = "Codes have been removed";

		Global.LoadCharData();
		SendPlayerInformation(Global.name, Global.desc, Global.faceIndex, Global.swordIndex, Global.shieldIndex, Global.fistIndex, 1, false);

		((Control)GetNode("../../TutorialButton")).Hide();

		LanInstance.SetupBroadcast("<CUSTOM LOBBY NAME>", "No descriptions implemented yet. If you're seeing this then umm... um-uhhhh the uhh... hmm...");
		codeLabel.Text = "Lobby Code: " + LanInstance.SI.code;
	}
	
	//-------------------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------------------
	//TODO: merge PlayButton's UpdateKickButtons with this
	private void UpdateLabels() //essentially, intepret new Gamemanger info
	{
		//NOTE: should be compatible with both connection and disconnection
		//sideNOTE: Really jammin' out to https://youtu.be/FAIz9Ok4JSs right now
		PlayerInfo pi;
		for (int i = 0; i < 4; i++)
		{
			if (i < GameManager.players.Count)
			{
				pi = GameManager.players[i];
				playerLabels[i].Text = pi.name + "\n" + pi.desc;
				playerChecks[i].Visible = true;
				playerChecks[i].SetPressedNoSignal(pi.ready);
				pb.playerReadys[i] = pi.ready;
			}
			else
			{
				playerLabels[i].Text = "<Player " + (i + 1) + ">";
				playerChecks[i].Visible = false;
			}

		}
	}

	//MENU METHODS -------------------------------------------------------------------------------------

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void CheckButtons(int num, bool onOff)
	{
		playerChecks[num - 1].SetPressedNoSignal(onOff);
		pb.playerReadys[num - 1] = onOff;
		GameManager.players[num - 1].ready = onOff;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void KickPlayer(int playerNum) //WARNING: 1-BASED INDEX!
	{
		GD.Print("Received request to kick player " + playerNum + " in ids " + GameManager.ids.ToArray().Join(", "));
		if (playerNum < 1 || playerNum > GameManager.ids.Count)
		{
			GD.Print("ERROR: tried to kick nonexistant player " + playerNum);
			return;
		}
		if (GameManager.ids[playerNum - 1] == Multiplayer.GetUniqueId())
		{
			GD.Print("kicking YOU from the lobby");
			Multiplayer.MultiplayerPeer = null; //supposedly this force-disconnects you?
			lc.Hide();
			hostJoinMenu.Show();
			cFLabel.Text = "You have been kicked from the lobby. Skill issue.";
			GameManager.players.Clear();
			GameManager.ids.Clear();
			pb.pCount = 0;
			peer.Close();
		}
		else
		{
			/*GD.Print("kicking player " + GameManager.ids[playerNum] + " from the lobby (id " + GameManager.ids[playerNum] + ")");
			GameManager.players.RemoveAt(playerNum - 1);
			GameManager.ids.Remove(playerNum - 1);
			pb.pCount--;*/
			DeletePlayerInfo(GameManager.ids[playerNum - 1]);
		}
		UpdateLabels();
		pb.UpdateKickButtons();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartTheGame(int randomArenaSelector)
	{
		pb.StartGame(this.ownId, randomArenaSelector);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetMatchSettings(float t, float hv1, float hv2, int ai)
	{
		pb.time.SetValueNoSignal(t);
		pb.health.SetValueNoSignal(hv1);
		pb.health2.SetValueNoSignal(hv2);
		pb.arenaPicker.Select(ai);
	}

	//BUTTON METHODS -------------------------------------------------------------------------------------

	private void _on_play_button_button_down()
	{
		if (Multiplayer.IsServer() && !pb.ms.vis)
		{
			int sel = 0;
			if (pb.arenaPicker.GetSelectedItems()[0] == pb.arenaPicker.GetItemCount() - 1) //if the last item is selected, then select a random one
			{
				Random r = new();
				sel = r.Next(0, 5);
			}
			else
			{
				sel = pb.arenaPicker.GetSelectedItems()[0];
			}

			Sync("StartTheGame", sel);
		}
		else
		{
			((Button)GetNode("../PlayButton")).Text = "Sorry Bucko, but you're not the host!";
		}
	}
	
	public void _on_name_edit_text_submitted(string newTitle) {
        LanInstance.SI.serverName = newTitle.Trim();
        titleLabel.Text = newTitle.Trim();
	}

	public void _on_code_edit_text_submitted(string newCode)
	{
		if (int.TryParse(newCode.Trim(), out int n) == true && (n >= 0 && n <= 255))
		{
			codeLabel.Text = "Code cannot be a number between 0 and 255 for internal reasons.";
			return;
		}

		foreach (string c in LanInstance.GetActiveCodes())
		{
			if (c == newCode.Trim())
			{
				codeLabel.Text = "Code already in use!";
				return;
			}
		}
		//LANMultiplayer already replaces servers with same IP, so just change code and let it broadcast
		LanInstance.SI.code = newCode.Trim();
		GD.Print("Changed lobby code to " + newCode.Trim());
		codeLabel.Text = "Lobby Code: " + newCode.Trim();
	}
	
	public void Disintegrate()
	{
		GD.Print("aaauuuughghgh!!! (host_join.cs @ Disintegrate)");
		GameManager.players.Clear();
		GameManager.ids.Clear();
		Multiplayer.MultiplayerPeer = null;
		peer.Close();
    }
}
