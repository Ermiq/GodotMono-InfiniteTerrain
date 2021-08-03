using Godot;
using System;
using System.Collections.Generic;

public class Ring
{
	public List<Chunk> chunks = new List<Chunk>();

	public Ring(int index, OpenSimplexNoise noise, Material material, float size, int detail)
	{
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				if (j == 0 && k == 0 && index != 1)
					continue;
				Chunk chunk = new Chunk(noise, material, new Vector2(j, k), size, detail, index != 1);
				chunk.Prepair(0, 0);
				chunk.Translation = chunk.prePosition;
				chunk.Apply();
				chunks.Add(chunk);
			}
		}
	}

	public void ShiftProcess(float offsetX, float offsetY)
	{
		foreach (Chunk chunk in chunks)
		{
			chunk.Prepair(offsetX, offsetY);
		}
	}

	public void ShiftApply()
	{
		foreach (Chunk chunk in chunks)
		{
			chunk.Apply();
		}
	}
}