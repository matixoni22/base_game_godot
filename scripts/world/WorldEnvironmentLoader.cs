using Godot;

public partial class WorldEnvironmentLoader : Node
{
    private WorldEnvironment WorldEnvironmentNode;

    public override void _Ready()
    {
        WorldEnvironmentNode = new WorldEnvironment();
        WorldEnvironmentNode.Environment =
            ResourceLoader.Load<Environment>("res://assets/materials/world.tres");

        AddChild(WorldEnvironmentNode);
    }
}