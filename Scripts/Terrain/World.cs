using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float originSize = 300.0f;
	int detail = 50;
	int ringsAmount = 5;
	
	PackedScene PlayerScene = ResourceLoader.Load("res://Scenes/Player.tscn") as PackedScene;
	PackedScene FlyCamScene = ResourceLoader.Load("res://Scenes/FlyCam.tscn") as PackedScene;
	Material material = ResourceLoader.Load("res://Terrain.material") as Material;

	Spatial Player;
	Spatial FlyCam;

	OpenSimplexNoise noise;
	List<Ring> rings = new List<Ring>();

	int playerPosX, playerPosZ;
	int playerPosXPrevious, playerPosZPrevious;
	float offsetX, offsetZ;

	Thread thread;
	int ringInProcess;

	public override void _Ready()
	{
		base._Ready();

		// Enable wireframe mode in game view:
		VisualServer.SetDebugGenerateWireframes(true);
		GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 8;
		noise.Persistence = 0.5f;
		noise.Period = 200;

		thread = new Thread();
		
		// Rings start from 1 and up to 'ringsAmount' inclusive.
		for (int i = 1; i <= ringsAmount; i++)
		{
			// The formula of n-th term in a geometric progression:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st term, ratio is the progression ratio.
			float size = originSize * (float)Mathf.Pow(3, i - 1);
			
			Ring ring = new Ring(i, noise, material, size, detail, i < 2);
			ring.ShiftProcess(0, 0);
			ring.ShiftApply();
			rings.Add(ring);
			AddChild(ring);
		}
		
		Player = GetNode("Player") as Spatial;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("ui_up"))
		{
			var vp = GetViewport();
			vp.DebugDraw = vp.DebugDraw == Viewport.DebugDrawEnum.Wireframe ? Viewport.DebugDrawEnum.Disabled : Viewport.DebugDrawEnum.Wireframe;
		}

		if (Input.IsActionJustPressed("ui_down"))
		{
			if (Player != null)
			{
				Vector3 pos = Player.Translation;
				RemoveChild(Player);
				Player = null;
				FlyCam = FlyCamScene.Instance() as Spatial;
				FlyCam.Translation = pos;
				AddChild(FlyCam);
			}
			else
			{
				Vector3 pos = FlyCam.Translation;
				RemoveChild(FlyCam);
				FlyCam = null;
				Player = PlayerScene.Instance() as Spatial;
				Player.Translation = pos;
				AddChild(Player);
			}
		}

		GetPlayerPosIndex();
	}

	void GetPlayerPosIndex()
	{
		if (Player == null)
		{
			return;
		}
		Vector3 player_translation = Player.Translation;
		player_translation.x += player_translation.x > 0 ? originSize * 0.5f : originSize * -0.5f;
		player_translation.z += player_translation.z > 0 ? originSize * 0.5f : originSize * -0.5f;
		playerPosX = (int)(player_translation.x / originSize);
		playerPosZ = (int)(player_translation.z / originSize);
		if (playerPosX != playerPosXPrevious || playerPosZ != playerPosZPrevious)
		{
			offsetX = playerPosX * originSize;
			offsetZ = playerPosZ * originSize;
			UpdateRings();
			playerPosXPrevious = playerPosX;
			playerPosZPrevious = playerPosZ;
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