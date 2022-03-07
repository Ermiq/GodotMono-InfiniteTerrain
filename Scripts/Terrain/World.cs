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

		FloatingOrigin.Event_OriginShift += OnOriginShift;

		Cam = GetParent().GetNode("Camera") as Spatial;

		settings = new TerrainSettings(0);

		// Create a transform for the root chunk orientation reference:
		Transform tr = Transform;
		tr.basis.x = Vector3.Right;
		tr.basis.y = Vector3.Up;
		tr.basis.z = -Vector3.Forward;
		tr.basis = tr.basis.Orthonormalized();

		rootChunk = new Chunk(null, tr.basis, tr.basis.y * settings.altitudeBase, settings.rootChunkSize, settings, 0);
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

		rootChunk.Update(ToLocal(Cam.GlobalTransform.origin));
	}

	void OnOriginShift(Vector3 offset)
	{
		Translation -= offset;
	}
}