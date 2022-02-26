using Godot;
using System;
using System.Collections.Generic;

public static class TerrainNoise
{
	static OpenSimplexNoise noise = new OpenSimplexNoise();
	static float noiseRoughness = 1f;
	static bool noiseRidges;

	static TerrainNoise()
	{
		noise.Octaves = 9;
		noise.Period = 2000f;
		noise.Persistence = 0.5f;
		noise.Lacunarity = 2f;
	}

	public static float Basic(Vector3 position)
	{
		float n = noise.GetNoise3dv(position * noiseRoughness);
		return n * 0.5f + 0.5f;
	}
}