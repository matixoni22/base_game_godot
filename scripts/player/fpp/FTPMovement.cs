using Godot;

public partial class FTPMovement : CharacterBody3D
{
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float BaseSpeed = 5.0f;
    [Export] public float SprintMomentum = 2.0f;
    [Export] public float MinSprintResource = 0.0f;
    [Export] public float MaxSprintResource = 100.0f;
    [Export] public float SprintResourceLostStep = 20.0f;
    [Export] public float SprintResourceRestoreStep = 20.0f;
    [Export] public HSlider SprintBar = new();

    private float SprintResource;

    public override void _Ready()
    {
        SprintResource = MaxSprintResource;
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        
        if (SprintResource != MaxSprintResource) 
            SprintBar.Visible = true;

        // Gravity
        if (!IsOnFloor())
            Velocity += GetGravity() * d;

        // Jump
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);

        // Input
        Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        float momentum = 1.0f;

        // Sprint logic
        if (Input.IsActionPressed("sprint"))
        {
            if (SprintResource > MinSprintResource)
            {
                momentum = SprintMomentum;
                SprintResource -= SprintResourceLostStep * d;
                SprintResource = Mathf.Max(SprintResource, MinSprintResource);
                SprintBar.Value = SprintResource;
            }
        }
        else
        {
            if (SprintResource < MaxSprintResource)
            {
                SprintResource += SprintResourceRestoreStep * d;
                SprintResource = Mathf.Min(SprintResource, MaxSprintResource);
                SprintBar.Value = SprintResource;
            }
        }

        // Movement
        if (direction != Vector3.Zero)
        {
            Velocity = new Vector3(
                direction.X * BaseSpeed * momentum,
                Velocity.Y,
                direction.Z * BaseSpeed * momentum
            );
        }
        else
        {
            Velocity = new Vector3(
                Mathf.MoveToward(Velocity.X, 0, BaseSpeed * momentum),
                Velocity.Y,
                Mathf.MoveToward(Velocity.Z, 0, BaseSpeed * momentum)
            );
        }

        MoveAndSlide();
    }
}
