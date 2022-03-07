using Godot;
using System;
using System.Collections.Generic;

public static class TerrainNoise
{
	static OpenSimplexNoise noise = new OpenSimplexNoise();

	static TerrainNoise()
	{
		noise.Octaves = 9;
		noise.Period = 2000f;
		noise.Persistence = 0.5f;
		noise.Lacunarity = 2f;
	}

	public static float Basic(Vector3 position, ulong seed)
	{
		noise.Seed = (int)seed;
		float n = noise.GetNoise3dv(position);
		return n * 0.5f + 0.5f;
	}
}