using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float chunk_size = 100.0f;
	int max_detail = 7;
	int rings_amount = 10;
	bool detailDegrade = true;

	PackedScene PlayerScene = ResourceLoader.Load("res://Scenes/Player.tscn") as PackedScene;
	PackedScene FlyCamScene = ResourceLoader.Load("res://Scenes/FlyCam.tscn") as PackedScene;
	Material material = ResourceLoader.Load("res://Terrain.material") as Material;

	Spatial Player;
	Spatial FlyCam;

	OpenSimplexNoise noise;
	List<Ring> rings= new List<Ring>();

	int playerPosIndexX, playerPosIndexZ;

	Thread thread;

	public override void _Ready()
	{
		base._Ready();

		// Enable wireframe mode in game view:
		VisualServer.SetDebugGenerateWireframes(true);
		GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 6;
		noise.Period = 160;

		thread = new Thread();
		
		for (int i = 0; i < rings_amount; i++)
		{
			float s = GetSizeForRing(i);
			Ring ring = new Ring(i, max_detail - i, noise, material, s);
			rings.Add(ring);
			foreach(Chunk c in ring.chunks)
			{
				AddChild(c);
			}
		}
		
		Player = GetNode("Player") as Spatial;
	}
	
	float GetSizeForRing(int ring)
	{
		// The formula of n-th term in a geometric progression:
		// Tn = T1 * ratio^(n - 1)
		// where T1 is 1st term, ratio is the progression ratio.
		return chunk_size * (float)Mathf.Pow(3, ring - 1);
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
		int prevX = playerPosIndexX;
		int prevY = playerPosIndexY;
		Vector3 player_translation = Player.Translation;
		player_translation.x += player_translation.x > 0 ? chunk_size * 0.5f : chunk_size * -0.5f;
		player_translation.z += player_translation.z > 0 ? chunk_size * 0.5f : chunk_size * -0.5f;
		playerPosIndexX = (int)(player_translation.x / chunk_size);
		playerPosIndexZ = (int)(player_translation.z / chunk_size);
		if (playerPosIndexX != prevX || playerPosIndexY != prevY)
		{
			float offsetX = (playerPosIndexX - prevX) * chunk_size;
			float offsetY = (playerPosIndexY - prevY) * chunk_size;
			UpdateRings(offsetX, offsetY);
		}
	}

	void ProcessRing(Ring ring, float offsetX, float offsetY)
	{
		//if (!thread.IsActive())
		//{
			//thread.Start(this, "LoadChunk", new object[4] { thread, ring, offsetX, offsetY });
			LoadRing(new object[4] { null, ring, offsetX, offsetY });
		//}
	}

	void LoadChunk(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		Chunk chunk = arr[1] as Chunk;
		float offsetX = (float)arr[2];
		float offsetY = (float)arr[3];
		
		ring.Shift(offsetX, offsetY);

		//CallDeferred("FinishThread", ring, thread);
	}

	void FinishThread(Chunk chunk, Thread thread)
	{
		thread.WaitToFinish();
	}

	void UpdateRings(float offsetX, float offsetY)
	{
		foreach (Ring ring in rings)
		{
			ProcessRing(ring, offsetX, offsetY);
		}
	}
}