using Godot;
using System;

public partial class ServerInfo
{
    public bool setup { get; set; } = false;
    //Browsables
    public string ip { get; set; } = ""; //set when found (for LAN, should be local IP)
    public string serverName { get; set; } = "Unnamed Server";
    public string serverDesc { get; set; } = "<No Description Provided>";
    public int playerCount { get; set; } = 1; //starts at 1 because host is always a player
    public bool lan { get; set; } = false; //if true, use LAN broadcasting (likely unneccessary)

    public string code { get; set; } = "";
    //no password (code based selection)

    //Playables
    public float startingHealth { get; set; } = 3000;
    public float maxHealth { get; set; } = 3600;
    public int matchIndex { get; set; } = 0;

    public float lifetime { get; set; } = 0; //for LAN broadcasting, time since last broadcast (for expiry)

    public ServerInfo()
    {
        setup = false;
    }

    public ServerInfo(string ip, string serverName, string serverDesc, int playerCount, bool lan)
    {
        this.ip = ip;
        this.serverName = serverName;
        this.serverDesc = serverDesc;
        this.playerCount = playerCount;
        this.lan = lan;

        setup = true;
    }

    public ServerInfo(float startingHealth, float maxHealth, int map)
    {
        this.startingHealth = startingHealth;
        this.maxHealth = maxHealth;
        this.matchIndex = map;

        setup = true;
    }

    public ServerInfo(string ip, string serverName, string serverDesc, int playerCount, bool lan,
                      float startingHealth, float maxHealth, int map)
    {
        this.ip = ip;
        this.serverName = serverName;
        this.serverDesc = serverDesc;
        this.playerCount = playerCount;
        this.lan = lan;
        this.startingHealth = startingHealth;
        this.maxHealth = maxHealth;
        this.matchIndex = map;

        setup = true;
    }
}
