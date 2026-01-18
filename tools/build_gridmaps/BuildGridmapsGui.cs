using Godot;

#nullable enable

[Tool]
public partial class BuildGridmapsGui : MarginContainer
{
	[Signal]
	public delegate void GeneratedEventHandler();


	// Optional getters if other scripts need access
	public required LineEdit ImportInput { get; set; }
	public required LineEdit ExportInput { get; set; }
	public required CheckButton IsPreviewSceneInput { get; set; }
	public required CheckButton IsPreviewImageInput { get; set; }

	private Button? _generateButton { get; set; }

	public override void _Ready()
	{
		ImportInput = GetNode<LineEdit>("VBoxContainer/ImportPath/ImportInput");
		ExportInput = GetNode<LineEdit>("VBoxContainer/ExportPath/ExportInput");
		IsPreviewSceneInput = GetNode<CheckButton>("VBoxContainer/IsSceneGenerated/IsPreviewSceneInput");
		IsPreviewImageInput = GetNode<CheckButton>("VBoxContainer/IsPeviewImageGenerated/IsPreviewImageInput");
		_generateButton = GetNode<Button>("VBoxContainer/HBoxContainer/GenerateButton");

		_generateButton.Pressed += OnButtonPressed;
	}

	private void OnButtonPressed()
	{
		EmitSignal(SignalName.Generated);
	}
}
