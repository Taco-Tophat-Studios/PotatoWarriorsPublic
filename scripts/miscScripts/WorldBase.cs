using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WorldBase : Node2D
{
	[Export]
	public TileMapLayer tileMap; //NOTE: all tile methods use this one
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    //And now, without further ado, CMTacoTophat's guide to finding tiles!

    //(Seriously, though, why did I name this stuff so badly)
    public Vector2I[] FindNormalsOfTiles(Vector2I[] checkingTiles, bool checkAll = false) {
        Vector2I[] normsToCheck = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };
        Vector2I[] returnedNorms = { Vector2I.Zero, Vector2I.Zero, Vector2I.Zero, Vector2I.Zero };

        for (int i = 0; i < 4; i++) {
            if (FindNumTilesWithNormal(normsToCheck[i], checkingTiles, checkAll) > 0) {
                returnedNorms[i] = normsToCheck[i];
            }
        }
        return returnedNorms;
    }

	public int FindNumTilesWithNormal(Vector2I normal, Vector2I[] checkingTiles, bool checkAll = false)
	{
		return FindTilesWithNormal(normal, checkingTiles, checkAll).Length;
	}

	public Vector2I[] FindTilesWithNormal(Vector2I normal, Vector2I[] checkingTiles, bool checkAll = false)
	{
		List<Vector2I> foundTiles = new List<Vector2I>();

		if (checkingTiles == null || checkingTiles.Length == 0)
		{
			if (!checkAll)
			{
                if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:tiles_none"]) { GD.Print("no tiles to check (match.cs @ FindTilesWithNormal)"); }
				return Array.Empty<Vector2I>();
			}
			checkingTiles = tileMap.GetUsedCells().ToArray();
		}

		Vector2I checkTile;
		foreach (Vector2I t in checkingTiles)
		{
			checkTile = t + normal;
			if (tileMap.GetCellAtlasCoords(checkTile).Equals(new Vector2I(-1, -1)) && tileMap.GetCellTileData(t).GetCollisionPolygonsCount(0) > 0)
			{
				//if the adjacent tile (by normal) is air/empty:
				foundTiles.Add(t);
			}
		}

		return foundTiles.ToArray();
	}

	public Vector2I[] FindIntersectingTiles(CollisionPolygon2D col, float angle = 0)
	{
		if (col.Polygon == null || col.Polygon.Length == 0)
		{
			GD.Print("no pointes (match.cs @ FindIntersectingTiles, polygon version)");
			return Array.Empty<Vector2I>();
		}
		/*for (int i = 0; i < colPoints.Length; i++)
        {
            colPoints[i] = colPoints[i].Rotated(angle);
        }*/
		ConcavePolygonShape2D shape = new();
		shape.SetSegments(col.Polygon);
		//GD.Print("angle: " + angle + " | points: " + shape.Segments.Join(", ") + "\n\n original: " + colPoints.Join(", "));
		Transform2D rotatedTrans = col.Transform.Rotated(angle);
		return FindIntersectingTiles(shape, rotatedTrans, col.GlobalPosition);
	}
    public Vector2I[] FindIntersectingTiles(CollisionShape2D col, float angle = 0)
    {
        Transform2D rotatedTrans = col.Transform.Rotated(angle);
        return FindIntersectingTiles(col.Shape, rotatedTrans, col.GlobalPosition);
    }
    private Vector2I[] FindIntersectingTiles(Shape2D shape, Transform2D colTrans, Vector2 colGP, bool arcMode = false)
    {
        colTrans.Origin = colGP;
        List<Vector2I> intersectingTiles = new List<Vector2I>();
        //col.Shape.Collide
        Vector2I center = tileMap.LocalToMap(tileMap.ToLocal(colGP));
        Vector2I check = center;
        if (!tileMap.GetCellAtlasCoords(check).Equals(new Vector2I(-1, -1)))
        {
            //add initial so radius can start at 1 and not be weird
            intersectingTiles.Add(center);
        }

        bool foundTileInSpiralRadius = true;
        int radius = 1;

        //check the tiles in a spiral pattern, starting at the center and growing outwards
        while (foundTileInSpiralRadius)
        {
            foundTileInSpiralRadius = false;
            for (int i = 0; i < 4; i++)
            {
                for (int theta = 0; theta < radius * 2; theta++)
                {
                    Vector2 preCheck = new Vector2(radius, -radius + theta).Rotated(Mathf.Pi / 2 * i);
                    check = new Vector2I(Mathf.RoundToInt(preCheck.X), Mathf.RoundToInt(preCheck.Y)) + center;

                    if (tileMap.GetCellAtlasCoords(check).Equals(new Vector2I(-1, -1)))
                    {
                        //if the tile is not empty
                        continue;
                    }

                    //fake a tile shape because godot internalizes LITERALLY EVERYTHING and wont let me get the DAMN COLLISION SHAPE for "pErFoRmAnCe ReAsOnS" :middle_finger:
                    CollisionShape2D tileShape = new CollisionShape2D()
                    {
                        Shape = new RectangleShape2D()
                        {
                            Size = tileMap.TileSet.TileSize
                        }
                    };
                    tileShape.GlobalPosition = tileMap.ToGlobal(tileMap.MapToLocal(check));
                    tileShape.Scale = tileMap.Scale;
                    //GD.Print("transforms: " + colTrans + " | " + tileShape.Transform);
                    if (shape.Collide(colTrans, tileShape.Shape, tileShape.Transform))
                    {
                        intersectingTiles.Add(check);
                        //GD.Print("added tile at " + check);
                        foundTileInSpiralRadius = true;
                    }
                }
            }
            radius++;
        }
        //GD.Print("tiles: " + intersectingTiles.ToArray().Join(", "));
        return intersectingTiles.ToArray();
    }


    //Here's the one for the tools
    public Vector2I[] FindIntersectingTilesFromArc(Vector2 arcCenter, float innerRad, float outerRad, float startAng, float endAng) {
        List<Vector2I> intersectingTiles = new List<Vector2I>();
        //col.Shape.Collide
        Vector2I center = tileMap.LocalToMap(tileMap.ToLocal(arcCenter));
        Vector2I check = center;
        if (!tileMap.GetCellAtlasCoords(check).Equals(new Vector2I(-1, -1)))
        {
            //add initial so radius can start at 1 and not be weird
            intersectingTiles.Add(center);
        }
        
        bool arcOverlap = startAng > endAng;
        Vector2 size = tileMap.TileSet.TileSize;

        //check the tiles in a spiral pattern, starting at the center and growing outwards
        for (int radius = 1; radius <= outerRad / Mathf.Max(size.X, size.Y) + 1; radius++)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int theta = 0; theta < radius * 2; theta++)
                {
                    Vector2 preCheck = new Vector2(radius, -radius + theta).Rotated(Mathf.Pi / 2 * i);
                    check = new Vector2I(Mathf.RoundToInt(preCheck.X), Mathf.RoundToInt(preCheck.Y)) + center;

                    if (tileMap.GetCellAtlasCoords(check).Equals(new Vector2I(-1, -1)))
                    {
                        //if the tile is not empty
                        continue;
                    }

                    Vector2 globalCheck = tileMap.ToGlobal(tileMap.MapToLocal(check));
                    Vector2[] tilePoints = new Vector2[4] {
                        globalCheck + tileMap.Scale*size,
                        globalCheck + tileMap.Scale*new Vector2(-size.X, size.Y),
                        globalCheck + tileMap.Scale*new Vector2(size.X, -size.Y),
                        globalCheck + tileMap.Scale*(-size)
                    };

                    Vector2 diff;
                    float diffAng;

                    bool InRadialRange = false;
                    bool InAngularRange = false; //if any of the points are in the angular range, set to true (not just bounds)

                    for (int j = 0; j < 4; j++)
                    {
                        diff = tilePoints[j] - arcCenter;
                        diffAng = NormalizeAngle(Mathf.Atan2(diff.Y, diff.X));

                        InRadialRange |= diff.Length() >= innerRad && diff.Length() <= outerRad;

                        InAngularRange |= arcOverlap ? (diffAng >= startAng || diffAng <= endAng) : (diffAng >= startAng && diffAng <= endAng);
                    }

                    if (InAngularRange && InRadialRange)
                    {
                        intersectingTiles.Add(check);
                        //GD.Print("added tile at " + check);
                    }
                }
            }
        }
        //GD.Print("tiles: " + intersectingTiles.ToArray().Join(", "));
        return intersectingTiles.ToArray();
    }

    public Vector2I[] FilterTilesPositionally(Vector2I[] tiles, Func<Vector2I, bool> filterFunc)
    {
        List<Vector2I> filteredTiles = new List<Vector2I>();
        foreach (Vector2I tile in tiles)
        {
            if (filterFunc(tile))
            {
                filteredTiles.Add(tile);
            }
        }
        return filteredTiles.ToArray();
    }
    //why do i need to do this bruhhh
    public static float NormalizeAngle(float angle)
    {
        return (angle + 2 * Mathf.Pi) % (2 * Mathf.Pi);
    }
}
