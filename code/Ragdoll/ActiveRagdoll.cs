using System;
using Sandbox;

public sealed class ActiveRagdoll : Component
{
	[Property] public ModelPhysics JimmyBody {get;set;}
	[Property] public SkinnedModelRenderer JimmyBones {get;set;}
	[Property] public List<BodyBone> BodyBones {get;set;}
	[Property] public float backUpTime {get;set;}
	[Property] public float frontUpTime {get;set;}
	[Property] public float GetUpEase {get;set;} = 0.5f;

	CharacterController characterController;

	bool _ragdolled;
	[Property] public bool Ragdolled {
		get
		{
			return _ragdolled;
		}
		set
		{
			if(value)
			{
				ragdollTime = 0;
			}
			else
			{
				frontGetUp = Vector3.GetAngle(Vector3.Up,BodyBones[0].Body.Transform.Left) > 90;
				GetUpTime = Time.Now;
			}

			_ragdolled = value;
		}
	}
	[Property] public float TipDistance {get;set;} = 10f;
	[Property] public float RagdollTime {get;set;} = 5f;

	[Button]
	public void LogBodies()
	{
		var bodies = JimmyBody.PhysicsGroup.Bodies;

		foreach(var body in bodies)
		{
			Log.Info(body.GroupName);
		}
	}

	public struct BodyBone
	{
		public PhysicsBody Body {get;set;}
		public GameObject Bone {get;set;}
		[KeyProperty] public float Strength {get;set;}
		[KeyProperty] string name => Bone.Name;
	}
	protected override void OnStart()
	{
		characterController = Components.Get<CharacterController>();

		var bodies = JimmyBody.PhysicsGroup.Bodies;

		BodyBones = new List<BodyBone>();

		foreach(var body in bodies)
		{
			GameObject bone = JimmyBones.GetBoneObject(body.GroupName);

			if(bone == null) return;

			body.UseController = true;

			BodyBones.Add(
				new BodyBone { Body = body, Bone = bone, Strength = 100}
			);
		}
	}
	protected override void OnUpdate()
	{
		RagdolledCheck();

		MatchBones();

		PositionCheck();

		lastRagdolled = Ragdolled;
	}

	void PositionCheck()
	{
		if(Ragdolled || timeSinceGetUp < (frontGetUp ? frontUpTime : backUpTime)) return;
		characterController.Move();
	}
	float GetUpTime = -1000f;
	float ragdollTime;

	float timeSinceGetUp => Time.Now - GetUpTime;
	void RagdolledCheck()
	{

		JimmyBones.Set("grounded_back", !frontGetUp && timeSinceGetUp < backUpTime);
		JimmyBones.Set("grounded_front", frontGetUp && timeSinceGetUp < frontUpTime);
		if(Ragdolled || timeSinceGetUp < 0.4f)
		{

			var rotationDifference = Rotation.Difference(BodyBones[0].Bone.LocalRotation, BodyBones[0].Body.Rotation);
            WorldRotation = rotationDifference;

			Vector3 currentDirection = Transform.World.Up;

			Rotation rotationToTarget = Rotation.FromToRotation(currentDirection, Vector3.Up);

			WorldRotation = rotationToTarget * WorldRotation;

			WorldPosition += BodyBones[0].Body.Position - BodyBones[0].Bone.WorldPosition;

			if(Ragdolled)
			{
				ragdollTime += Time.Delta;
				if(ragdollTime >= RagdollTime)
				{
					Ragdolled = false;
				}
			}
			return;
		}

		if(Vector3.DistanceBetween(BodyBones[0].Body.Position, BodyBones[0].Bone.WorldPosition) > TipDistance)
		Ragdolled = true;
	}

	bool frontGetUp;

	void MatchBones()
	{
		for(int i = 0; i < BodyBones.Count; i++)
		{
			BodyBones[i].Body.UseController = !Ragdolled;
			if(Ragdolled) continue;
			BodyBones[i].Body.Move(BodyBones[i].Bone.WorldTransform,(10/BodyBones[i].Strength) / MathX.Clamp(MathX.Lerp(0,1,timeSinceGetUp/GetUpEase),0.1f,1));
		}
	}

	[Property]bool lastRagdolled;
}
