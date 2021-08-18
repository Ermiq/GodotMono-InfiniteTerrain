using Godot;
using System;
using System.Collections.Generic;

public class VehicleController : RigidBody
{
	// control variables
	public float EnginePower = 200.0f;
	public float SteeringAngle = 30.0f;
	// currently, raycast driver expects this array to exist in the controller script
	public List<RayCastDriver> RayElements = new List<RayCastDriver>();
	float drivePerRay; // = EnginePower;
	RayCastDriver frontRightWheel;
	RayCastDriver frontLeftWheel;
	float steerValue;

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
			float dir = 0;
			if (Input.IsActionPressed("ui_up"))
				dir += 1;
			if (Input.IsActionPressed("ui_down"))
				dir -= 1;
			// faster down the slope, slower up the hill
			dir -= GlobalTransform.basis.z.Dot(Vector3.Up);
			
			// if input provided, steer
			if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("ui_right"))
			{
				if (Input.IsActionPressed("ui_left"))
					steerValue = Mathf.MoveToward(steerValue, SteeringAngle, SteeringAngle * delta);
				if (Input.IsActionPressed("ui_right"))
					steerValue = Mathf.MoveToward(steerValue, -SteeringAngle, SteeringAngle * delta);
			}
			else
			{
				steerValue = Mathf.MoveToward(steerValue, 0, SteeringAngle * delta);
			}
			frontLeftWheel.RotationDegrees = new Vector3(frontLeftWheel.RotationDegrees.x, steerValue, frontLeftWheel.RotationDegrees.z);
			frontRightWheel.RotationDegrees = new Vector3(frontRightWheel.RotationDegrees.x, steerValue, frontRightWheel.RotationDegrees.z);

			ray.ApplyDriveForce(dir * GlobalTransform.basis.z * drivePerRay * delta);
		}
	}
}