using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class World : Spatial
{
	// A reference size for a chunk, representing the minimim size:
	public static float chunkSize = 100.0f;

	// The amount of quads in a chunk's row/line:
	public static int detail = 10;

	public static Material material = ResourceLoader.Load("res://Terrain.material") as Material;

	static OpenSimplexNoise noise;

	// The multiplier for the noise's -1..1 results:
	static float heightCoef = 1000f;

	bool doUpdate = true;

	PackedScene CarScene = ResourceLoader.Load("res://Scenes/Car.tscn") as PackedScene;
	PackedScene CamScene = ResourceLoader.Load("res://Scenes/Camera.tscn") as PackedScene;
	Spatial Car;
	Spatial Cam;

	Chunk chunk;
	float mainChunkSize = 1000000f;

	public override void _Ready()
	{
		base._Ready();

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 9;
		noise.Persistence = 0.2f;
		noise.Period = 2000;
		noise.Lacunarity = 3f;

		Cam = GetParent().GetNode("Camera") as Spatial;
		Car = GetParent().GetNode("Car") as Spatial;

		// Create a root terrain chunk:
		chunk = new Chunk(null, Transform.basis, Vector3.Zero, mainChunkSize, 0, 1, 0);
		AddChild(chunk);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("f2"))
		{
			doUpdate = !doUpdate;
		}

		if (Input.IsActionJustPressed("f3"))
		{
			if (Car == null)
			{
				Car = CarScene.Instance() as Spatial;
				Car.Translation = Cam.Translation;
				GetParent().AddChild(Car);
			}
			else
			{
				GetParent().RemoveChild(Car);
				Car = null;
			}
		}

		UpdateTerrain();
	}

	void UpdateTerrain()
	{
		if (!doUpdate)
			return;
		chunk.Check(Cam.Translation);
		chunk.Update();
	}

	public static Vector3 EvaluatePosition(Vector3 position)
	{
		float n = noise.GetNoise3dv(position);
		position.y = heightCoef * n;
		return position;
	}
}