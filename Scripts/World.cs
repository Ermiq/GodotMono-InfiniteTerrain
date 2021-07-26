using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	float chunk_size = 100.0f;
	int chunk_detail = 6;
	int chunk_amount = 5;
	int detailDegradeCoef = 1;

	OpenSimplexNoise noise;

	ConcurrentDictionary<string, Chunk> chunks = new ConcurrentDictionary<string, Chunk>();
	List<string> chunks_in_process = new List<string>();

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

		for (int x = -chunk_amount; x <= chunk_amount; x++)
		{
			for (int z = -chunk_amount; z <= chunk_amount; z++)
			{
				Chunk chunk = new Chunk(noise, x, z, chunk_size);
				AddChild(chunk);
				chunks[x + "," + z] = chunk;
			}
		}
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		UpdatePlayerPosIndex();
		UpdateChunks();
	}

	void UpdatePlayerPosIndex()
	{
		Vector3 player_translation = (GetNode("Player") as Spatial).Translation;
		playerPosIndexX = (int)player_translation.x / (int)chunk_size;
		playerPosIndexZ = (int)player_translation.z / (int)chunk_size;
	}

	int GetDetailForIndex(int x, int z)
	{
		if (detailDegradeCoef == 0 || playerPosIndexX == x && playerPosIndexZ == z)
			return chunk_detail;
		else
			return chunk_detail - GetIndexOffset(x, z);
	}

	int GetIndexOffset(int x, int z)
	{
		int diff = Mathf.Max(Mathf.Abs(playerPosIndexX - x), Mathf.Abs(playerPosIndexZ - z));
		return diff;
	}

	void ProcessCell(int x, int z)
	{
		string key = x + "," + z;

		if (chunks_in_process.Contains(key))
		{
			return;
		}

		int detail = GetDetailForIndex(x, z);
		
		Chunk chunk;
		chunks.TryGetValue(key, out chunk);
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
			chunk.Translation = new Vector3(chunk.x * chunk_size, 0, chunk.z * chunk_size);

			thread.Start(this, "LoadChunk", new object[2] { thread, chunk });
			chunks_in_process.Add(key);
		}
	}

	void LoadChunk(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		Chunk chunk = arr[1] as Chunk;
		
		int detail = GetDetailForIndex(chunk.x, chunk.z);
		chunk.SetDetail(detail);
		chunk.Generate();

		CallDeferred("FinishThread", chunk, thread);
	}

	void FinishThread(Chunk chunk, Thread thread)
	{
		thread.WaitToFinish();

		string key = chunk.x + "," + chunk.z;
		chunks[key] = chunk;
		chunks_in_process.Remove(key);
	}

	void UpdateChunks()
	{
		foreach (string key in chunks.Keys)
		{
			Chunk chunk = chunks[key];
			if (GetIndexOffset(chunk.x, chunk.z) >= chunk_amount)
			{
				chunk.isBusy = false;
			}
		}

		for (int x = (playerPosIndexX - chunk_amount); x <= (playerPosIndexX + chunk_amount); x++)
		{
			for (int z = (playerPosIndexZ - chunk_amount); z <= (playerPosIndexZ + chunk_amount); z++)
			{
				ProcessCell(x, z);
			}
		}
	}

	Chunk GetFreeChunk()
	{
		foreach (Chunk chunk in chunks.Values)
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