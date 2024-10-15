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
	[Property] public float FootRadius {get;set;} = 0.5f;
	[Property] public List<GameObject> Feet {get;set;}
	public List<BodyBone> FeetBodyBones {get;set;}

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
	[Property] public float FeetTipDistance {get;set;} = 10f;
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

	public class BodyBone
	{
		public PhysicsBody Body { get; set; }
		public GameObject Bone { get; set; }
		public List<BodyBone> Children { get; set; } = new List<BodyBone>();

		private float _strength;
		[KeyProperty]
		public float Strength
		{
			get => _strength;
			set
			{
				value = MathX.Clamp(value,0f,100f);
				foreach (var child in Children)
				{
					child.Strength -= _strength - value;
				}
				_strength = value;
			}
		}

		[KeyProperty]
		public string Name => Bone.Name;
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

			BodyBone bodyBone = new BodyBone { Body = body, Bone = bone, Strength = 100};

			foreach(var bB in BodyBones)
			{
				if(bB.Bone != bone.Parent && bB.Bone != bone.Parent.Parent)
				{
					continue;
				}
				bB.Children.Add(bodyBone);
				break;
			}

			BodyBones.Add( bodyBone );
		}

		FeetBodyBones = new List<BodyBone>();

		foreach(var foot in Feet)
		{
			foreach(var bodyBone in BodyBones)
			{
				if(bodyBone.Body.GroupName.ToLower() != foot.Name.ToLower()) continue;
				FeetBodyBones.Add(bodyBone);
				break;
			}
		}
	}
	protected override void OnUpdate()
	{
		RagdollChecks();

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

	[Property] bool down => frontGetUp ? timeSinceGetUp < frontUpTime : timeSinceGetUp < backUpTime;
	void RagdollChecks()
	{

		JimmyBones.Set("grounded_back", !frontGetUp && timeSinceGetUp < backUpTime);
		JimmyBones.Set("grounded_front", frontGetUp && timeSinceGetUp < frontUpTime);

		if(Ragdolled || timeSinceGetUp < 0.4f)
		{
			characterController.Move();
			
			var rotationDifference = Rotation.Difference(BodyBones[0].Bone.LocalRotation, BodyBones[0].Body.Rotation);
            WorldRotation = rotationDifference;

			Vector3 currentDirection = Transform.World.Up;

			Rotation rotationToTarget = Rotation.FromToRotation(currentDirection, Vector3.Up);

			WorldRotation = rotationToTarget * WorldRotation;

			WorldPosition += BodyBones[0].Body.Position - BodyBones[0].Bone.WorldPosition;
			
			if(Ragdolled && characterController.IsOnGround)
			{
				ragdollTime += Time.Delta;
				if(ragdollTime >= RagdollTime)
				{
					Ragdolled = false;
				}
			}
			return;
		}

		bool feetFall = false;

		for (int i = 0; i < Feet.Count && !down; i++)
		{
			feetFall = true;
			if(Vector3.DistanceBetween(FeetBodyBones[i].Bone.WorldPosition,FeetBodyBones[i].Body.Position) > FeetTipDistance)
				break;

			var foot = Feet[i];
			var collisions = Scene.FindInPhysics(new Sphere(foot.Children[0].WorldPosition, FootRadius));

			foreach (var collision in collisions)
			{
				if (collision.IsAncestor(GameObject)) continue;
				feetFall = false;
				break;
			}
			if (feetFall) break;
		}



		if(
			Vector3.DistanceBetween(BodyBones[0].Body.Position, BodyBones[0].Bone.WorldPosition) > TipDistance || 
			feetFall
			)
			Ragdolled = true;
	}

	bool frontGetUp;

	void MatchBones()
	{
		for(int i = 0; i < BodyBones.Count; i++)
		{
			BodyBones[i].Body.UseController = !Ragdolled && BodyBones[i].Strength >= 0.1f;
			if(Ragdolled) continue;
			if (BodyBones[i].Strength <= 0.1f) BodyBones[i].Body.ApplyForce(Vector3.Up*-9.8f*Time.Delta);
			else BodyBones[i].Body.Move(BodyBones[i].Bone.WorldTransform,(10/BodyBones[i].Strength) / MathX.Clamp(MathX.Lerp(0,1,timeSinceGetUp/GetUpEase),0.1f,1));
		}
	}

	[Property]bool lastRagdolled;
}
