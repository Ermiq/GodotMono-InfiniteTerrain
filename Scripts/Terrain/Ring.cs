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
			}
		}
	}
}