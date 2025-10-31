using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

public partial class match : WorldBase
{
    public enum MatchMap
    {
        Stadium,
        Truck,
        Ship,
        SpaceStation,
        City
    }
    public MatchMap map;
    public static MatchMap[] indexableMaps = { MatchMap.Stadium, MatchMap.Truck, MatchMap.Ship, MatchMap.SpaceStation, MatchMap.City };
    //match is kinda the class for eveything local

    // Called when the node enters the scene tree for the first time.
    //In seconds. Add 1 so it starts at, say 5:00 and not 4:59
    Timer t;
    [Export]
    float matchTime = 0;
    private Label timerLabel;
    //used for the timer at the end right before transition
    [Export]
    public TileMapLayer tm;
    private bool end = false;
    private string endCause = "";
    Timer te;
    public List<playerBase> players = new();
    public List<playerBase> activePlayers = new();
    public playerBase authorityPlayer; //for this client
    public List<PlayerToolUniv> tools = new();
    private Label[] pLabels;
    [Export]
    public bool freeze = false;
    public int playerCount = 2;
    [Export]
    public Node2D CBUL;
    [Export]
    public Node2D CBLR;
    [Export]
    public DebugTml dTML;
    [Export]
    public bool tutorial = false;
    public bool isLowGravity = false;
    public float playerMaxHealths = 3600;
    public float playerCurrentHealths = 3600;
    public List<Area2D> swordAreas = new();
    public bool paused = false;
    playerBase w;
    public delegate void AfterReadyEventHandler();
    public event AfterReadyEventHandler AfterReadyEvent;
    public host_join hj;
    [Export]
    public CanvasLayer UI;
    [Export]
    public MultiplayerSynchronizer ArenaMS;

    //to be used as a constructor, because I dont trust their implementation
    //(getting and setting already crashes Godot upon start)
    public void SetUpMatch(float ti, int p, float mH, float cH, MatchMap m)
    {
        t = new Timer(ti, true, this);
        playerCount = p;
        playerMaxHealths = mH;
        playerCurrentHealths = cH;

        map = m;
    }

    int[] pointCounts = { 0, 0, 0, 0 };
    //this really should to be synced from the player
    //TODO: move point incrementation(?) system, AND METHOD CALLS, to playerBase
    public void IncrementPointForPlayer(int amt, int pInd, string reason)
    {
        //GD.Print("Incremented player " + (pInd + 1) + "'s points by " + amt + " because \"" + reason + "\"");
        //pointCounts[pInd] += amt;
        players[pInd].points += amt;
    }

    public override void _Ready()
    {
        t = new Timer(301, true, this);
        te = new Timer(3, false, this);

        t.TimerFinished += MatchTimerFinishedEventHandler;
        te.TimerFinished += MatchEndTimerFinishedEventHandler;

        ((AudioStreamPlayer)GetTree().Root.GetNode("IntroMusic")).Stop();

        //time = matchTime;
        timerLabel = (Label)GetNode("UI/TimerLabel");

        pLabels = new Label[] { (Label)UI.GetNode("P1Label"), (Label)UI.GetNode("P2Label"), (Label)UI.GetNode("P3Label"), (Label)UI.GetNode("P4Label") };

        playerCount = players.Count;
        this.CallDeferred("CallAfterReady");
        tileMap = tm;
        ArenaMS.SetMultiplayerAuthority(1);
    }
    private void CallAfterReady()
    {
        AfterReadyEvent?.Invoke();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (pLabels[i].Visible == false)
            {
                pLabels[i].Visible = true; //TODO: find out what originally causes this
            }
            if (players[i].dead)
            {
                pLabels[i].Text = players[i].name + "- DEAD!";
            } else
            {
                if (players[i].health <= 0)
                {
                    players[i].dead = true; //for syncing purposes
                    PlayerDies(players[i]);
                }
                

                pLabels[i].Text = players[i].name + "\nhealth: " + Mathf.RoundToInt(players[i].health);
                pLabels[i].Text += "\nScore: " + pointCounts[i];
            }
        }
        //shows player1 winning (maybe)
        if (Input.IsActionJustPressed("endMatchTest"))
        {
            EndGame("DEBUG", players[0]);
        }
        if (!end)
        {
            matchTime = t.timerVal;
            timerLabel.Text = Mathf.Floor((t.threshHold - t.timerVal) / 60) + ":";
            if ((t.threshHold - t.timerVal) % 60 < 10)
            {
                timerLabel.Text += "0";
            }
            timerLabel.Text += Mathf.Floor((t.threshHold - t.timerVal) % 60);
        } else if (!(te.active || te.done))
        {
            /*for the short time between when it says END and really ends
            I know you're thinking about it, CMTacoTophat, but DON'T. don't move this to the timer section
            so if the timer goes 3 seconds over it activates. That is clever, but this is also the case if one player
            is killed, not just if time runs out*/
        
            te.Start();
        }

    }
    public void MatchTimerFinishedEventHandler()
    {
        EndGame("TIME!", players[0]);
    }
    public void MatchEndTimerFinishedEventHandler()
    {
        PackedScene podiums = (PackedScene)GD.Load("res://Screens/victory_screen.tscn");
        victory_screen inst = (victory_screen)podiums.Instantiate();
        inst.m = this;
        inst.SetUpVictoryScreen(w, playerCount, pointCounts, this, endCause);

        Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
        hj.peer.Close();

        GetTree().Root.AddChild(inst);
    }

    //why do i need this??? stupid c#
    private void EndGame(string cause, playerBase winner)
    {
        timerLabel.Text = cause;

        if (winner != null)
        {
            w = winner;
            endCause = "win";
        }
        else if (cause.Equals("DEBUG?"))
        {
            GD.Print("ended match with debug");
            endCause = "debug";
        }
        else if (cause.Equals("TIME!"))
        {
            GD.Print("match ended from time");
            endCause = "time";
        }
        else
        {
            GD.Print("No cause? (match.cs @ EndGame())");
            endCause = "unknown";
        }

        //Wait for 2-3 seconds
        end = true;
        //Global.StoreData();
    }

    //called by event
    public void PlayerDies(playerBase player) //womp womp
    {
        activePlayers.Remove(player);
        if (activePlayers.Count == 1)
        {
            freeze = true;
            EndGame("MATCH", activePlayers[0]);
        }
    }

    

}
