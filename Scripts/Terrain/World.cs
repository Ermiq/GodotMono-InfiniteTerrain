using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float originSize = 100.0f;
	int originDetail = 7;
	int ringsAmount = 6;
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

		int prevD = originDetail;
		int[] details = new int[ringsAmount];
		for (int i = 0; i < ringsAmount; i++)
		{
			details[i] = originDetail + i;// (int)(prevD * 0.5f);
			//prevD = details[i] % 2 > 0 ? details[i] - 1 : details[i];
		}
		
		for (int i = 0; i < ringsAmount; i++)
		{
			// The formula of n-th term in a geometric progression:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st term, ratio is the progression ratio.
			float size = originSize * (float)Mathf.Pow(3, i - 1);
			int detail =
				details[i];
				//originDetail * (int)Mathf.Pow(2, i - 1);
			
			Ring ring = new Ring(i, noise, material, size, detail);
			rings.Add(ring);
			foreach(Chunk c in ring.chunks)
			{
				AddChild(c);
			}
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
		int prevX = playerPosIndexX;
		int prevY = playerPosIndexZ;
		Vector3 player_translation = Player.Translation;
		player_translation.x += player_translation.x > 0 ? originSize * 0.5f : originSize * -0.5f;
		player_translation.z += player_translation.z > 0 ? originSize * 0.5f : originSize * -0.5f;
		playerPosIndexX = (int)(player_translation.x / originSize);
		playerPosIndexZ = (int)(player_translation.z / originSize);
		if (playerPosIndexX != prevX || playerPosIndexZ != prevY)
		{
			float offsetX = (playerPosIndexX - prevX) * originSize;
			float offsetY = (playerPosIndexZ - prevY) * originSize;
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

	void LoadRing(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		Ring ring = arr[1] as Ring;
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