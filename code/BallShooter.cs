using System;
using System.Threading;
using Sandbox;
using Sandbox.Physics;

public sealed class BallShooter : Component
{
	[Property] public float Size {get;set;} = 6f;
	[Property] public float Speed {get;set;} = 2000f;
	[Property] public float Force {get;set;} = 2000f;
	protected override void OnUpdate()
	{
		if(Input.Pressed("attack1"))
			SpawnBall();
		if(Input.Pressed("attack2"))
			ApplyForce();

		if(Input.Pressed("Use"))
			SelectBone();

		if(Input.Released("Use"))
			ReleaseBone();
	}

	[Property] Rigidbody connectedBody;

	Sandbox.Physics.FixedJoint joint;

	void SelectBone()
	{
		var trace = Scene.Trace.Ray(WorldPosition,WorldPosition+Transform.World.Forward*1024f).Run();
		
		if(!trace.Hit)
			return;

		
		if(!trace.Body.IsValid())
			return;

		if(connectedBody.IsValid())
			connectedBody.GameObject.Destroy();
		
		connectedBody = new GameObject().Components.Create<Rigidbody>();
		connectedBody.WorldPosition = trace.EndPosition;
		connectedBody.MotionEnabled = false;
		connectedBody.GameObject.SetParent(GameObject);

		var p1 = new PhysicsPoint(connectedBody.PhysicsBody, connectedBody.Transform.Position);
		var p2 = new PhysicsPoint(trace.Body,trace.Body.Position);

		joint = PhysicsJoint.CreateFixed(p1,p2);
		joint.SpringLinear = new PhysicsSpring(100, 5);
		joint.SpringAngular = new PhysicsSpring(100, 5);
	}
	void ReleaseBone()
	{
		joint.Remove();
		joint = null;
		if(connectedBody.IsValid())
			connectedBody.GameObject.Destroy();
	}

	void SpawnBall()
	{
		GameObject ball = new GameObject();
		ball.WorldPosition = WorldPosition;
		ball.WorldRotation = WorldRotation;
		ball.WorldScale = Size/32;
		ModelRenderer modelRenderer = ball.Components.Create<ModelRenderer>();
		modelRenderer.Model = Model.Load("models/dev/sphere.vmdl");
		Rigidbody rigidbody = ball.Components.Create<Rigidbody>();
		rigidbody.Velocity = Transform.World.Forward*Speed;
		ball.Components.Create<SphereCollider>().Radius = 32;
	}

	void ApplyForce()
	{
		var trace = Scene.Trace.Ray(WorldPosition,WorldPosition+Transform.World.Forward*1024f).Run();

		if(!trace.Hit)
			return;

		Sound.Play(trace.Surface.Sounds.ImpactHard, trace.HitPosition);
		string decal = "";
        var decals = trace.Surface.ImpactEffects.BulletDecal;
        if ((decals?.Count() ?? 0) > 0)
            decal = decals.OrderBy(x => Random.Shared.Float()).FirstOrDefault();
		
		var body = trace.GameObject.Components.Get<ModelPhysics>()?.PhysicsGroup?.GetBody(trace.Body.GroupIndex);
		if( body.IsValid() )
		{
			body.ApplyForce( Transform.World.Forward * Force * 1000f);
			return;
		}

		var rb = trace.GameObject.Components.Get<Rigidbody>();
		if(rb.IsValid())
		{
			rb.Velocity += Force/rb.PhysicsBody.Mass;
			return;
		}

	}
}
