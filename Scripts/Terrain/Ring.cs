using Godot;
using System;
using System.Collections.Generic;

public class Ring
{
	public List<Chunk> chunks = new List<Chunk>();

	public Ring(int index, int detail, OpenSimplexNoise noise, Material material, float size)
	{
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				Chunk chunk = new Chunk(noise, material, new Vector2(j, k), size);
				chunks.Add(chunk);
				chunk.SetDetail(detail + (index - 1));
				chunk.Generate();
				chunk.Translation = new Vector3(j * size, 0, k * size);
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