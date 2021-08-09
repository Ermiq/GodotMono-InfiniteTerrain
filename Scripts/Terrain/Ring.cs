using Godot;
using System;
using System.Collections.Generic;

public class Ring
{
	public Chunk[] chunks;

	public Ring(int index, OpenSimplexNoise noise, Material material, float size, int detail, bool addCollision)
	{
		chunks = new Chunk[index == 1 ? 1 : 8];
		
		if (index == 1)
		{
			Chunk chunk = new Chunk(noise, material, Vector2.Zero, size * 3, detail * 3, addCollision);
			chunk.Prepair(0, 0, 0);
			chunk.Translation = chunk.prePosition;
			chunk.Apply();
			chunks[0] = chunk;
		}
		else
		{
			int count = 0;
			for (int j = -1; j < 2; j++)
			{
				for (int k = -1; k < 2; k++)
				{
					if (j == 0 && k == 0)
						continue;
					Chunk chunk = new Chunk(noise, material, new Vector2(j, k), size, detail, addCollision);
					chunk.Prepair(0, 0, 0);
					chunk.Translation = chunk.prePosition;
					chunk.Apply();
					chunks[count] = chunk;
					count++;
				}
			}
		}
	}

	public void ShiftProcess(float offsetX, float offsetY, float offsetZ)
	{
		foreach (Chunk chunk in chunks)
		{
			chunk.Prepair(offsetX, offsetY, offsetZ);
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