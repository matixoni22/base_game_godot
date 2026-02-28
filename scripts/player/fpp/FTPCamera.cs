using Godot;

public partial class FTPCamera : Node3D
{
    private const float Sense = 0.0005f;

    public override void _Ready()
    {
        DisplayServer.MouseSetMode(DisplayServer.MouseMode.Captured);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            GetParent<Node3D>().RotateY(-mouseMotion.Relative.X * Sense);
            RotateX(-mouseMotion.Relative.Y * Sense);

            Vector3 rot = Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            Rotation = rot;
        }
    }
}