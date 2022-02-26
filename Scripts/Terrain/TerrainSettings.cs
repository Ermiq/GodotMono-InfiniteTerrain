using Godot;

public class TerrainSettings
{
	public ShaderMaterial material { get; private set; } = new ShaderMaterial();
	public Shader shaderDepth { get; private set; } = ResourceLoader.Load("res://Terrain.shader") as Shader;

	public int rootChunkSize = 10000000;
	public int chunkSize = 200;
	public int detail = 20;

	public float altitudeBase = 0f;
	public float altitudeHigh = 500f;

	public TerrainSettings()
	{
		material.Shader = shaderDepth;
		material.SetShaderParam("tex_flat", ResourceLoader.Load("res://Assets/Textures/Grass_Seamless_1024.jpg"));
		material.SetShaderParam("tex_slope", ResourceLoader.Load("res://Assets/Textures/Sand_Ground_1024.jpg"));
	}

	public TerrainSettings(float altitudeBase, float altitudeHigh, int noiseFunc)
	{
		material.Shader = shaderDepth;
		material.SetShaderParam("tex_flat", ResourceLoader.Load("res://Assets/Textures/Grass_Seamless_1024.jpg"));
		material.SetShaderParam("tex_slope", ResourceLoader.Load("res://Assets/Textures/Sand_Ground_1024.jpg"));
		
		this.altitudeBase = altitudeBase;
		this.altitudeHigh = altitudeHigh;
	}

	public float EvaluatePositionFlat(Vector3 position)
	{
		return TerrainNoise.Basic((Vector3)position) * altitudeHigh;
	}
}