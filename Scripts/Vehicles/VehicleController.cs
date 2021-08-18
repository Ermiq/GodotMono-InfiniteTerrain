using Godot;
using System;
using System.Collections.Generic;

public class VehicleController : RigidBody
{
	// control variables
	public float EnginePower = 20.0f;
	public float SteeringAngle = 20.0f;
	// currently, raycast driver expects this array to exist in the controller script
	public List<RayCastDriver> RayElements = new List<RayCastDriver>();
	float drivePerRay; // = EnginePower;
	RayCastDriver frontRightWheel;
	RayCastDriver frontLeftWheel;

	public override void _Ready()
	{
		base._Ready();

		drivePerRay = EnginePower;

		// setup front right and front left wheels
		frontLeftWheel = GetNode<RayCastDriver>("FL_ray");
		frontRightWheel = GetNode<RayCastDriver>("FR_ray");

		// setup array of drive elements and setup drive power
		foreach (Node node in GetChildren())
		{
			if (node is RayCastDriver)
				RayElements.Add(node as RayCastDriver);
		}
		drivePerRay = EnginePower / RayElements.Count;
		GD.Print("Found ", RayElements.Count, " raycasts connected to wheeled vehicle, setting to provide ", drivePerRay, " power each.");
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		// 4WD with front wheel steering
		foreach (RayCastDriver ray in RayElements)
		{
			var dir = 0;
			if (Input.IsActionPressed("ui_up"))
				dir += 1;
			if (Input.IsActionPressed("ui_down"))
				dir -= 1;
			// steering, set wheels initially straight
			frontLeftWheel.RotationDegrees = new Vector3(frontLeftWheel.RotationDegrees.x, 0.0f, frontLeftWheel.RotationDegrees.z);
			frontRightWheel.RotationDegrees = new Vector3(frontRightWheel.RotationDegrees.x, 0.0f, frontRightWheel.RotationDegrees.z);
			// if input provided, steer
			if (Input.IsActionPressed("ui_left"))
			{
				frontLeftWheel.RotationDegrees = new Vector3(frontLeftWheel.RotationDegrees.x, SteeringAngle, frontLeftWheel.RotationDegrees.z);
				frontRightWheel.RotationDegrees = new Vector3(frontRightWheel.RotationDegrees.x, SteeringAngle, frontRightWheel.RotationDegrees.z);
			}
			if (Input.IsActionPressed("ui_right"))
			{
				frontLeftWheel.RotationDegrees = new Vector3(frontLeftWheel.RotationDegrees.x, -SteeringAngle, frontLeftWheel.RotationDegrees.z);
				frontRightWheel.RotationDegrees = new Vector3(frontRightWheel.RotationDegrees.x, -SteeringAngle, frontRightWheel.RotationDegrees.z);
			}

			ray.ApplyDriveForce(dir * GlobalTransform.basis.z * drivePerRay * delta);
		}
	}
}