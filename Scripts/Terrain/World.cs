using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class World : Spatial
{
	public static Vector3 Up = Vector3.Up;
	public static Vector3 Right = Vector3.Right;
	public static Vector3 Forward = Vector3.Forward;

	// A reference size for a chunk, representing the minimim size:
	public static float chunkSize = 100.0f;

	// The amount of quads in a chunk's row/line:
	public static int detail = 50;

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
		noise.Lacunarity = 4f;

		Cam = GetParent().GetNode("Camera") as Spatial;
		Car = GetParent().GetNode("Car") as Spatial;

		// Create a root terrain chunk:
		chunk = new Chunk(Vector3.Zero, mainChunkSize, true);
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

		chunk.Update(Cam.Translation);
	}

	public static Vector3 EvaluatePosition(Vector3 position)
	{
		float n = noise.GetNoise3dv(position);
		position.y = heightCoef * n;
		return position;
	}
}