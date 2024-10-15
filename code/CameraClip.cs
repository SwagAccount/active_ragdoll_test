using Sandbox;

public sealed class CameraClip : Component
{
	[Property] public float moveSpeed {get;set;} = 100f;
    [Property] public float rotationSpeed {get;set;} = 100f;
    [Property] public float FovChange {get;set;} = 5f;

    CameraComponent cameraComponent;

	protected override void OnStart()
	{
		cameraComponent = Components.Get<CameraComponent>();
	}

	private float yaw = 0f;
    private float pitch = 0f;

    protected override void OnUpdate()
    {
        float horizontal = Input.AnalogMove.y;
        float vertical = Input.AnalogMove.x;

        WorldPosition += (-horizontal * Transform.World.Right + vertical * Transform.World.Forward) * moveSpeed * (Input.Down("Run") ? 2 : 1) * Time.Delta;

        yaw += Input.AnalogLook.yaw * rotationSpeed * Time.Delta;
        pitch += Input.AnalogLook.pitch * rotationSpeed * Time.Delta;
        pitch = MathX.Clamp(pitch, -90f, 90f);

        WorldRotation = new Angles(pitch, yaw, 0f);

        cameraComponent.FieldOfView -= Input.MouseWheel.y * FovChange;
    }
}
