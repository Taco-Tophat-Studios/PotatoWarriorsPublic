using Godot;
using System;
using System.Collections.Generic;

public partial class Transponder : Area2D
{
	public float Health = 100;
	public Timer EnemyTimer;
	public FendOffMatch FOM;

	private int[] enemyWeights = {1};
	private int totalEW = 0;
	private enemyBase[] enemySelectionForSpawn;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		foreach (int i in enemyWeights)
		{
			totalEW += i;
		}
		enemySelectionForSpawn = new enemyBase[enemyWeights.Length];
		enemySelectionForSpawn[0] = GD.Load<PackedScene>("res://Characters/soldier.tscn").Instantiate<Soldier>();
	}
	public void initializeTimer()
	{
		EnemyTimer = new Timer(Global.RandFloat(5, 10), true, this);
		EnemyTimer.TimerFinished += SpawnEnemy;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SpawnEnemy()
	{
		Random r = new();
		int temp = r.Next(0, totalEW + 1);

		//go through each enemy weight
		for (int i = 0; i < enemyWeights.Length; i++)
		{
			//if the random picker value is subtracted and still positive (before range or picked enemy) continue
			//if RPV is sub. and negative, it is in range of picked enemy and that enemy is chosen
			temp -= enemyWeights[i];
			if (temp < 0)
			{
				//eventually change to make them fall in with parachute
				enemySelectionForSpawn[i].GlobalPosition = this.GlobalPosition; //doesn't matter because it will be reset next time around
				FOM.AddChild(enemySelectionForSpawn[i]);
			}
		}
	}
}
