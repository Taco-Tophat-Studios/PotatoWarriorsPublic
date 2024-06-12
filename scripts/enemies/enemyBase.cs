using Godot;
using System;
using System.Collections.Generic;

public partial class enemyBase : CharacterBody2D
{
	protected singlePlayerBase player;
	protected enemyBase[] otherEnemies;
	//EXPLANATION: The key is used to look up the response, which, upon launching, will call the desired method
	protected Dictionary<string, Response> Behaviour = new Dictionary<string, Response>();

	public void TestMethod(params Variant[] args)
	{
		
	}
}
