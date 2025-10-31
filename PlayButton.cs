using Godot;

public partial class PlayButton : buttonBaseClass
{
    Node2D lobby;
    string[] scenes = new string[] { "res://arenas/arena1.tscn", "res://arenas/arena2.tscn", "res://arenas/arena3.tscn", "res://arenas/arena4.tscn", "res://arenas/arena5.tscn" };
    public ItemList arenaPicker;
    public int pCount = 0;
    public SpinBox time;
    public SpinBox health;
    public SpinBox health2;
    private host_join hj;
    private Node2D standardBG;

    //use this to store the match stuff, then access this from playbutton to assign the stuff from it to the
    //actual match class
    public match SetUpMatch; //no don't put it private, silly, why would you even do that?
    public match m;
    public bool[] playerReadys;

    public MatchSettings ms;
    public override void _Ready()
    {
        //TODO: make these all exports
        playerReadys = new bool[4] { false, false, false, false };
        lobby = (Node2D)GetNode("../../../Lobby");
        arenaPicker = (ItemList)GetNode("../MatchSettings/ArenaPicker");
        time = (SpinBox)GetNode("../MatchSettings/TimeCount");
        health = (SpinBox)GetNode("../MatchSettings/HealthCount");
        health2 = (SpinBox)GetNode("../MatchSettings/HealthCount2");
        hj = (host_join)GetNode("../HostJoinNode");
        ms = (MatchSettings)GetNode("../MatchSettingsButton");
        standardBG = (Node2D)GetTree().Root.GetNode("Lobby/StandardBG");
    }
    public void StartGame(long selfId, int rAS)
    {
        if (PlayersReady() && !ms.vis)
        {
            this.Text = "Starting Game...";
            scene = "res://arenas/arena1.tscn";

            if (GetTree().Root.HasNode("IntroMusic"))
            {
                GetTree().Root.GetNode("IntroMusic").QueueFree();
            }

            //actually start doing stuff
            PlayerInfo PI;

            PackedScene arena;

            arena = (PackedScene)GD.Load(scenes[rAS]);

            Node2D inst = (Node2D)arena.Instantiate();
            GetTree().Root.AddChild(inst);

            m = (match)GetTree().Root.GetNode("World");

            PackedScene player = (PackedScene)GD.Load("res://player_1.tscn");
            playerBase pl;
            Vector2 defScale = new(0.5f, 0.5f);

            //have these be the settings specified before
            m.SetUpMatch((float)time.Value * 60 /*bc its in minutes*/,
                            pCount,
                            (float)health.Value,
                            (float)health2.Value,
                            match.indexableMaps[rAS]);

            for (int i = 0; i < GameManager.players.Count; i++)
            {
                PI = GameManager.players[i];

                pl = (playerBase)player.Instantiate();
                pl.Name = "Player" + (i + 1); //this is the NODE name.
                pl.name = PI.name;
                pl.faceIndex = PI.faceIndex;
                pl.GlobalPosition = ((Node2D)m.GetNode("SpawnPoint" + (i + 1))).GlobalPosition;
                pl.GlobalScale = defScale;
                pl.id = GameManager.ids[i];
                pl.playerIndex = i;

                if (match.indexableMaps[rAS] == match.MatchMap.SpaceStation)
                {
                    pl.gravity = 300;
                    m.isLowGravity = true;
                }
                pl.toolIndices = new int[3] { PI.swordIndex, PI.shieldIndex, PI.fistIndex };
                m.AddChild(pl);
                pl.SetCameraAsMain(Multiplayer.GetUniqueId());
            }

            m.hj = hj;
            hj.m = m; //lol
            hj.gameStarted = true;
            standardBG.GetNode<Node2D>("bg/ParallaxLayer").Hide();
            //standardBG.Visible = false;qqq
            lobby.Hide();
            //lobby.QueueFree();

            hj.LanInstance.VanishIntoTheVoid();

        }
        else if (ms.vis)
        {
            this.Text = "Close Settings!";
        }
        else
        {
            GD.Print("ERROR: Players not ready, slow down buckaroo!");
        }
    }
    private bool PlayersReady()
    {
        bool endVal = true;

        for (int i = 0; i < pCount; i++)
        {
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
    private void LeButton(int n, bool t) //i'm completely out of ideas for check button related method names
    {
        if (GameManager.ids.IndexOf(Multiplayer.GetUniqueId()) == n - 1)
        {
            hj.Sync("CheckButtons", n, t);
        }
        else
        {
            ((CheckButton)GetNode("../Player" + n + "ReadyCheck")).SetPressedNoSignal(false);
        }
    }
    public void _on_player_N_kick_button_pressed(int n)
    {
        GD.Print("Attempting to kick player " + n);
        if (Multiplayer.IsServer()/*GameManager.ids.IndexOf(Multiplayer.GetUniqueId()) == 0*/) //only host can kick
        {
            hj.Sync("KickPlayer", n);
        }
        else
        {
            GD.Print("Nuh uh, you can't kick people!");
        }
    }
    public void UpdateKickButtons()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        // Keep pCount in sync with the authoritative list of players.
        pCount = GameManager.ids.Count;
        if (pCount > 4) pCount = 4;

        Button playerKicker;
        
        for (int i = 0; i < 4; i++)
        {
            playerKicker = (Button)GetNode("../Player" + (i + 1) + "KickButton");
            if (i != 0 && i < pCount) //do this check here, not in the loop condition, so it will hide the rest
            {
                playerKicker.Show();
            } else
            {
                playerKicker.Hide();
            }

        }
    }
}
