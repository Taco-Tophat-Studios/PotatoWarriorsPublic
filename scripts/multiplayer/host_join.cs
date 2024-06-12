using Godot;
using System;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

public partial class host_join : Node2D
{
	public bool gameStarted = false;

	[Export]
	private int port = 9999;
	[Export]
	private string address = "127.0.0.1"; //supposedly the "local host adapater" or whatever that means

	Control lc;

	Label cFLabel;
	Node2D hostJoinMenu;
	Label codeLabel;
	PlayButton pb;

	ENetMultiplayerPeer peer;

	ENetConnection.CompressionMode CM = ENetConnection.CompressionMode.RangeCoder;

	List<ENetMultiplayerPeer> peers = new List<ENetMultiplayerPeer>();

	Label[] playerLabels = new Label[4];
	public CheckButton[] playerChecks = new CheckButton[4];

	public long ownId;
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		/*string[] ips = IP.GetLocalAddresses();
		GD.Print(ips[ips.Length - 1]);
		address = ips[ips.Length - 1];*/
		/*Upnp u =new Upnp();
		u.Discover();
		u.AddPortMapping(9999); */


		lc = (Control)GetNode("../../LobbyControl");
		lc.Hide();

		cFLabel = (Label)GetNode("../../HostJoin").GetNode("CFLabel");
		hostJoinMenu = (Node2D)GetNode("../../HostJoin");
		codeLabel = (Label)GetNode("../CodeLabel");

		pb = (PlayButton)GetNode("../PlayButton");
		pb.pCount--;

		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		

		for (int i = 0; i < 4; i++) {
			playerLabels[i] = (Label)GetNode("../Player" + (i + 1) + "Label");
			playerChecks[i] = (CheckButton)GetNode("../Player" + (i + 1) + "ReadyCheck");
			playerChecks[i].Visible = false;
		}

    }
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

		GD.Print("succeeded to connect as" + ownId);
		lc.Show();
		if (IsInstanceValid(hostJoinMenu))
		{
			hostJoinMenu.QueueFree();
		}

		//Global.LoadCharData();
		//RpcId(1, "SendPlayerInformation", Global.name, Global.desc, ownId); 
		
	}
	//runs on all peers, id is who disconnected
	public void PeerDisconnected(long id)
	{
		GD.Print("Player " + id.ToString() + " disconnected");
		DeletePlayerInfo(id);

		playerBase p;

		//delete player in the game
		foreach (Node n in GetTree().GetNodesInGroup("Player")) {
			p = (playerBase)n;
			if (p.id == id) {
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


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	/*public override void _Process(double delta)
	{
	}*/
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendPlayerInformation(string n, string d, int ID)
	{
		//create a new player, add to gamemanager's list, and if you're the server, tell the clients to do the same
		PlayerInfo pi = new PlayerInfo()
		{
			name = n,
			desc = d,
			id = ID
		};
		if (!GameManager.players.Contains(pi))
		{
			GameManager.players.Add(pi);
			GameManager.ids.Add(ID);
			pb.pCount++;
			UpdateLabels();
		}
        //don't replace this because it needs each player's name, desc, etc, and not the id
        if (Multiplayer.IsServer() && ID != 1)
        {
            foreach (PlayerInfo p in GameManager.players)
            {
				RpcId(ID, "SendPlayerInformation", p.name, p.desc, p.id); //sends it back to the original, as this already calls for everybody except the guy who joined
            }
			foreach (long i in GameManager.ids)
			{
				if (i != 1 && i != ID)
				{
					RpcId(i, "SendPlayerInformation", n, d, ID);
				}
			}
        }
	}
	private void DeletePlayerInfo(long id) {
		if (GameManager.ids.Contains(id))
		{
			int i = GameManager.ids.IndexOf(id);

			GameManager.players.RemoveAt(i);
			GameManager.ids.Remove(id);
		}
		
		if (!gameStarted)
		{
			pb.pCount--;
			UpdateLabels();
		}
		
	}

	private void UpdateLabels() {
        //NOTE: should be compatible with both connection and disconnection
        for (int i = 0; i < 4; i++)
		{
			if (i < GameManager.players.Count) {
				playerLabels[i].Text = GameManager.players[i].name + "\n" + GameManager.players[i].desc;
				playerChecks[i].Visible = true;

			} else {
				playerLabels[i].Text = "<Player " + (i + 1) + ">";
				playerChecks[i].Visible = false;
			}
			
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void CheckButtons(int num, bool onOff) {
		playerChecks[num - 1].SetPressedNoSignal(onOff);
		pb.playerReadys[num - 1] = onOff;
	}
	public void Sync(string method, params Variant[] p) {
		foreach (long i in GameManager.ids) {
			RpcId(i, method, p);
		}
	}
    public void Sync(string method)
    {
        foreach (long i in GameManager.ids)
        {
            RpcId(i, method);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartTheGame(int randomArenaSelector)
	{
        pb.StartGame(this.ownId, randomArenaSelector);
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetMatchSettings (float t, bool ca, float hv1, float hv2, int ai)
	{
		pb.time.SetValueNoSignal(t);
		pb.coin.SetPressedNoSignal(ca);
		pb.health.SetValueNoSignal(hv1);
		pb.health2.SetValueNoSignal(hv2);
		pb.arenaPicker.Select(ai);
	}

    //-------------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------

   
    public void OnJoin(string adr)
	{
		string ip =""; //= Global.DecryptIPAddress(adr);
		if (ip.Equals("ERROR"))
		{
			cFLabel.Text = "Code invalid!";
			return;
		}
		lc.Show();
		hostJoinMenu.QueueFree();
		

		peer = new ENetMultiplayerPeer();
		peer.CreateClient(ip, port);

		peer.Host.Compress(CM);

		Multiplayer.MultiplayerPeer = peer; //set ourself as a peer

		GD.Print("CLIENT STATUS: joined game");

        GetNode("../../TutorialButton").QueueFree();
    }
	public void OnHost()
	{
		pb.pCount = 1;

		lc.Show();
		hostJoinMenu.QueueFree();

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
		//codeLabel.Text = "Code:\n" + Global.EncryptIPAddress(address);

		//Global.LoadCharData();
		//SendPlayerInformation(Global.name, Global.desc, 1);

        GetNode("../../TutorialButton").QueueFree();

    }
	
	private void _on_play_button_button_down()
	{
		if (Multiplayer.IsServer())
		{
			int sel = 0;
			if (pb.arenaPicker.GetSelectedItems()[0] == 5)
			{
				Random r = new Random();
				sel = r.Next(0, 5);
			} else
			{
				sel = pb.arenaPicker.GetSelectedItems()[0];
			}
			
			Sync("StartTheGame", sel);
        } else
		{
			((Button)GetNode("../PlayButton")).Text = "Sorry Bucko, but you're not the host!";
		}	
	}
}


