using Godot;
using System;

public partial class DebugTml : TileMapLayer
{
	[Export]
	private TileMapLayer tm;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		try
		{
			GlobalPosition = tm.GlobalPosition;
		}
		catch (NullReferenceException e)
		{
			GD.Print("yo, no tilemap (DebugTml.cs): " + e.Message);
		}
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void HighlightTiles(Vector2I[] tiles, Vector2I aC, bool clear = true)
	{
		if (clear)
		{
			Clear();
		}
		if (tiles.Length == 0)
		{
			//GD.Print("nnoo ttiilleess ttoo hhiigghhlliigghhtt (DebugTml.cs @ HighLightTiles).");
			return;
		}
		foreach (Vector2I tile in tiles)
		{
			SetCell(tile, 1, aC, 0);
		}
	}
}
