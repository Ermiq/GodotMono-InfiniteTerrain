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
	List<Chunk> chunks = new List<Chunk>();

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

		Chunk chunk = new Chunk(noise, material, new Vector2(0, 0), chunk_size);
		AddChild(chunk);
		chunks.Add(chunk);
		for (int i = 1; i < rings_amount; i++)
		{
			float s = chunk_size * (float)Math.Pow(1.5f, i);
			Ring ring = new Ring(i, max_detail - i, noise, material, s);
			foreach(Chunk c in ring.chunks)
			{
				AddChild(c);
				chunks.Add(c);
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
		UpdateChunks();
	}

	void GetPlayerPosIndex()
	{
		if (Player == null)
		{
			return;
		}
		Vector3 player_translation = Player.Translation;
		player_translation.x += player_translation.x > 0 ? chunk_size * 0.5f : chunk_size * -0.5f;
		player_translation.z += player_translation.z > 0 ? chunk_size * 0.5f : chunk_size * -0.5f;
		playerPosIndexX = (int)(player_translation.x / chunk_size);
		playerPosIndexZ = (int)(player_translation.z / chunk_size);
	}

	int GetDetailForIndex(int ring)
	{
		if (ring == 0)
			return max_detail;
		else
			return max_detail - ring;
	}

	SeamSide GetSeamSide(Vector2 index)
	{
		if (index == Vector2.Up)
			return SeamSide.TOP;
		else if (index == Vector2.Right)
			return SeamSide.RIGHT;
		else if (index == Vector2.Down)
			return SeamSide.BOTTOM;
		else if (index == Vector2.Left)
			return SeamSide.LEFT;
		else return SeamSide.NONE;
	}

	void ProcessCell(int ring, Vector2 index)
	{
		int detail = GetDetailForIndex(ring);

		Chunk chunk = chunks.Find(c => c.detail == ring && c.index == index);
		if (chunk != null && chunk.detail == detail)
		{
			return;
		}

		if (!thread.IsActive())
		{
			if (chunk == null)
			{
				chunk = GetFreeChunk();
				if (chunk == null)
					return;
				chunk.ring = ring;
				chunk.index = index;
			}
			thread.Start(this, "LoadChunk", new object[5] { thread, chunk, ring, index, detail });
			//LoadChunk(new object[5] { null, chunk, x, z, detail });
		}
	}

	void LoadChunk(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		Chunk chunk = arr[1] as Chunk;
		int ring = (int)arr[2];
		Vector2 index = (Vector2)arr[3];
		int detail = (int)arr[4];

		chunk.SetDetail(detail, GetSeamSide(chunk.index));
		chunk.Generate();
		chunk.Translation = new Vector3(chunk_size * Mathf.Pow(3, ring - 1), 0, chunk_size * Mathf.Pow(3, ring - 1));

		CallDeferred("FinishThread", chunk, thread);
	}

	void FinishThread(Chunk chunk, Thread thread)
	{
		thread.WaitToFinish();
	}

	void UpdateChunks()
	{
		foreach (Chunk chunk in chunks)
		{
			if (Mathf.Max(Mathf.Abs(chunk.index.x), Mathf.Abs(chunk.index.y)) > rings_amount)
			{
				chunk.isBusy = false;
			}
			ProcessCell(chunk.ring, chunk.index);
		}
	}

	Chunk GetFreeChunk()
	{
		foreach (Chunk chunk in chunks)
		{
			if (!chunk.isBusy)
			{
				chunk.isBusy = true;
				return chunk;
			}
		}
		return null;
	}
}