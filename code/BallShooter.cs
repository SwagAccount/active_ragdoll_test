using System;
using System.Threading;
using Sandbox;

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
