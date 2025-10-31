using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class victory_screen : Node2D
{
    Color[] playerColors = new Color[4] { new(1, 0, 0), new(0, 0, 1), new(1, 0, 1), new(0, 1, 0) };
    public match m;
    public playerBase winner;
    public int winnerNum;
    [Export]
    AnimatedSprite2D PlayerVictoryAnimation1;
    [Export]
    AnimatedSprite2D PlayerVictoryAnimation2;
    [Export]
    AnimatedSprite2D PlayerVictoryAnimation3;
    [Export]
    AnimatedSprite2D PlayerVictoryAnimation4;

    string cause;

    private AnimatedSprite2D[] VictorAnimArray;
    private PlayerInfo[] playerInfos = new PlayerInfo[4];
    private int[] pointsArr = new int[4];
    private Label[] vicLabels = new Label[3];
    [Export]
    Label WinnerLabel;
    int playerCount;

    public void SetUpVictoryScreen(playerBase W, int pC, int[] points, match ma, string eC)
    {
        GD.Print("UNO " + pC + " | " + points.Join(", ") + " | " + W.name + " | " + eC);
        m = ma;
        playerCount = pC;
        cause = eC;
        VictorAnimArray = new AnimatedSprite2D[4] { PlayerVictoryAnimation1, PlayerVictoryAnimation2, PlayerVictoryAnimation3, PlayerVictoryAnimation4 };
        foreach (AnimatedSprite2D a in VictorAnimArray)
        {
            ((ShaderMaterial)a.Material).SetShaderParameter("key", new Color(166f / 255f, 166f / 255f, 166f / 255f));
            a.Stop();
            a.Visible = false;
        }

        // RANKING SYSTEM
        winner = W;
        // Do NOT mutate the original points array. Create an index array sorted by points (descending).
        int[] sortedIndexes = Enumerable.Range(0, playerCount).ToArray();
        Array.Sort(sortedIndexes, (a, b) => points[b].CompareTo(points[a])); // sorts indexes by points descending

        GD.Print("Sorted indexes (desc): " + sortedIndexes.Join(", "));

        // ensure we have the right-size label array (exclude overall winner if cause == "win")
        int labelsCount = (cause == "win") ? Math.Max(0, playerCount - 1) : playerCount;
        vicLabels = new Label[labelsCount];
        for (int i = 0; i < labelsCount; i++)
        {
            vicLabels[i] = (Label)GetNode("Place" + (i + 2) + "Label");
        }

        if (cause == "win")
        {
            int winnerPoints = (W.playerIndex >= 0 && W.playerIndex < points.Length) ? points[W.playerIndex] : 0;
            WinnerLabel.Text = "VICTORY TO " + (m.authorityPlayer != null && m.authorityPlayer == W ? "YOU!" : W.name);
            // color the winner slot (slot 0 is winner)
            ((ShaderMaterial)VictorAnimArray[0].Material).SetShaderParameter("replace", playerColors[W.playerIndex]);
        }

        // fill labels by rank and color the victory sprites
        int labelIdx = 0;
        for (int rank = 0; rank < playerCount; rank++)
        {
            int pIndex = sortedIndexes[rank];
            string pname = (m.players != null && pIndex < m.players.Count) ? m.players[pIndex].name : ("Player " + (pIndex + 1));
            if (cause == "win" && rank == 0)
            {
                // already handled winner display above; skip adding a label for winner
            }
            else
            {
                if (labelIdx < vicLabels.Length)
                {
                    vicLabels[labelIdx].Text = m.authorityPlayer != null && m.authorityPlayer.playerIndex == pIndex ? "(YOU) " : pname;
                    labelIdx++;
                }
            }

            // color the animator corresponding to this rank (slot 0 = winner, 1 = 2nd, etc.)
            if (rank < VictorAnimArray.Length)
            {
                ((ShaderMaterial)VictorAnimArray[rank].Material).SetShaderParameter("replace", playerColors[pIndex]);
            }
            GD.Print("TRES " + pIndex + " | " + rank + " | " + pname + " | " + ((labelIdx < vicLabels.Length) ? vicLabels[Math.Max(0,labelIdx-1)].Text : "nuthin") + " | " + playerColors[pIndex]);
        }

        Global.LoadCharData();
        if (W == m.authorityPlayer)
        {
            Global.wonGames++;
        }
        else
        {
            Global.lostGames++;
        }
        Global.points += points[m.authorityPlayer.playerIndex];
        Global.StoreData();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Ready()
    {
        for (int i = 0; i < playerCount; i++)
        {
            VictorAnimArray[i].Visible = true;
            VictorAnimArray[i].Play("PlayerVic" + (i + 1));
        }

        // Defer freeing root nodes so we don't dispose the running match while its handler is still executing.
        var root = GetTree().Root;
        if (root.HasNode("Lobby"))
            root.GetNode("Lobby").CallDeferred("queue_free");
        if (root.HasNode("World"))
            root.GetNode("World").CallDeferred("queue_free");
    }
    public void TransitionToAnimLoop(int ind)
    {
        VictorAnimArray[ind].Play("VicLoop" + (ind + 1));
    }

}
