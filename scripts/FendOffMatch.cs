using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FendOffMatch : WorldBase
{
    [Export]
    public bool spawnEnemies;
    [Export]
    public Node2D CBUL;
    [Export]
    public Node2D CBLR;

    [Export]
    public singlePlayerBase potato;
    [Export]
    public TileMapLayer tm;
    public Vector2I usedCells;
    private Timer tT;
    public float time = 0;
    private Random r;
    public Vector2I tilesULBound;
    public Vector2I tilesLRBound;
    public List<Node2D> TransponderLocations;
    public List<bool> TransponderAvailable;
    public List<Transponder> Transponders;
    public List<enemyBase> enemies = new();
    bool tilesAlreadyExist = false; //to keep track of the above being initialized, because it has to in the children due to the fact that Godot can't take a freaking hint to call the parents classes' ready methods before the childrens'
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        r = new Random();

        TransponderLocations = new List<Node2D>();
        TransponderAvailable = new List<bool>();
        Transponders = new List<Transponder>();

        tT = new Timer(Global.RandFloat(10, 20), true, this);

        tT.TimerFinished += CreateTransponder;

        Godot.Collections.Array<Node> TLs = GetNode("TransponderSpawns").GetChildren();
        foreach (Node n in TLs)
        {
            TransponderLocations.Add((Node2D)n);
            TransponderAvailable.Add(true);
        }

        InitializeTileExistance();
        tileMap = tm;
    }
    public void InitializeTileExistance()
    {
        if (!tilesAlreadyExist)
        {
            int minX = 0;
            int minY = 0;
            int maxX = 0;
            int maxY = 0;
            foreach (Vector2I t in tm.GetUsedCells())
            {
                if (t.X < minX) {
                    minX = t.X;
                } else if (t.X > maxX) {
                    maxX = t.X;
                }
                if (t.Y < minY) {
                    minY = t.Y;
                } else if (t.Y > maxY) {
                    maxY = t.Y;
                }
            }
            tilesULBound = new Vector2I(minX, minY);
            tilesLRBound = new Vector2I(maxX, maxY);
            tilesAlreadyExist = true;
        }
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public Vector2I GCTTC(Vector2 pos) //global coords to tile coords
    {
        return new Vector2I((int)Mathf.Floor(pos.X / 64), (int)Mathf.Floor(pos.Y / 64));
    }

    public void PlayerDies()
    {

    }

    public void CreateTransponder()
    {
        if (spawnEnemies)
        {
            int rand;
            for (int i = 0; i < TransponderLocations.Count; i++)
            {
                rand = r.Next(0, TransponderLocations.Count);
                if (TransponderAvailable[rand])
                {
                    PackedScene newTransponder = GD.Load<PackedScene>("res://Objects/transponder.tscn");
                    Transponder t = newTransponder.Instantiate<Transponder>();
                    t.FOM = this;
                    t.GlobalPosition = TransponderLocations[i].GlobalPosition;
                    Transponders.Add(t);

                    AddChild(t);
                    t.initializeTimer();

                    TransponderAvailable[rand] = false;

                    tT.Start(); //DONT check if there are available locations and stop timer, because the player could destroy one and open a location after the check and timer stop, softlocking
                    //if transponder spots are available, a new transponder is created
                    return;
                }
                //if no transponders are available, control leaves method
            }
            tT.Start();
        }
    }
}
