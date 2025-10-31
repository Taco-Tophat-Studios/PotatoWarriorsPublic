using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;

public partial class PlayerToolUniv : Area2D
{
	//SPELT SHEILD WRONG (for people ctrl f'ing shield stuff)
	private bool active = true;
	public bool GetActive () {
		return active;
	}
	public void SetActive (bool a) {
		if (active != a) { //if its alrady the same dont bother with the below
			for (int i = 0; i < toolSpecs.Count; i++) {
				toolSpecs[IterableTools[i]].Item1.Disabled = !a;
			}
			sprite.Visible = a;
			SwingSprite.Visible = a;
			active = a;
		}
		//a specific
		if (!a) {
			ToolColPolyPoints = Array.Empty<Vector2>();
			advToolCol.Polygon = ToolColPolyPoints;
		}
	}
	public bool isAuthOrSingle = true;
	public bool isSingleSpecifically = true; //if it is singleplayer
	[Export]
	public byte[] bouncingWithTools = {0, 0, 0, 0};
	public bool toolPreventingTerrainDamage = false; //if the tool is preventing terrain damage, i. e., the shield
	private FendOffMatch f;
	public playerBase MPplayer;
	private match m;
	public singlePlayerBase SPplayer;
	public playerInherited player; //for either kind
	public enum Tool
	{
		Sword,
		Shield,
		Fist
	}
	public Tool[] IterableTools = new Tool[] {Tool.Sword, Tool.Shield, Tool.Fist};
	[Export]
	public Tool equippedTool = Tool.Sword;
	public bool Is(Tool t)
	{
		return equippedTool == t;
	}
	public enum Mode
	{
		DamageAndHealth,
		Knockback,
		Maneuvering
	}
	public Color[] modeColors = { new(1, 0, 0, 0.625f), new(0, 0, 1, 0.625f), new(0, 1, 0, 0.625f) };
	[Export]
	public Mode currentMode = Mode.DamageAndHealth;
	private int inputRotDirection = 0;
	[Export]
	public float angle;
	[Export]
	public float angVel;
	private float SpecialAngVelStart = 0;
	private bool specialAngVeling = false;
	private float specialAngVelTime = 0;
	public float mouseAngle;
	private float mouseDist;
	private Vector2 mousePos;
	private Vector2 lastMousePos;
	private Vector2 mouseVel;
	private const float angDecelleration = 0.0001f; //the factor by which angVel will become after 1 second (probably)
	private const float range = 128;
	private const float angAcc = 10f;
	public const float maxAngVel = 32; 
	public float mouseAngMomentum;
	private const float mAMConst = 0.06f;
	//probably depreciate: on area enter should only trigger once and this was probably used for some kind of repetition prevention
	//bool[] bouncingWithTools = new bool[4] {false, false, false, false};
	[Export]
	public Node2D tip;
	public Vector2 tipRelPos;
	public Vector2 tipGlobalPos;
	public Vector2 PerpendicularNormal;
	public float tipVel;
	[Export]
	public Vector2 tipVel2D;
	[Export]
	private Vector2 tipRelVel2D;
	[Export]
	public AudioStreamPlayer audio;
	[Export]
	public AnimatedSprite2D sprite;
	[Export]
	public AnimatedSprite2D SwingSprite;
	[Export]
	public Node2D swingSpriteBase;
	private int frameNum;
	//First is the tool enum, then the tuple holds the collision shape, the animatedsprite animation (that holds the sprites for the tool), and then the index for the particular one
	private Dictionary<Tool, Tuple<CollisionShape2D, string, int>> toolSpecs = new();
	//index for the tool because making everything a dictionary is a big pain
	//For the differently-shaped and located collisions for the shield, sword, and fist respectively
	private Dictionary<Tool, Vector2I> toolColSpecs = new();
							//X is width, Y is offset
	private Vector2 StartScale;

	//TOOL SPECIFIC VARS
	List<RayCast2D> ReturningFireRays = new();
	List<Line2D> ReturningFireLines = new();
	List<Timer> FireReturnTimers = new();
	public bool fistHoldingFire;
	public float fistHeldFireDamage = 0;
    public float fistHeldFireKnockback = 0;

	[Export]
	CollisionPolygon2D advToolCol;
	private const int mD = 24;
	private const float dC2 = 2*Mathf.Pi/mD;//I use this, and not doing / 2pi because I screwed it up with the swing sprite as well, but I can just divide by it instead ig
	private Vector2[] ToolColPolyPoints = new Vector2[0];
	private Vector2 tempInnerPolyPoint;
	private Vector2 tempOuterPolyPoint;
    public void StealFire(float d, float k) //prometheus moment
    {
        fistHeldFireDamage = d;
        fistHeldFireKnockback = k;
		//don't need to play an animation because fist holding the thing is one frame 1 of fist anim
        sprite.Frame = 1;
        fistHoldingFire = true;
    }

	SyncedCallables SC = new();

	[Export]
	MultiplayerSynchronizer multSync;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Try to get multiplayer player
		if (GetParent() is playerBase mp)
		{
			MPplayer = mp;
			m = (match)GetTree().Root.GetNode("World");
			isSingleSpecifically = false;
			if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:tool_player_type"]) { GD.Print("detected player as MP type"); }
			multSync.SetMultiplayerAuthority((int)MPplayer.id);
		}
		// Try to get singleplayer player
		else if (GetParent() is singlePlayerBase sp)
		{
			SPplayer = sp;
			isSingleSpecifically = true;
			f = (FendOffMatch)GetTree().Root.GetNode("World");
			if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:tool_player_type"]) { GD.Print("detected player as SP type"); }
		}
		else
		{
			GD.PrintErr("PlayerToolUniv: Unknown parent type!");
		}

		player = (playerInherited)GetParent();

		audio = (AudioStreamPlayer)GetNode("SFXSource");
		tip = (Node2D)GetNode("Tip");

		StartScale = new Vector2(GlobalScale.X, GlobalScale.Y);
		tipRelPos = tip.GlobalPosition - player.GlobalPosition;
		tipGlobalPos = tip.GlobalPosition;
		//FireReturnTimer = new Timer(0.5f, false, this);

		GenerateCollisionHandlers();

		((ShaderMaterial)SwingSprite.Material).SetShaderParameter("key", new Color(1, 1, 1, 160/255));
	}
	public void AfterPlayerReady() {
		isAuthOrSingle = isSingleSpecifically || MPplayer.isAuthority; //if singleplayer, then true, otherwise check if the player is authority

		//TODO: provide local defaults for single player
		toolSpecs.Add(Tool.Sword, new Tuple<CollisionShape2D, string, int>((CollisionShape2D)GetNode("SwordCollisionShape"), "sword", 0));
		toolSpecs.Add(Tool.Shield, new Tuple<CollisionShape2D, string, int>((CollisionShape2D)GetNode("ShieldCollisionShape"), "shield", 0));
		toolSpecs.Add(Tool.Fist, new Tuple<CollisionShape2D, string, int>((CollisionShape2D)GetNode("FistCollisionShape"), "fist", 0));

		toolColSpecs.Add(Tool.Sword, new Vector2I(136, 32));
		toolColSpecs.Add(Tool.Shield, new Vector2I(64, 48));
		toolColSpecs.Add(Tool.Fist, new Vector2I(168, 0));
		//x is width, y is offset from the base
	}
	public void SetupSpriteIndices(int[] idx)
	{
		//why can't you just assign to a tuple...
		toolSpecs[Tool.Sword] = new Tuple<CollisionShape2D, string, int>(toolSpecs[Tool.Sword].Item1, toolSpecs[Tool.Sword].Item2, idx[0]);
		toolSpecs[Tool.Shield] = new Tuple<CollisionShape2D, string, int>(toolSpecs[Tool.Shield].Item1, toolSpecs[Tool.Shield].Item2, idx[1]);
		toolSpecs[Tool.Fist] = new Tuple<CollisionShape2D, string, int>(toolSpecs[Tool.Fist].Item1, toolSpecs[Tool.Fist].Item2, idx[2]);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
	public override void _PhysicsProcess(double delta)
	{
		//TODO: change TipVel2D and stuff to be handled client side instead of synchronized
		if (isAuthOrSingle && active)
		{
			//TODO: possibly convert to using change in mouse position on-screen, multiplied (only really doing this now in case constant global-based movement is more intuitive)
			float lastCameraRotation = player.cam.Rotation;
			player.cam.Rotation = 0;

			//Position comp
			Vector2 playerCamPosDiff = player.cam.GetScreenCenterPosition() - player.lastCameraGlobalPosition;
			player.lastCameraGlobalPosition = new Vector2(player.cam.GetScreenCenterPosition().X, player.cam.GetScreenCenterPosition().Y);

			//Rotation comp
			float cameraRotDiff = player.cam.Rotation - lastCameraRotation;
			lastCameraRotation = player.cam.Rotation;

			mousePos = GetGlobalMousePosition() - player.GlobalPosition;
			if (!Mathf.IsZeroApprox(cameraRotDiff))
			{
				//correct rotation by unrotating the last mouse position
				lastMousePos = lastMousePos.Rotated(-cameraRotDiff);
			}
			mouseAngle = Mathf.Atan2(mousePos.Y, mousePos.X);
			mouseDist = mousePos.Length();

			Vector2 delta1 = mousePos - lastMousePos; //mouse base movement
			delta1 = delta1.IsZeroApprox() ? Vector2.Zero : delta1;

			Vector2 delta2 = playerCamPosDiff - player.positionDiff; //camera relative movement correction

			Vector2 delta3 = delta1 - delta2; //subtract camera relative movement correction
			delta3 = delta3.IsZeroApprox() ? Vector2.Zero : delta3;

			mouseVel = delta3 / (float)delta; //subtract position difference to account for the player's movement and camera relative movement
			lastMousePos = new Vector2(mousePos.X, mousePos.Y);

			player.cam.Rotation = lastCameraRotation;

			if (mouseDist < 64)
			{
				mouseDist = 64;
			}
			else if (mouseDist > range)
			{
				mouseDist = range;
			}
			//meant to remove the effect of radius on mouseAngMomentum (i. e., further in reduces influence of moving mouse)
			//float radialEqualizer = Mathf.Clamp(Mathf.Pow(256 / mousePos.Length(), 2), 0, 80);

			mouseAngMomentum = -mAMConst * mouseVel.Cross(lastMousePos) * (float)delta * Mathf.Pow(256 / mousePos.Length(), 2);

			if (Mathf.Abs(mouseAngMomentum) > 1)
			{
				angVel += mouseAngMomentum * (float)delta;
			}

			angVel *= Mathf.Pow(angDecelleration, (float)delta);
			angVel = Mathf.Clamp(angVel, -maxAngVel, maxAngVel);

			if (this.GlobalScale.IsEqualApprox(StartScale))
			{
				this.GlobalScale = StartScale;
			}

			angle += angVel * (float)delta;
			angle = WorldBase.NormalizeAngle(angle); //normalize the angle to be between 0 and 2pi
													 //this.Position = mousePos.Rotated(-angle);//new Vector2(100 * Mathf.Cos(angle), -100 * Mathf.Sin(angle));
													 //tip stuff
			tipVel2D = (tip.GlobalPosition - tipGlobalPos) / (float)delta;
			tipRelVel2D = ((tip.GlobalPosition - player.GlobalPosition) - tipRelPos) / (float)delta;
			//WARNING: NOT SYNCED (although only used in other authority contexts)

			tipVel = tipVel2D.Length();

			tipGlobalPos = new Vector2(tip.GlobalPosition.X, tip.GlobalPosition.Y);
			tipRelPos = new Vector2(tip.GlobalPosition.X - player.GlobalPosition.X, tip.GlobalPosition.Y - player.GlobalPosition.Y);

			PerpendicularNormal = Position.Normalized(); //away from player center

			//----------------------
			HandleFistHoldingFire();

			if (Input.IsActionJustPressed("Debug_sync_effVel_test"))
			{
				m.players[1].RpcId(m.players[1].id, "CTEV", "DEBUG", new Vector2(1, -1) * BalancedValues.UNIT_KNOCKBACK, 0, true);
			}
			if (Input.IsActionJustPressed("Debug_increment_points_test"))
			{
				MPplayer.points++;
			}
		}
		if (active)
		{
			Position = new Vector2(mouseDist, 0).Rotated(angle);
			Rotation = angle;
			sprite.Rotation = -angVel * 0.04f;

			HandleAdvToolPolygon((float)delta);

			SwingSprite.Scale = new Vector2(3 * -Mathf.Sign(angVel), 3);
			SwingSprite.Rotation = angle + Mathf.Pi / 2;

			frameNum = Mathf.RoundToInt(2 * Math.Abs(this.angVel) / Mathf.Pi);
			SwingSprite.Visible = true;
			int fN = SwingSprite.SpriteFrames.GetFrameCount(toolSpecs[equippedTool].Item2);
			if (frameNum >= fN)
			{
				SwingSprite.Frame = fN - 1;
			}
			else if (frameNum > 0)
			{
				SwingSprite.Frame = frameNum;
			}
			else
			{
				SwingSprite.Visible = false;
			}
			((ShaderMaterial)SwingSprite.Material).SetShaderParameter("replace", modeColors[(int)currentMode]);
		}

		if (isAuthOrSingle && active)
		{
			CheckForWeaponSwitch(Tool.Shield, Tool.Fist);
			CheckForWeaponSwitch(Tool.Fist, Tool.Shield);

			//Mode tCM;
			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				currentMode = Mode.Knockback;
			}
			else if (Input.IsMouseButtonPressed(MouseButton.Right))
			{
				currentMode = Mode.Maneuvering;
			}
			else
			{
				currentMode = Mode.DamageAndHealth;
			}

			/*if (equippedTool == Tool.Fist && !fistHoldingFire) {
				StealFire(200, 200);
			}*/
			foreach (CollisionType cT in Enum.GetValues(typeof(CollisionType)))
			{
				if (collisionStati[cT])
				{
					foreach (Node2D n in collisionNodes[cT])
					{
						collisionHandlers[cT].Invoke(n, true, true); //TODO: maybe call the handler with entered = false?
					}
				}
			}
		}
		else if (active)
		{
			SetVars(equippedTool); //even for non-authority tools, equippedTool is handled by authority player, so just set it to the current one
								   //(for authority tools, setvars is called with CheckForWeaponSwitch in the block above)
		}	
	}
	
	private void HandleAdvToolPolygon(float delta) {
		if (Mathf.IsZeroApprox(angVel))
		{
			advToolCol.Polygon = Array.Empty<Vector2>(); //might mutate the array, so make a new one
		}
		else
		{
			float angleChunks = Mathf.Abs(angVel * delta / dC2);
			int segments = Mathf.Clamp((int)angleChunks, 0, mD);
			int l = Mathf.IsZeroApprox(angVel) ? 0 : segments * 2 + 4;
			//If no angVel, there should be no points. 
			//If some angvel, use 4 (2 at beginning, 2 for variable part at end) + 2 to deliniate each segment
			int s = Mathf.Sign(angVel);

			ToolColPolyPoints = new Vector2[l];
			float tempAng;
			Vector2 correctOffset = new(-mouseDist / 1.5f, 0);
			Vector2I toolCollisionSpec = toolColSpecs[equippedTool];

			float constant0 = mouseDist / Scale.X + toolCollisionSpec.Y;
			if (ToolColPolyPoints.Length < 4)
			{
				advToolCol.Polygon = Array.Empty<Vector2>();
			}
			else
			{
				//first two points
				ToolColPolyPoints[0] = new Vector2(constant0, 0f) + correctOffset;
				ToolColPolyPoints[l - 1] = new Vector2(constant0 + toolCollisionSpec.X, 0f) + correctOffset;
				for (int i = 1; i <= segments; i++)
				{
					tempAng = dC2 * i;
					tempInnerPolyPoint = new Vector2(Mathf.Cos(tempAng), -s * Mathf.Sin(tempAng)) * constant0;
					tempOuterPolyPoint = new Vector2(Mathf.Cos(tempAng), -s * Mathf.Sin(tempAng)) * (constant0 + toolCollisionSpec.X);

					tempInnerPolyPoint += correctOffset;
					tempOuterPolyPoint += correctOffset;

					ToolColPolyPoints[i] = tempInnerPolyPoint;
					ToolColPolyPoints[l - i - 1] = tempOuterPolyPoint;
				}
				//last two points
				float lastAngle = (angleChunks + segments) * dC2; // angleChunks (fraction of a segment) + segments (# of whole segments) -> * dC2 => angle to put last points at

				ToolColPolyPoints[l / 2 - 1] = new Vector2(Mathf.Cos(lastAngle), -s * Mathf.Sin(lastAngle)) * constant0 + correctOffset;
				ToolColPolyPoints[l / 2] = new Vector2(Mathf.Cos(lastAngle), -s * Mathf.Sin(lastAngle)) * (constant0 + toolCollisionSpec.X) + correctOffset;
			}
			if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:tool_collider_polygon_points"])
			{
				try
				{
					GD.Print(ToolColPolyPoints[0].Length());
				}
				catch
				{
					GD.Print("Tool coly points are N/A | N/A");
				}
			}
			advToolCol.Polygon = ToolColPolyPoints;
		}
	}
	
	//---------------------------------------------------------------------------------------------------
	private void HandleFistHoldingFire()
	{
		if (Input.IsActionJustReleased("fist") && fistHoldingFire)
		{   //should probably implement some degree of aim correction at some point
			/*FistHeldFireReturn.Position = this.Position;
			FistHeldFireReturn.TargetPosition = tipVel2D.Normalized() * 2048 + this.Position;*/

			//Have to create a new object and add it to world because otherwise it would rotate with the tool
			//also, since this is a reference variable, the hope is that multiple of these can be created and assigned to the world in case the player does this many times in a brief interlude
			RayCast2D tempRay = new();
			tempRay.GlobalPosition = this.GlobalPosition;
			tempRay.TargetPosition = -tipVel2D.Normalized() * 2048; //maybe it does do it straight on, but it seems to be little askew
																	//^^^ the tangent of the angular velocity, stretch3d to 2048, with the base at the position
			tempRay.ExcludeParent = true;
			f.AddChild(tempRay);
			ReturningFireRays.Add(tempRay);

			Line2D tempLine = new();
			tempLine.GlobalPosition = tempRay.GlobalPosition;
			tempLine.AddPoint(Vector2.Zero);
			tempLine.AddPoint(tempRay.TargetPosition + tempRay.GlobalPosition - tempLine.GlobalPosition);
			tempLine.DefaultColor = new Color(1, 1, 1);
			f.AddChild(tempLine);
			ReturningFireLines.Add(tempLine);

			FireReturnTimers.Add(new Timer(0.3f, true, this));

			if (tempRay.IsColliding())
			{
				foreach (enemyBase e in f.enemies)
				{
					if (tempRay.GetCollider() == e)
					{
						GD.Print("hit an anemone!");
						e.Damage(fistHeldFireDamage, "fist-held fire");
						e.CTEV("tool_fist_returnFire attack_beam", new Vector2(fistHeldFireKnockback, 0).Rotated(tipVel2D.Angle()));
						//e.effectVelocity += new Vector2(fistHeldFireKnockback, 0).Rotated(tipVel2D.Angle());
						break;
					}
				}
			}
			sprite.Frame = 0; //back to normal fist image
		}

		for (int i = 0; i < ReturningFireRays.Count; i++)
		{
			if (FireReturnTimers[i].active && FireReturnTimers[i].timerVal <= FireReturnTimers[i].threshHold)
			{
				ReturningFireLines[i].Width = 20 * (FireReturnTimers[i].threshHold - FireReturnTimers[i].timerVal) / FireReturnTimers[i].threshHold; //hopefully this works lol
			}
			else if (FireReturnTimers[i].done)
			{
				ReturningFireRays[i].QueueFree();
				ReturningFireRays.RemoveAt(i);

				ReturningFireLines[i].QueueFree();
				ReturningFireLines.RemoveAt(i);

				FireReturnTimers[i].Dispose();
				FireReturnTimers.RemoveAt(i);
			}
		}
	}
	//I think you pass in two to handle cases with pressing multiple keys at the same time???? I dunno I completely forgot :shrug:
	private void CheckForWeaponSwitch(Tool check, Tool other)
	{
		//if just pressed, activate
		if (Input.IsActionJustPressed(toolSpecs[check].Item2))
		{
			SetVars(check);
			//If just released, set to another
		}
		else if (Input.IsActionJustReleased(toolSpecs[check].Item2))
		{
			/*if something other than the sword was previously being held (before this one activated and then 
			released), then set it to other*/
			if (Input.IsActionPressed(toolSpecs[other].Item2))
			{
				SetVars(other);
			}
			else
			{
				SetVars(); //default to sword otherwise
			}

		}
	}
	private void SetVars(Tool t = Tool.Sword) {
		foreach (Tool tool in Enum.GetValues(typeof(Tool)))
		{
			toolSpecs[tool].Item1.Disabled = tool != t; //if it is the new tool, enable collision (this is if it is DISABLED, so NOT is used)
			//REGARDLESS of authority or not because this is already taken care of, and a tool collider needs to exist on both sides
			//for HandleToolCollision
		}
		//enable new tool
		equippedTool = t;
		sprite.Animation = toolSpecs[t].Item2;
		sprite.Frame = toolSpecs[t].Item3;
		SwingSprite.Animation = toolSpecs[t].Item2;

		//TODO: Redraw shield sprites so the pixel size is consistent
		if (equippedTool == Tool.Shield)
		{
			sprite.Scale = new Vector2(3, 3);
		}
		else
		{
			sprite.Scale = new Vector2(2, 2);
		}
	}
	private float BounceExpression(float avOTHER, Vector2 trpTHIS, Vector2 trpOTHER /*V2 FROM ULTRAKILL?!*/, Vector2 tvOTHER)
	{
		tvOTHER *= (float)GetProcessDeltaTime();
		//v1 and v2 are the tip positions rotated instead of the tip velocities, because the latter can be 0
		//and cause a bunch of headaches. Therefore, keep the angle signed, but the vectors which it compares with
		//tangent and normalized, always in positive, so the angle's sign can rub off on them
		float ret = avOTHER * trpTHIS.Rotated(Mathf.Pi / 2).Normalized().Dot(trpOTHER.Rotated(Mathf.Pi / 2).Normalized());
		//account for the sword's swing velocity
		ret += trpTHIS.Rotated(Mathf.Pi / 2).Normalized().Dot(tvOTHER) / Position.Length();

		return ret;
	}
	private Vector2 GetPerpendicularTipVelocity(PlayerToolUniv t, float mutliplier = 0.01f) {
		return t.tipVel2D.Rotated(Mathf.Sign(t.angVel) * -Mathf.Pi / 2) * mutliplier;
	}
	public void BounceTools(PlayerToolUniv t) { //other player's tool
        float angVelTemp;
        //the angVel's effective component and the added velocity from swinging it
        angVelTemp = BounceExpression(t.angVel, this.tipRelPos, t.tipRelPos, t.tipVel2D);
		GD.Print("bouncing tools (PlayerToolUniv @ BounceTools)");
        this.angVel = angVelTemp;
	}
	private void BounceOneTool(PlayerToolUniv t, float multipler = 2) //multiply by more than one to bring it away from this tool, so no repeated collisions
	{
		t.BounceTools(this);
		t.angVel *= multipler;	
	}

	private bool IsSuperiorTool(PlayerToolUniv other, bool strict = true)
	{
		if (equippedTool == Tool.Sword && other.equippedTool == Tool.Fist)
		{
			return true;
		}
		else if (equippedTool == Tool.Shield && other.equippedTool == Tool.Sword)
		{
			return true;
		}
		else if (equippedTool == Tool.Fist && other.equippedTool == Tool.Shield)
		{
			return true;
		}
		else if (equippedTool == other.equippedTool && !strict)
		{
			return true; //if the tools are the same (and not strict checking), then this is superior
		}
		return false;
	}

	//COLLISION DETECTION
	//---------------------------------------------------------------------------------------------------
	//---------------------------------------------------------------------------------------------------
	//---------------------------------------------------------------------------------------------------
	public enum CollisionType
	{
		Entity,
		Tool,
		Terrain,
	}
	public Dictionary<CollisionType, bool> collisionStati = new()
	{
		{ CollisionType.Entity, false },
		{ CollisionType.Tool, false },
		{ CollisionType.Terrain, false }
	};
	public Dictionary<CollisionType, HashSet<Node2D>> collisionNodes = new()
	{
		{ CollisionType.Entity, new HashSet<Node2D>() },
		{ CollisionType.Tool, new HashSet<Node2D>() },
		{ CollisionType.Terrain, new HashSet<Node2D>() },
	};
	public void GenerateCollisionHandlers()
	{
		collisionHandlers = new()
		{
			{ CollisionType.Entity, (n, cBF, e) => HandleEntityCollision((EntityBase)n, calledByFrame: cBF, entered: e) },
			{ CollisionType.Tool, (n, cBF, e) => HandleToolCollision((PlayerToolUniv)n, calledByFrame: cBF, entered: e) },
			{ CollisionType.Terrain, (n, cBF, e) => HandleTerrainCollision((TileMapLayer)n, calledByFrame: cBF, entered: e) },
		};
	}
	public Dictionary<CollisionType, Action<Node2D, bool, bool>> collisionHandlers = new();
	//handle like enemy collision, but with interactions to/from match

	//Reference: https://docs.google.com/drawings/d/1S4i-ayUdrFyLWSLLmO5TgJ6jCjm5gh7MxbDMUFatUwE/edit
	
	//NOTE: this can literally only happen in multiplayer, so its fine to assume p is a playerBase
	//TODO: move these to a separate file to clean up this one
    private void HandleEntityCollision(EntityBase e, bool calledByFrame = false, bool entered = true)
	{
		if (e == player)
		{
			return; //don't do NUTHIN
		}

		playerBase p = new();
		bool isPlayer = false;
		if (e is playerBase)
        {
			isPlayer = true;
			p = (playerBase)e;
        }

		if (!calledByFrame)
		{
			collisionStati[CollisionType.Entity] = entered;
			if (entered)
			{
				collisionNodes[CollisionType.Entity].Add(e);
			}
			else
			{
				collisionNodes[CollisionType.Entity].Remove(e);
			}
		}

		float StopPotentialRoll(float rollMult = 1.5f) //apparently a "local function"
		{
			if (entered && p.rolling)
			{
				//p.rollStopper = true;
				RpcId(p.id, "StopRolling");
				return rollMult;
			}
			return 1f;
		}

		//REMINDER: MOVING SELF SHOULD BE BASED ON RELATIVE TIP VELOCITY, MOVING/DAMAGING OTHERS (and healing self with shield) SHOULD BE BASED ON GLOBAL TIP VELOCITY
		Vector2 impactVector = (tipVel2D - e.VelocityWithEffVel) / BalancedValues.UNIT_EFFECT_VELOCITY;
		float impactScaler = impactVector.Length(); //based on relative velocity

		if (entered) //cannot use !calledByFrame because the shield maneuivering counter to roll demands constant velocity update from the rolling player
		{
			switch (equippedTool)
			{
				case Tool.Sword:
					if (angVel > maxAngVel * 0.25f && !calledByFrame)
					{
						if (currentMode == Mode.DamageAndHealth)
						{
							if (isPlayer)
                            {
								p.RpcId(p.id, "Damage", angVel / maxAngVel * BalancedValues.UNIT_DAMAGE * impactScaler * 0.5f * StopPotentialRoll(), "MP Sword hit damage");
								if (!isSingleSpecifically) {m.IncrementPointForPlayer((int)impactScaler, player.playerIndex, "Sword Damage");}
							} else
                            {
                                e.Damage(angVel / maxAngVel * BalancedValues.UNIT_DAMAGE * impactScaler * 2, "SP Sword hit damage");
                            }
							
						}
						else if (currentMode == Mode.Knockback)
						{
							if (isPlayer)
							{
								p.RpcId(p.id, "CTEV", "tool_sword_knockback", impactScaler * BalancedValues.UNIT_KNOCKBACK * 0.5f * PerpendicularNormal * StopPotentialRoll());
								if (!isSingleSpecifically) {m.IncrementPointForPlayer((int)impactScaler, player.playerIndex, "Sword Knockback");}
							} else
                            {
								e.CTEV("tool_sword_knockback", impactScaler * BalancedValues.UNIT_KNOCKBACK * 0.5f * PerpendicularNormal);
                            }
						}
					}
					break;
				case Tool.Shield:
					Vector2 pEV = player.VelocityWithEffVel - e.VelocityWithEffVel;
					if (!calledByFrame && currentMode == Mode.Knockback)
					{
						if (pEV.Length() > BalancedValues.UNIT_EFFECT_VELOCITY)
						{
							if (isPlayer)
							{
								p.RpcId(p.id, "Network_ModifyEVComponentWise", this, SyncedCallables.NameLookup[SyncedCallables.Names.BilliardFunc], false);
								m.IncrementPointForPlayer((int)(pEV.Length() / BalancedValues.UNIT_EFFECT_VELOCITY), player.playerIndex, "Knocked into shield");
							} else
							{
								e.ModifyEVComponentWise(billiardFuncWrapper, false);
                            }
						}

						//remove component of the player's effect velocity in the direction of the impact (don't move this to SyncedCallables because this only applies to this player)
						Func<string, Vector2, bool, Vector2> knockbackFunc = (_, eV, _) =>
						{
							return eV - Position.Normalized().Dot(eV) * Position.Normalized();
						};

						player.ModifyEVComponentWise(knockbackFunc);
						_ = StopPotentialRoll();
					} else if (!calledByFrame && currentMode == Mode.DamageAndHealth && (isPlayer && p.rolling)) //p.rolling shouldnt execute if isnt player (short circuit and), so it should be safe
					{
						float healRatio = pEV.Length() / BalancedValues.UNIT_EFFECT_VELOCITY * StopPotentialRoll(2f);
						player.Heal(BalancedValues.UNIT_HEAL * healRatio);
						if (!isSingleSpecifically) {m.IncrementPointForPlayer((int)healRatio, player.playerIndex, "Slammed into shield");}
					} else if (currentMode == Mode.Maneuvering && (isPlayer && p.rolling)) {
						//get pushed by roll TODO: Flesh this out more
						//if the rolling player accelerates, the shield (and this tool's player) will stay on them until jumping or something 
						//(unless the other player stops rolling, but then it'll just keep going)
						player.CTEV("tool_shield_roll_push", new Vector2(p.VelocityWithEffVel.X, 0));
					}
					break;
				case Tool.Fist:
					if (!calledByFrame)
                    {
						if (currentMode == Mode.Knockback)
						{
							if (isPlayer)
							{
								p.RpcId(p.id, "CTEV", "tool_fist_knockback", impactVector * BalancedValues.UNIT_KNOCKBACK * 0.5f * StopPotentialRoll());
								m.IncrementPointForPlayer((int)impactScaler, player.playerIndex, "Fist Knockback");
							} else
							{
								e.CTEV("tool_fist_knockback", impactVector * BalancedValues.UNIT_KNOCKBACK * 0.5f);
							}
						}
						else if (currentMode == Mode.DamageAndHealth)
						{
							float damager = StopPotentialRoll();
							//squish the player between the wall/floor/ceiling
							foreach (Vector2I n in e.currentTerrainNorms)
							{ //if the player is not on a tile with a norm, the DP is zero
								Vector2 N = (Vector2)n;                     //if the norm is opposite the incoming fists' TV2D, the DP is negative
								if (N.Dot(tipVel2D) < 0)
								{                   //so, if they are contacting something that would make them squished
													//p.Damage(BalancedValues.UNIT_DAMAGE);   //by the fist, damage them.
									GD.Print("Fist Crushing");
									if (isPlayer)
									{
										p.RpcId(p.id, "Damage", BalancedValues.UNIT_DAMAGE * damager, "MP Fist crush");
										m.IncrementPointForPlayer((int)impactScaler, player.playerIndex, "Fist Crush");
									} else
									{
										e.Damage(BalancedValues.UNIT_DAMAGE * damager, "SP Fist crush");
									}
								}
							}
						}
                    }
					break;
			}
		}
	}
	//NOTE: this can literally only happen in multiplayer, so its fine to assume t.player is a playerBase
	private void HandleToolCollision(PlayerToolUniv t, bool calledByFrame = false, bool entered = true)
	{
		if (t == this)
        {
			return;
        }

		if (!calledByFrame)
		{
			collisionStati[CollisionType.Tool] = entered;
			if (entered)
			{
				collisionNodes[CollisionType.Tool].Add(t);
			}
			else
			{
				collisionNodes[CollisionType.Tool].Remove(t);
			}
		}

		Tool type = t.equippedTool;
		playerBase p = (playerBase)t.player;
		if (entered && !calledByFrame && IsSuperiorTool(t, false))
		{
			BounceTools(t);
		}

		//handle weapon contact where THIS player has advantage (if at disadvantage, code will be run on other player's side)
		if (!calledByFrame && entered && IsSuperiorTool(t)) //only because, so far, all of these require the first two
		{
			//TODO: make this a switch
			if (equippedTool == Tool.Sword)
			{
				if (currentMode == Mode.DamageAndHealth)
				{
					p.RpcId(p.id, "Damage", tipVel / BalancedValues.UNIT_TIP_VELOCITY * BalancedValues.UNIT_DAMAGE * 1.5f, "sword counter");
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Sword through Damage & Health");
				}
				else if (currentMode == Mode.Knockback)
				{
					p.RpcId(p.id, "CTEV", "tool_sword_knockback", GetPerpendicularTipVelocity(this, 0.02f));
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Sword through Knockback");
				}

				BounceOneTool(t);
			}
			else if (equippedTool == Tool.Shield)
			{
				if (currentMode == Mode.DamageAndHealth)
				{
					player.Heal(t.tipVel / BalancedValues.UNIT_TIP_VELOCITY * BalancedValues.UNIT_HEAL);
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Shield through Maneuvering");
				}
				else if (currentMode == Mode.Maneuvering && (player.VelocityWithEffVel.Dot(this.Position) > 0))
				{
					//bounce off sword according to where shield is held (billiard physics)
					player.ModifyEVComponentWise(billiardFuncWrapper);
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Shield through Maneuvering");
				}

				BounceOneTool(t);
			}
			else if (equippedTool == Tool.Fist)
			{
				if (currentMode == Mode.Knockback)
				{
					p.RpcId(p.id, "CTEV", "tool_fist_shieldCounter", tipVel2D * 1f);
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Fist through Knockback");
				}
				else if (currentMode == Mode.Maneuvering)
				{
					//launch off of shield
					//GetOverIt(Vector2I.Up, new Vector2(0.1f, 0.25f), "tool_fist_terrainImpact");
					//TODO: make this call even if done by frame, as thats how it works with normal GOI calls
					player.CTEV("tool_fist_shield_counter", -tipRelVel2D * new Vector2(0.1f, 0.25f), 1);
					m.IncrementPointForPlayer((int)(tipVel / BalancedValues.UNIT_TIP_VELOCITY), player.playerIndex, "Countered with Fist through Maneuvering");
				}

				BounceOneTool(t);
			}

			m.IncrementPointForPlayer(3, player.playerIndex, "Countered correctly");
		}

	}
	private void HandleTerrainCollision(TileMapLayer tm, bool calledByFrame = false, bool entered = true)
	{
		if (!calledByFrame)
		{
			collisionStati[CollisionType.Terrain] = entered;
			if (entered)
			{
				collisionNodes[CollisionType.Terrain].Add(tm);
			}
			else
			{
				collisionNodes[CollisionType.Terrain].Remove(tm);
			}
		}
		/*
			in case the player is moving so fast on such low framerate that they manage to enter the terrain with
			their tool following, or at the same time, and both contact the terrain, the player taking damage is the priority
		*/
		if (player.hitTerrainFirst) {
			return;
		}

		toolPreventingTerrainDamage = false; //will turn to true if correct conditions met (checked below)
		//NOTE: this is turned true if GOI is successful

		switch (equippedTool)
		{
			case Tool.Sword:
				if (entered && currentMode == Mode.Maneuvering && (tipVel2D.Y > 0 || m.isLowGravity) /*allow to scale DOWN walls for low gravity*/)
				{
					if (DebugTags.GLOBAL_DEBUG_TAGS["SHOW:tool_tile_intersects"])
					{
						m.dTML.Clear();
					}
					GetOverIt(Vector2I.Right, new Vector2(0, 0.1f), "tool_sword_terrainImpact"); //should always push the player up --> should only push player when travelling down
					GetOverIt(Vector2I.Left, new Vector2(0, 0.1f), "tool_sword_terrainImpact");
				}
				break;
			case Tool.Shield:
				Vector2I[] intersects = GetIntersectsOfThisTool();
				int horizontalNormalTiles = m.FindNumTilesWithNormal(Vector2I.Left, intersects) + m.FindNumTilesWithNormal(Vector2I.Right, intersects);
				int verticalNormalTiles = m.FindNumTilesWithNormal(Vector2I.Up, intersects) + m.FindNumTilesWithNormal(Vector2I.Down, intersects);
				
				//heal from impact
				if (!calledByFrame && entered && currentMode == Mode.DamageAndHealth)
				{
					//GD.Print("Shield potentially Absorbing Impact");
					float ratioX = player.EffectVelocity.X / BalancedValues.UNIT_EFFECT_VELOCITY;
					float ratioY = player.EffectVelocity.Y / BalancedValues.UNIT_EFFECT_VELOCITY;
					if (ratioX > 1)
					{
						GD.Print("absorbing impact X");
						player.Heal(ratioX * BalancedValues.UNIT_HEAL);
					}
					if (ratioY > 1)
                    {
						GD.Print("absorbing impact Y");
						player.Heal(ratioY * BalancedValues.UNIT_HEAL);
                    }

					toolPreventingTerrainDamage = true;
				}
				
				//bounce off of terrain (with full bounce amount; not dampened)
				if (currentMode == Mode.Maneuvering && entered)
				{
					//DONT experience friction: slide across floor if shield is touching floor

					//bounce player off terrain
					if (!calledByFrame && entered)
					{
						Func<string, Vector2, bool, Vector2> bounce = (n, eV, sBC) => { return eV; }; //default do nothing 
																									  // (there will always be a tile with one of the normals, because this is only called when it is contacting tiles. 
																									  // This is just to make the error stop whining)

						if (horizontalNormalTiles > 0)
						{
							bounce = (n, eV, sBC) => { return eV * new Vector2(-1, 0); };
							player.ModifyEVComponentWise(bounce); //different bounce function for this and the next case; don't just modify afterward
						}
						if (verticalNormalTiles > 0)
						{
							bounce = (n, eV, sBC) => { return eV * new Vector2(0, -1); };
							player.ModifyEVComponentWise(bounce);
						}
					}

					if (angle < Mathf.Pi) { //maneuvering
						GD.Print("Shield allowing sliding");
						player.experienceFloorFriction = false; 
					} else
                    {
						GD.Print("shield didn't slide because angle is " + angle);
                    }
					if (Mathf.Abs(player.VelocityWithEffVel.X) > BalancedValues.UNIT_EFFECT_VELOCITY / 2 && !player.ShieldSlideSprite.IsPlaying() && player.floorAndWallStatus[0])
					{
						GD.Print("sliding");
						player.ShieldSlideSprite.Visible = true;
						player.ShieldSlideSprite.Play();
						Vector2 sprScale = player.ShieldSlideSprite.Scale;
						player.ShieldSlideSprite.Scale = new Vector2(sprScale.X * Mathf.Sign(player.VelocityWithEffVel.X), sprScale.Y);
                    } else
                    {
						player.ShieldSlideSprite.Stop(); //also done below, where shield is no longer used like this (or used at all), but done here in case player slows down while still valid
                    }
                } else if (!player.experienceFloorFriction)
				{
					GD.Print("shield stopped sliding");
					player.experienceFloorFriction = true; //WARNING: this is dangerous, as it could potentially lead to losing it if caused by something somewhere else!
					player.ShieldSlideSprite.Stop();
					player.ShieldSlideSprite.Visible = false;
				}

				break;
			case Tool.Fist:
				if (entered && currentMode == Mode.Maneuvering) //should be called initially and per frame
				{
					if (DebugTags.GLOBAL_DEBUG_TAGS["SHOW:tool_tile_intersects"])
					{
						m.dTML.Clear();
					}
					if (tipRelVel2D.Y > 0)//only if it is pushing INTO the ground, not PULLING OUT (because, theoretically, the fist would then only be sliding across the ground, not pushing off of it)
                    {
                        GetOverIt(Vector2I.Up, new Vector2(0.1f, 0.25f), "tool_fist_terrainImpact");
                    } else if (m.isLowGravity)
                    {
                        GetOverIt(Vector2I.Down, new Vector2(0.1f, 0.25f), "tool_fist_terrainImpact");
                    }
				}
				break;
		}

		if (!entered) {
			m.dTML.Clear();
		}
	}

	public Vector2 billiardFuncWrapper(string n, Vector2 eV, bool sBC)
	{
		return SC.BilliardFunc(this, n, eV, sBC);
	}

	public Vector2 billiardFuncWrapper2(string n, Vector2 eV, bool sBC)
	{
		PlayerToolUniv copy = new();
		copy.Position = -this.Position; 
		return SC.BilliardFunc(copy, n, eV, sBC);
	}
	//---------------------------------------------------------------------------------------------------
	//---------------------------------------------------------------------------------------------------
	//---------------------------------------------------------------------------------------------------

	//Godot body methods for detecting stuff
	private void _on_body_entered(Node2D body)
	{
		DetermineCollisionHandling(body, true);
	}
	
	private void _on_body_exited(Node2D body)
	{
		DetermineCollisionHandling(body, false);
	}

	private void DetermineCollisionHandling(Node2D body, bool entered)
	{
		if (body is EntityBase Ebase)
        {
			HandleEntityCollision(Ebase, entered: entered);
        } else if (body is TileMapLayer layer)
        {
			HandleTerrainCollision(layer, entered: entered);
        } else if (body is PlayerToolUniv univ)
        {
			HandleToolCollision(univ, entered: entered);
        } else
        {
            GD.Print("WARNING: no suitable body type found for Collision handling! (PlayerToolUniv.cs @ DetermineCollisionHandling, entered: " + entered + ")");
        }
	}
    //HELPER FUNCS
	private Vector2I[] GetIntersectsOfThisTool()
	{
		if (ToolColPolyPoints == null || ToolColPolyPoints.Length < 4)
        {
			return new Vector2I[0];
        }
        //get some vars down
		Vector2 toolRadii = toolColSpecs[equippedTool];
		//reminder: arc FOLLOWS angle
		float sAng;
		float eAng;
		if (angVel < 0)
		{
			sAng = angle;
			eAng = angle + dC2 * ToolColPolyPoints.Length / 2;
		}
		else
		{
			sAng = angle - dC2 * ToolColPolyPoints.Length / 2;
			eAng = angle;
		}
		sAng = WorldBase.NormalizeAngle(sAng);
		eAng = WorldBase.NormalizeAngle(eAng);

		//find and filter tile intersections
		Vector2I[] tiles = m.FindIntersectingTilesFromArc(player.GlobalPosition, toolRadii.Y, toolRadii.Y + toolRadii.X, sAng, eAng);
		return tiles;
    }
	private void GetOverIt(Vector2I norm, Vector2 EVMult, string CTEVname) //cause it works like the hammer in GOI lol
	{
		//Detect if method can be used
		if (tipRelVel2D.Length() < BalancedValues.UNIT_TIP_VELOCITY)
		{
			return; //don't do anything if the velocity is too low
		}

		Vector2I[] intersects = GetIntersectsOfThisTool();

		Vector2I playerTilePos = m.tm.LocalToMap(m.tm.ToLocal(player.GlobalPosition));
		Func<Vector2I, bool> filterNormsOnCorrectSide = (Vector2I tilePos) =>
		{
			Vector2I diff = tilePos - playerTilePos;
			int dot = diff.X * norm.X + diff.Y * norm.Y; //dot product (undefined for Integer vectors for some reason?????)
			return dot <= 0; //only use tiles on the correct side of the player (the norm is facing them)
		};
		intersects = m.FilterTilesPositionally(intersects, filterNormsOnCorrectSide);

		//debug: show 'em
		if (DebugTags.GLOBAL_DEBUG_TAGS["SHOW:tool_tile_intersects"])
		{
			if (norm.Equals(Vector2I.Up)) //for the fist (triggers else below)
			{
				m.dTML.Clear();
			}
			//separate colors for sword
			if (norm.Equals(Vector2I.Right))
			{
				m.dTML.HighlightTiles(intersects, new Vector2I(1, 0), false);
			}
			else
			{
				m.dTML.HighlightTiles(intersects, new Vector2I(0, 0), false);
			}
			
		}

		//actually move player
		if (m.FindNumTilesWithNormal(norm, intersects) > 0) //NOTE: any higher and this may skip ledges/overhangs X tiles thick!
		{
			player.CTEV(CTEVname, -tipRelVel2D * EVMult, 1);
			if (DebugTags.GLOBAL_DEBUG_TAGS["PRINT:tool_tile_movement"]) { GD.Print("Getting over it with CMTacoTophat | TipVel: " + tipVel2D + " | Frame: " + Engine.GetPhysicsFrames() + " | norm: " + norm); }
			toolPreventingTerrainDamage = true; //reset back to false handled in terrain collision method
		}
	}
}
