using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class World : Spatial
{
	TerrainSettings settings;
	Chunk rootChunk;
	bool doUpdate = true;

	Spatial Cam;

	public override void _Ready()
	{
		base._Ready();

		Cam = GetParent().GetNode("Camera") as Spatial;

		settings = new TerrainSettings();

		rootChunk = new Chunk(null, Transform.basis.Orthonormalized(), Vector3.Zero, settings.rootChunkSize, settings, 0);
		AddChild(rootChunk);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("f2"))
		{
			doUpdate = !doUpdate;
		}

		UpdateTerrain();
	}

	void UpdateTerrain()
	{
		if (!doUpdate)
			return;

		rootChunk.Check(Cam.GlobalTransform.origin);
		rootChunk.Update();
	}
}