using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class World : Spatial
{
	public static Vector3 Up = Vector3.Up;
	public static Vector3 Right = Vector3.Right;
	public static Vector3 Forward = Vector3.Forward;
	public static int heightCoef = 1;

	float originalSize = 100.0f;
	int detail = 100;
	int ringsAmount = 3;
	bool doUpdate = true;

	PackedScene CarScene = ResourceLoader.Load("res://Scenes/Car.tscn") as PackedScene;
	PackedScene CamScene = ResourceLoader.Load("res://Scenes/Camera.tscn") as PackedScene;
	Spatial Car;
	Spatial Cam;
	RayCast Ray;

	Material material = ResourceLoader.Load("res://Terrain.material") as Material;

	OpenSimplexNoise noise;
	Ring[] rings;

	Vector3 playerPosition => Cam.Translation;
	Vector3 playerPreviousPosition;

	Task[] tasks;

	public override void _Ready()
	{
		base._Ready();

		// Enable wireframe mode in game view:
		VisualServer.SetDebugGenerateWireframes(true);

		// Set viewport to draw wireframe:
		// GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;

		noise = new OpenSimplexNoise();
		noise.Seed = (int)OS.GetUnixTime();
		noise.Octaves = 9;
		noise.Persistence = 0.2f;
		noise.Period = 2000;
		noise.Lacunarity = 4f;

		rings = new Ring[ringsAmount];
		// Rings start from 1 and up to 'ringsAmount' inclusive.
		for (int i = 1; i <= ringsAmount; i++)
		{
			// Each ring from the center has size of chunk increased by 3 from the previous ring.
			// Using the geometric progression formula we find the size of a chunk:
			// Tn = T1 * ratio^(n - 1)
			// where T1 is 1st chunk size,
			// ratio is the progression ratio (3 in our case),
			// n (Tn) is the ring index (and the size of its chunks) we need to find:
			float size = originalSize * (float)Mathf.Pow(3, i - 1);

			Ring ring = new Ring(i, noise, material, size, detail, i == 1);

			rings[i - 1] = ring;
			AddChild(ring);
		}

		Cam = GetParent().GetNode("Camera") as Spatial;
		Car = GetParent().GetNode("Car") as Spatial;

		Ray = Cam.GetNode<RayCast>("RayCast");
		if (Car != null)
			Ray.AddException(Car);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("f1"))
		{
			var vp = GetViewport();
			vp.DebugDraw = vp.DebugDraw == Viewport.DebugDrawEnum.Wireframe ? Viewport.DebugDrawEnum.Disabled : Viewport.DebugDrawEnum.Wireframe;
		}

		if (Input.IsActionJustPressed("f2"))
		{
			doUpdate = !doUpdate;
		}

		if (Input.IsActionJustPressed("f3"))
		{
			if (Car == null)
			{
				Car = CarScene.Instance() as Spatial;
				Car.Translation = playerPosition;
				GetParent().AddChild(Car);
				Ray.AddException(Car);
			}
			else
			{
				GetParent().RemoveChild(Car);
				Ray.RemoveException(Car);
				Car = null;
			}
		}

		GetPlayerPosIndex();
	}

	void GetPlayerPosIndex()
	{
		if (!doUpdate)
			return;

		if (playerPosition.DistanceSquaredTo(playerPreviousPosition) > originalSize * originalSize)
		{
			var spaceState = GetWorld().DirectSpaceState;
			// use global coordinates, not local to node
			Godot.Collections.Dictionary result = spaceState.IntersectRay(playerPosition, playerPosition - World.Up * 9999f);
			if (result.Count > 0)
			{
				Vector3 point = (Vector3)result["position"];
				heightCoef = Mathf.FloorToInt(playerPosition.DistanceTo(point) / originalSize) + 1;
			}
			playerPreviousPosition = playerPosition;
			//UpdateRings();
			UpdateRingsAsync();
		}
	}

	void UpdateRings()
	{
		for (int i = 0; i < ringsAmount; i++)
		{
			Ring ring = rings[i];
			ring.ShiftProcess(new Vector3(playerPosition.x, 0, playerPosition.z));
		}
		foreach (Ring ring in rings)
		{
			ring.ShiftApply();
		}
	}

	async void UpdateRingsAsync()
	{
		if (tasks != null)
			return;

		tasks = new Task[ringsAmount];
		for (int i = 0; i < ringsAmount; i++)
		{
			Ring ring = rings[i];
			tasks[i] = Task.Run(() =>
			{
				ring.ShiftProcess(new Vector3(playerPosition.x, 0, playerPosition.z));
			});
		}
		await Task.WhenAll(tasks);
		
		foreach (Ring ring in rings)
		{
			ring.ShiftApply();
		}
		tasks = null;
	}
}