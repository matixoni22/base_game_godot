using Godot;

#nullable enable

[Tool]
public partial class BuildGridmapsGui : MarginContainer
{
	[Signal] public delegate void GeneratedEventHandler();

	[Export] public required LineEdit ImportInput { get; set; }
	[Export] public required LineEdit ExportInput { get; set; }
	[Export] public required LineEdit GeneratingTimePerEachInput { get; set; }
	[Export] public required CheckButton IsPreviewSceneInput { get; set; }
	[Export] public required CheckButton IsPreviewImageInput { get; set; }
	[Export] public required Button GenerateButton { get; set; }

	public override void _Ready()
	{
		ImportInput = GetNode<LineEdit>("VBoxContainer/ImportPath/ImportInput");
		ExportInput = GetNode<LineEdit>("VBoxContainer/ExportPath/ExportInput");
		GeneratingTimePerEachInput = GetNode<LineEdit>("VBoxContainer/GeneratingTimePerEach/GeneratingTimePerEachInput");
		IsPreviewSceneInput = GetNode<CheckButton>("VBoxContainer/IsSceneGenerated/IsPreviewSceneInput");
		IsPreviewImageInput = GetNode<CheckButton>("VBoxContainer/IsPeviewImageGenerated/IsPreviewImageInput");
		GenerateButton = GetNode<Button>("VBoxContainer/HBoxContainer/GenerateButton");

		GenerateButton.Pressed += OnButtonPressed;
	}

	private void OnButtonPressed() => EmitSignal(SignalName.Generated);
}
