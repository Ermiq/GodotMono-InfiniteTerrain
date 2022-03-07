using Godot;

public class TerrainSettings
{
	public int rootChunkSize = 10000000;
	public int chunkSize = 200;
	public int detail = 50;

	public Shader Shader = ResourceLoader.Load("res://Terrain.shader") as Shader;
	public Resource tex_flat = ResourceLoader.Load("res://Assets/Textures/Grass_Seamless_1024.jpg");
	public Resource tex_slope = ResourceLoader.Load("res://Assets/Textures/Sand_Ground_1024.jpg");

	public float altitudeBase;
	public float altitudeHigh;

	RandomNumberGenerator rng;

	public TerrainSettings(uint seed)
	{
		rng = new RandomNumberGenerator();
		rng.Seed = seed;

		this.altitudeBase = 1;
		this.altitudeHigh = 1000;
	}

	public Vector3 EvaluatePositionFlat(Vector3 position, Vector3 up)
	{
		float alt = TerrainNoise.Basic(position, rng.Seed);
		return position * altitudeBase + up * alt * altitudeHigh;
	}

	public float EvaluatePositionFlat01(Vector3 position)
	{
		return TerrainNoise.Basic(position, rng.Seed);
	}
}