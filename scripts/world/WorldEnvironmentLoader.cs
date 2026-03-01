using Godot;

public partial class WorldEnvironmentLoader : Node
{
    [Export] public Environment Environment = new();
    private WorldEnvironment WorldEnvironmentNode;

    public override void _Ready()
    {
        WorldEnvironmentNode = new WorldEnvironment
        {
            Environment = Environment
        };
        AddChild(WorldEnvironmentNode);
    }
}