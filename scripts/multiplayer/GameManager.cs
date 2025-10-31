using Godot;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public static List<PlayerInfo> players = new();
    public static List<long> ids = new();

    public static new string ToString()
    {
        string result = "GameManager State:\n";
        result += "Players:\n";
        for (int i = 0; i < players.Count; i++)
        {
            result += $"Player {i}---NAME: {players[i].name}, DESC: {players[i].desc}, READY: {players[i].ready}\n";
        }
        result += "IDs:\n";
        foreach (var id in ids)
        {
            result += id.ToString() + "\n";
        }
        return result;
    }
}
