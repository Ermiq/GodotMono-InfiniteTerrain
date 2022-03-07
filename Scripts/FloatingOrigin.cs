using Godot;
using System;

public class FloatingOrigin : Spatial
{
	public static event Action<Vector3> Event_OriginShift;

	float threshold = 10000.0f;
	Spatial camera;

	public override void _Ready()
	{
		camera = GetNode("/root/Node/Camera") as Spatial;
		Event_OriginShift?.Invoke(Vector3.Zero);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		// Check distance of world from camera and shift if greater than threshold
		if (camera.Translation.LengthSquared() > threshold * threshold)
		{
			Vector3 offset = camera.Translation;
			camera.Translation -= offset;
			Event_OriginShift?.Invoke(offset);
		}
	}
}