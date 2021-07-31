using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float chunk_size = 50.0f;
	int chunk_detail = 7;
	int chunk_amount = 1;
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

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 6;
		noise.Period = 160;

		thread = new Thread();

		Statics.IterateSpiral(chunk_amount * 2, 0, 0, (x, y) =>
		{
			Chunk chunk = new Chunk(noise, material, x, y, chunk_size);
			AddChild(chunk);
			chunks.Add(chunk);
		});
					
		Player = GetNode("Player") as Spatial;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

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

	int GetDetailForIndex(int x, int z)
	{
		int offset = GetIndexOffset(x, z);
		if (offset < 2)
			return chunk_detail;
		else
			return chunk_detail - offset + 1;
	}

	int GetIndexOffset(int x, int z)
	{
		int diff = Mathf.Max(Mathf.Abs(playerPosIndexX - x), Mathf.Abs(playerPosIndexZ - z));
		return diff;
	}

	int GetSeamSide(int x, int y)
	{
		int d = GetDetailForIndex(x, y);
		int[] result = new int[4];
		result[0] = GetDetailForIndex(x, y + 1);
		result[1] = GetDetailForIndex(x + 1, y);
		result[2] = GetDetailForIndex(x, y - 1);
		result[3] = GetDetailForIndex(x - 1, y);
		for (int i = 0; i < result.Length; i++)
			if (result[i] < d)
				return i;
		return -1;
	}

	void ProcessCell(int x, int z)
	{
		int detail = GetDetailForIndex(x, z);

		Chunk chunk = chunks.Find(c => c.x == x && c.z == z);
		if (chunk != null && chunk.detail == detail)
		{
			return;
		}

		if (!thread.IsActive())
		{
			if (chunk == null)
			{
				chunk = GetFreeChunk();
				chunk.x = x;
				chunk.z = z;
			}
			thread.Start(this, "LoadChunk", new object[5] { thread, chunk, x, z, detail });
			//LoadChunk(new object[5] { null, chunk, x, z, detail });
		}
	}

	void LoadChunk(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		Chunk chunk = arr[1] as Chunk;
		int x = (int)arr[2];
		int z = (int)arr[3];
		int detail = (int)arr[4];

		chunk.SetDetail(detail, GetSeamSide(x, z));
		chunk.Generate();
		chunk.Translation = new Vector3(chunk.x * chunk_size, 0, chunk.z * chunk_size);

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
			if (GetIndexOffset(chunk.x, chunk.z) > chunk_amount)
			{
				chunk.isBusy = false;
			}
		}
		
		Statics.IterateSpiral(chunk_amount * 2, playerPosIndexX, playerPosIndexZ, ProcessCell);
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