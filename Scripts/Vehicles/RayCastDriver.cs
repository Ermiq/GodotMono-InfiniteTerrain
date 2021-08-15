using Godot;
using System;

public class RayCastDriver : RayCast
{
	// control variables
	public float MaxForce = 300.0f;
	public float SpringForce = 180.0f;
	public float Stifness = 0.85f;
	public float Damping = 0.05f;
	public float Xtraction = 1.0f;
	public float Ztraction = 0.15f;

	// public variables
	public Vector3 InstantLinearVelocity;

	// private variables
	VehicleController parentBody;
	float previousDistance;
	Vector3 previousHit = Vector3.Zero;

	public override void _Ready()
	{
		base._Ready();

		// setup references (only need to get once, should be more efficient?)
		parentBody = GetParent() as VehicleController;
		AddException(parentBody);
	}

	// function for applying drive force to parent body (if grounded)
	public void ApplyDriveForce(Vector3 force)
	{
		if (IsColliding())
		{
			parentBody.ApplyImpulse(parentBody.GlobalTransform.basis.Xform(parentBody.ToLocal(GetCollisionPoint())), force);
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		// if grounded, handle forces
		if (IsColliding())
		{
			// obtain instantaneaous linear velocity
			var curHit = GetCollisionPoint();
			InstantLinearVelocity = (curHit - previousHit) / delta;

			// apply spring force with damping force
			var curDistance = (GlobalTransform.origin - GetCollisionPoint()).Length();
			var FSpring = Stifness * (Mathf.Abs(CastTo.y) - curDistance);
			var FDamp = Damping * (previousDistance - curDistance) / delta;
			var suspensionForce = Mathf.Clamp((FSpring + FDamp) * SpringForce, 0, MaxForce);
			var suspensionImpulse = GlobalTransform.basis.y * suspensionForce * delta;

			// obtain axis velocity
			var ZVelocity = GlobalTransform.basis.XformInv(InstantLinearVelocity).z;
			var XVelocity = GlobalTransform.basis.XformInv(InstantLinearVelocity).x;

			// axis deceleration forces
			var XForce = -GlobalTransform.basis.x * XVelocity * (parentBody.Weight * parentBody.GravityScale) / parentBody.RayElements.Count * Xtraction * delta;
			var ZForce = -GlobalTransform.basis.z * ZVelocity * (parentBody.Weight * parentBody.GravityScale) / parentBody.RayElements.Count * Ztraction * delta;

			// counter sliding by negating off axis suspension impulse
			XForce.x -= suspensionImpulse.x * parentBody.GlobalTransform.basis.y.Dot(Vector3.Up);
			ZForce.z -= suspensionImpulse.z * parentBody.GlobalTransform.basis.y.Dot(Vector3.Up);

			// final impulse force vector to be applied
			var finalForce = suspensionImpulse + XForce + ZForce;

			// note that the point has to be xform()'ed to be at the correct location. Xform makes the pos global
			parentBody.ApplyImpulse(parentBody.GlobalTransform.basis.Xform(parentBody.ToLocal(GetCollisionPoint())), finalForce);
			previousDistance = curDistance;
			previousHit = curHit;
		}
		else
		{
			// not grounded, set prev values to fully extended suspension
			previousDistance = -CastTo.y;
			previousHit = ToGlobal(CastTo);
		}
	}
}