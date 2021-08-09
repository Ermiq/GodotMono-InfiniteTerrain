using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float originalSize = 500.0f;
	float adjustedSize;
	int detail = 40;
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
	float offsetX, offsetY, offsetZ;

	Thread thread;
	
	public override void _Ready()
	{
		base._Ready();

		// Enable wireframe mode in game view:
		VisualServer.SetDebugGenerateWireframes(true);
		GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 9;
		noise.Persistence = 0.2f;
		noise.Period = 10000;
		noise.Lacunarity = 4f;

		thread = new Thread();

		adjustedSize = originalSize;
		
		// Rings start from 1 and up to 'ringsAmount' inclusive.
		for (int i = 1; i <= ringsAmount; i++)
		{
			// The formula of n-th term in a geometric progression:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st term, ratio is the progression ratio.
			float size = originalSize * (float)Mathf.Pow(3, i - 1);
			
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

	int indexX, indexY, indexZ;
	void GetPlayerPosIndex()
	{
		if (!doUpdate)
			return;

		Vector3 player_translation = currentPlayer.Translation;
		indexY = Mathf.FloorToInt(player_translation.y / 5000f);
		indexY = Mathf.Clamp(indexY, 1, indexY);
		indexX = Mathf.FloorToInt(player_translation.x / (originalSize * indexY));
		indexZ = Mathf.FloorToInt(player_translation.z / (originalSize * indexY));
		
		if (playerPreviousPosition.x != indexX || playerPreviousPosition.y != indexY || playerPreviousPosition.z != indexZ)
		{
			if (thread.IsActive())
				return;
			offsetX = indexX * (originalSize * indexY) + (originalSize * indexY * 0.5f);
			offsetY = indexY;
			if (offsetY != 1) offsetY *= 1.5f;
			offsetZ = indexZ * (originalSize * indexY) + (originalSize * indexY * 0.5f);
			
			playerPreviousPosition.x = indexX;
			playerPreviousPosition.y = indexY;
			playerPreviousPosition.z = indexZ;
			UpdateRings();
		}
	}

	void UpdateRings()
	{
		thread.Start(this, "LoadRings", new object[4] { thread, offsetX, offsetY, offsetZ });
		//LoadRing(new object[3] { null, offsetX, offsetZ });
	}

	void LoadRings(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		float offsetX = (float)arr[1];
		float offsetY = (float)arr[2];
		float offsetZ = (float)arr[3];
		
		foreach (Ring ring in rings)
		{
			ring.ShiftProcess(offsetX, offsetY, offsetZ);
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