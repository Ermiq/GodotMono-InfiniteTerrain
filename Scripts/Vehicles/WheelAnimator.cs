using Godot;
using System;

public class WheelAnimator : MeshInstance
{
	// public variables
	public Vector3 WheelOffset = new Vector3(0, 0.85f, 0); // Y = wheel mesh radius
	public float WheelSpeedScaling = 1.0f;
	public float ReturnSpeed = 8.0f;

	// private variables
	RayCastDriver wheelRay;
	Vector3 lastPos = Vector3.Zero;

	public override void _Ready()
	{
		base._Ready();

		// setup references
		wheelRay = GetParent() as RayCastDriver;
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);
		// obtain velocity of the wheel
		var instantV = (GlobalTransform.origin - lastPos) / delta;
		var ZVel = wheelRay.GlobalTransform.basis.XformInv(instantV).z;
		lastPos = GlobalTransform.origin;

		// rotate the wheel according to speed
		RotateX(ZVel * WheelSpeedScaling * delta);

		// set the wheel position
		if (wheelRay.IsColliding())
		{
			Transform transform = Transform;
			transform.origin.y = (wheelRay.ToLocal(wheelRay.GetCollisionPoint()) + WheelOffset).y;
			Transform = transform;
		}
		else
		{
			Transform transform = Transform;
			transform.origin.y = Mathf.Lerp(transform.origin.y, (wheelRay.CastTo + WheelOffset).y, ReturnSpeed * delta);
			Transform = transform;
		}
	}
}
