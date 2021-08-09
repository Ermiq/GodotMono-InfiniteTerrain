using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float originalSize = 300.0f;
	int detail = 30;
	int ringsAmount = 5;
	bool doUpdate = true;
	
	PackedScene PlayerScene = ResourceLoader.Load("res://Scenes/Player.tscn") as PackedScene;
	PackedScene FlyCamScene = ResourceLoader.Load("res://Scenes/FlyCam.tscn") as PackedScene;
	Spatial Player;
	Spatial FlyCam;
	Spatial currentPlayer;
	
	Material material = ResourceLoader.Load("res://Terrain.material") as Material;

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
		noise.Persistence = 0.25f;
		noise.Period = 5000;
		noise.Lacunarity = 4f;

		thread = new Thread();
		
		// Rings start from 1 and up to 'ringsAmount' inclusive.
		for (int i = 1; i <= ringsAmount; i++)
		{
			// Each ring from the center has size of chunk increased by 3 from the previous ring.
			// Using the geometric progression formula we find the size of a chunk:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st chunk size,
			// ratio is the progression ratio (3 in our case),
			// n (Tn) is the ring index (and the size of its chunks) we need to find:
			float size = originalSize * (float)Mathf.Pow(3, i - 1);
			
			Ring ring = new Ring(i, noise, material, size, detail, i == 1);
			
			rings.Add(ring);
			foreach(Chunk c in ring.chunks)
			{
				AddChild(c);
			}
		}
		
		Player = GetParent().GetNode("Player") as Spatial;
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
				GetParent().RemoveChild(Player);
				Player = null;
				FlyCam = FlyCamScene.Instance() as Spatial;
				FlyCam.Translation = pos;
				GetParent().AddChild(FlyCam);
				currentPlayer = FlyCam;
			}
			else
			{
				Vector3 pos = FlyCam.Translation;
				GetParent().RemoveChild(FlyCam);
				FlyCam = null;
				Player = PlayerScene.Instance() as Spatial;
				Player.Translation = pos;
				GetParent().AddChild(Player);
				currentPlayer = Player;
			}
		}

		GetPlayerPosIndex();
	}

	void GetPlayerPosIndex()
	{
		if (!doUpdate)
			return;

		Vector3 player_translation = currentPlayer.Translation;
		Vector3 index;
		index.y = Mathf.FloorToInt(player_translation.y / 5000f);
		index.y = Mathf.Clamp(index.y, 1, index.y);
		index.x = Mathf.FloorToInt(player_translation.x / (originalSize * index.y));
		index.z = Mathf.FloorToInt(player_translation.z / (originalSize * index.y));
		
		if (playerPreviousPosition != index)
		{
			if (thread.IsActive())
				return;
			offsetX = index.x * (originalSize * index.y) + (originalSize * index.y * 0.5f);
			offsetZ = index.z * (originalSize * index.y) + (originalSize * index.y * 0.5f);
			offsetY = index.y;
			if (offsetY != 1) offsetY *= 1.5f;
			
			playerPreviousPosition.x = index.x;
			playerPreviousPosition.y = index.y;
			playerPreviousPosition.z = index.z;
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