using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class World : Spatial
{
	int chunk_size = 20;
	int chunk_detail = 4;
	int chunk_amount = 10;
	int detailDegradeCoef = 2;
	
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
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		UpdatePlayerPosIndex();
		update_chunks();
		clean_up_chunks();
		reset_chunks();
	}

	void UpdatePlayerPosIndex()
	{
		Vector3 player_translation = (GetNode("Player") as Spatial).Translation;
		playerPosIndexX = (int)(player_translation.x) / chunk_size;
		playerPosIndexZ = (int)(player_translation.z) / chunk_size;
	}

	int GetDetailForIndex(int x, int z)
	{
		if (playerPosIndexX == x && playerPosIndexZ == z)
			return chunk_detail;
		else
		{
			int diff = Mathf.Max(Mathf.Abs(playerPosIndexX - x), Mathf.Abs(playerPosIndexZ - z)) / detailDegradeCoef;
			return chunk_detail - diff;
		}
	}

	void add_chunk(int x, int z)
	{
		Chunk chunk = null;
		string key = x + "," + z;

		chunks.TryGetValue(key, out chunk);
		if (chunk != null)
		{
			chunk.SetToRemove(false);
			return;
		}
		else
		{
			if (chunks_in_process.Contains(key))
			{
				return;
			}
			else if (!thread.IsActive())
			{
				thread.Start(this, "load_chunk", new object[3] { thread, x, z });
				chunks_in_process.Add(key);
			}
		}
	}

	void load_chunk(object[] arr)
	{
		Thread thread = arr[0] as Thread;
		int x = (int)arr[1];
		int z = (int)arr[2];

		int detail = GetDetailForIndex(x, z);
		var chunk = new Chunk(noise, x, z, chunk_size, detail);

		AddChild(chunk);
		chunk.Translation = new Vector3(chunk.x * chunk_size, 0, chunk.z * chunk_size);
		
		CallDeferred("finish", chunk, thread);
	}

	void finish(Chunk chunk, Thread thread)
	{
		thread.WaitToFinish();
		
		string key = chunk.x + "," + chunk.z;
		chunks[key] = chunk;
		chunks_in_process.Remove(key);
	}

	void update_chunks()
	{
		/*
		for (int x1 = 0; x1 > (p_x - chunk_amount); x1--)
		{
			for (int x2 = 0; x2 <= (p_x + chunk_amount); x2++)
			{
				for (int z1 = 0; z1 > (p_z - chunk_amount); z1--)
				{
					for (int z2 = 0; z2 <= (p_z + chunk_amount); z2++)
					{
						add_chunk(x1, z1);
						add_chunk(x2, z2);
						add_chunk(x1, z2);
						add_chunk(x2, z1);
					}
				}

			}
		}
		*/
		
		for (int x = (playerPosIndexX - chunk_amount); x <= (playerPosIndexX + chunk_amount); x++)
		{
			for (int z = (playerPosIndexZ - chunk_amount); z <= (playerPosIndexZ + chunk_amount); z++)
			{
				add_chunk(x, z);
			}
		}
		
	}

	void clean_up_chunks()
	{
		foreach (string key in chunks.Keys)
		{
			Chunk chunk = chunks[key];
			if (chunk.should_be_removed)
			{
				chunk.QueueFree();
				chunks.TryRemove(key, out chunk);
			}
		}
	}

	void reset_chunks()
	{
		foreach (string key in chunks.Keys)
		{
			chunks[key].SetToRemove(true);
		}
	}
}
