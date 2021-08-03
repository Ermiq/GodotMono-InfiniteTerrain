using Godot;
using System;
using System.Collections.Generic;

public class Ring
{
	public List<Chunk> chunks = new List<Chunk>();

	public Ring(OpenSimplexNoise noise, Material material, float size, int detail)
	{
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				if (j == 0 && k == 0 && index != 0)
					continue;
				Chunk chunk = new Chunk(noise, material, new Vector2(j, k), size, detail);
				chunk.Translation = new Vector3(j * size, 0, k * size);
				chunk.Generate();
				chunks.Add(chunk);
			}
		}
	}
	
	public void Shift(float offsetX, float offsetY)
	{
		foreach (Chunk chunk in chunks)
		{
			chunk.Translation += new Vector3(offsetX, 0, offsetY);
		}
	}
}