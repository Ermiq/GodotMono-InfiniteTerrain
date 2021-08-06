using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float originSize = 300.0f;
	int detail = 30;
	int ringsAmount = 5;
	
	PackedScene PlayerScene = ResourceLoader.Load("res://Scenes/Player.tscn") as PackedScene;
	PackedScene FlyCamScene = ResourceLoader.Load("res://Scenes/FlyCam.tscn") as PackedScene;
	Material material = ResourceLoader.Load("res://Terrain.material") as Material;

	Spatial Player;
	Spatial FlyCam;
	Spatial currentPlayer;
	bool doUpdate = true;

	OpenSimplexNoise noise;
	List<Ring> rings = new List<Ring>();

	Vector3 playerPreviousPosition;
	float offsetX, offsetZ;

	Thread thread;
	
	public override void _Ready()
	{
		base._Ready();

		// Enable wireframe mode in game view:
		VisualServer.SetDebugGenerateWireframes(true);
		GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 5;
		noise.Persistence = 0.2f;
		noise.Period = 2000;
		noise.Lacunarity = 3f;

		thread = new Thread();
		
		// Rings start from 1 and up to 'ringsAmount' inclusive.
		for (int i = 1; i <= ringsAmount; i++)
		{
			// The formula of n-th term in a geometric progression:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st term, ratio is the progression ratio.
			float size = originSize * (float)Mathf.Pow(3, i - 1);
			
			Ring ring = new Ring(i, noise, material, size, detail, i == 1);
			rings.Add(ring);
			foreach(Chunk c in ring.chunks)
			{
				AddChild(c);
			}
		}
		
		Player = GetNode("Player") as Spatial;
		currentPlayer = Player;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("ui_up"))
		{
			var vp = GetViewport();
			vp.DebugDraw = vp.DebugDraw == Viewport.DebugDrawEnum.Wireframe ? Viewport.DebugDrawEnum.Disabled : Viewport.DebugDrawEnum.Wireframe;
		}

		if (Input.IsActionJustPressed("ui_left"))
		{
			doUpdate = !doUpdate;
		}

		if (Input.IsActionJustPressed("ui_down"))
		{
			if (currentPlayer == Player)
			{
				Vector3 pos = Player.Translation;
				RemoveChild(Player);
				Player = null;
				FlyCam = FlyCamScene.Instance() as Spatial;
				FlyCam.Translation = pos;
				AddChild(FlyCam);
				currentPlayer = FlyCam;
			}
			else
			{
				Vector3 pos = FlyCam.Translation;
				RemoveChild(FlyCam);
				FlyCam = null;
				Player = PlayerScene.Instance() as Spatial;
				Player.Translation = pos;
				AddChild(Player);
				currentPlayer = Player;
			}
		}

		GetPlayerPosIndex();
	}

	void GetPlayerPosIndex()
	{
		if (!doUpdate)
		{
			return;
		}
		Vector3 player_translation = currentPlayer.Translation;
		if (player_translation.DistanceSquaredTo(playerPreviousPosition) > Mathf.Pow(originSize * 0.5f, 2))
		{
			offsetX = player_translation.x;
			offsetZ = player_translation.z;
			UpdateRings();
			playerPreviousPosition = player_translation;
		}
	}

	void UpdateRings()
	{
		if (!thread.IsActive())
		{
			thread.Start(this, "LoadRings", new object[3] { thread, offsetX, offsetZ });
			//LoadRing(new object[3] { null, offsetX, offsetZ });
		}
	}

	void LoadRings(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		float offsetX = (float)arr[1];
		float offsetY = (float)arr[2];
		
		foreach (Ring ring in rings)
		{
			ring.ShiftProcess(offsetX, offsetY);
		}

		CallDeferred("FinishThread", thread);
	}

	void FinishThread(Thread thread)
	{
		thread.WaitToFinish();

		foreach (Ring ring in rings)
			ring.ShiftApply();
	}
}