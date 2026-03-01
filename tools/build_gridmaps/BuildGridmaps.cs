using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

#nullable enable

[Tool]
[GlobalClass]
public partial class BuildGridmaps : EditorScript
{
	[Signal] public delegate void WaitOnGeneratedGuiEventHandler();
	[Signal] public delegate void WaitToRenderEventHandler();
	
	[Export] public string RenderingScene = "res://tools/build_gridmaps/rendering_3d.tscn";
	[Export] public string ModelsFolder = "res://models/gridmaps/";
	[Export] public string GridmapsFolder = "res://assets/gridmaps/";
	[Export] public bool IsPreviewSceneGenerated = false;
	[Export] public bool IsPreviewImageGenerated = false;
	[Export] public float GeneratingTimePerEachModelSeconds = 0.1f;


	private PackedScene gui = GD.Load<PackedScene>("res://tools/build_gridmaps/BuildGridmapsGui.tscn");
	private Node? scene;
	private SceneTree? tree;

	// ----------------------------------------------------

	public override async void _Run()
	{
		// Create GUI window
		var guiWindow = new Window();
		guiWindow.CloseRequested += () => guiWindow.QueueFree();

		GetEditorInterface().PopupDialog(
			guiWindow,
			new Rect2I(new Vector2I(100, 100), new Vector2I(400, 200))
		);

		var guiScene = gui.Instantiate<BuildGridmapsGui>();
		guiWindow.AddChild(guiScene);

		// Load default values
		guiScene.ImportInput.Text = ModelsFolder;
		guiScene.ExportInput.Text = GridmapsFolder;
		guiScene.IsPreviewSceneInput.ButtonPressed = IsPreviewSceneGenerated;
		guiScene.IsPreviewImageInput.ButtonPressed = IsPreviewImageGenerated;
		guiScene.GeneratingTimePerEachInput.Text = GeneratingTimePerEachModelSeconds.ToString();
		guiScene.GenerateButton.GrabFocus();

		guiScene.Connect(
			BuildGridmapsGui.SignalName.Generated,
			Callable.From(() => EmitSignal(SignalName.WaitOnGeneratedGui))
		);

		await ToSignal(this, SignalName.WaitOnGeneratedGui);

		// Initialize rendering scene
		GetEditorInterface().OpenSceneFromPath(RenderingScene);
		scene = GetScene();
		tree = scene.GetTree();

		// Read inputs
		ModelsFolder = guiScene.ImportInput.Text;
		GridmapsFolder = guiScene.ExportInput.Text;
		GeneratingTimePerEachModelSeconds = float.Parse(guiScene.GeneratingTimePerEachInput.Text);
		IsPreviewSceneGenerated = guiScene.IsPreviewSceneInput.ButtonPressed;
		IsPreviewImageGenerated = guiScene.IsPreviewImageInput.ButtonPressed;

		// Work
		var modelsDir = DirAccess.Open(ModelsFolder);
		if (modelsDir == null)
		{
			GD.Print($"Folder {ModelsFolder} not found");
			return;
		}

		foreach (string dir in modelsDir.GetDirectories())
		{
			var fullDirPath = ModelsFolder + dir;
			var sceneDir = DirAccess.Open(fullDirPath);
			if (sceneDir == null) continue;

			var sceneFiles = FilterGlbFiles(sceneDir.GetFiles());
			if (sceneFiles.Count == 0) continue;

			var scenePath = $"{sceneDir.GetCurrentDir()}/_{dir}.tscn";
			PackedScene gridmapScene;

			if (sceneDir.FileExists(scenePath))
			{
				GD.Print($"Updating gridmap scene {scenePath}...");
				DirAccess.RemoveAbsolute(scenePath);
			}
			else
			{
				GD.Print($"Creating new gridmap scene {scenePath}...");
			}

			gridmapScene = CreateNewGridmapScene(scenePath, sceneFiles);

			var meshLibPath = $"{GridmapsFolder}_{dir}.tres";
			GD.Print($"Creating new mesh library {meshLibPath}...");

			var meshLib = await CreateMeshLibraryFromScene(gridmapScene);
			ResourceSaver.Save(meshLib, meshLibPath);
		}

		guiWindow.QueueFree();
	}

	// ----------------------------------------------------
	private PackedScene CreateNewGridmapScene(string scenePath, List<string> sceneObjects)
	{
		var newScene = new PackedScene();
		var tilesNode = new Node3D { Name = "Tiles" };

		var sceneDir = scenePath.GetBaseDir();
		foreach (var obj in sceneObjects)
		{
			var objPath = $"{sceneDir}/{obj}";
			GD.Print($"... adding {objPath}");
			var ps = GD.Load<PackedScene>(objPath);
			var inst = ps.Instantiate<Node3D>();
			tilesNode.AddChild(inst);
			inst.Owner = tilesNode;
		}

		newScene.Pack(tilesNode);
		ResourceSaver.Save(newScene, scenePath);
		return newScene;
	}

	private List<string> FilterGlbFiles(string[] files)
	{
		var result = new List<string>();
		foreach (var f in files)
			if (f.EndsWith(".glb"))
				result.Add(f);
		return result;
	}

	private async Task<MeshLibrary> CreateMeshLibraryFromScene(PackedScene packedScene)
	{
		var lib = new MeshLibrary();
		var meshScene = packedScene.Instantiate<Node3D>();
		var idRef = new int[] { 0 };
		await CollectMeshes(meshScene, lib, idRef);
		return lib;
	}

	private async Task CollectMeshes(Node3D node, MeshLibrary lib, int[] idRef)
	{
		if (node is MeshInstance3D mi && mi.Mesh != null)
		{
			GD.Print($"... adding mesh {node.Name}");
			var preview = await GenerateMeshPreview(mi.Mesh, node.Name);

			int id = idRef[0];
			lib.CreateItem(id);
			lib.SetItemName(id, node.Name);
			lib.SetItemMesh(id, mi.Mesh);
			lib.SetItemMeshTransform(id, node.Transform);
			lib.SetItemMeshCastShadow(id, RenderingServer.ShadowCastingSetting.On);
			lib.SetItemShapes(id, [.. new Variant[] { mi.Mesh.CreateTrimeshShape() }]);
			lib.SetItemNavigationLayers(id, 1);
			lib.SetItemPreview(id, preview);
			idRef[0]++;
		}
		else if (node is MultiMeshInstance3D mmi && mmi.Multimesh != null)
		{
			GD.Print($"... adding multi mesh {node.Name}");
			var preview = await GenerateMeshPreview(mmi.Multimesh.Mesh, node.Name);

			int id = idRef[0];
			lib.CreateItem(id);
			lib.SetItemName(id, node.Name);
			lib.SetItemMesh(id, mmi.Multimesh.Mesh);
			lib.SetItemMeshTransform(id, node.Transform);
			lib.SetItemShapes(id, [.. new Variant[] { mmi.Multimesh.Mesh.CreateTrimeshShape() }]);
			lib.SetItemMeshCastShadow(id, RenderingServer.ShadowCastingSetting.On);
			lib.SetItemNavigationLayers(id, 1);
			lib.SetItemNavigationMeshTransform(id, Transform3D.Identity);
			lib.SetItemPreview(id, preview);
			idRef[0]++;
		}

		foreach (Node child in node.GetChildren())
			if (child is Node3D n3)
				await CollectMeshes(n3, lib, idRef);
	}

	private async Task<Texture2D> GenerateMeshPreview(Mesh mesh, string name)
	{
		var vp = new SubViewport
		{
			Name = $"VP_{name}",
			Disable3D = false,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Size = new Vector2I(256, 256),
			TransparentBg = true
		};

		scene!.AddChild(vp);
		vp.Owner = scene;

		var mi = new MeshInstance3D { Mesh = mesh };
		vp.AddChild(mi);

		var light = new DirectionalLight3D { LightEnergy = 1 };
		vp.AddChild(light);

		var cam = new Camera3D
		{
			Position = new Vector3(2, 1.5f, 2),
			RotationDegrees = new Vector3(-30, 45, 0)
		};
		vp.AddChild(cam);

		if (IsPreviewSceneGenerated)
		{
			var vpPath = $"{GridmapsFolder}{name}.tscn";
			var ps = new PackedScene();
			ps.Pack(vp);
			ResourceSaver.Save(ps, vpPath);
		}

		await WaitOnRoot(tree!.Root, GeneratingTimePerEachModelSeconds);

		var img = vp.GetTexture().GetImage();
		var tex = ImageTexture.CreateFromImage(img);

		if (IsPreviewImageGenerated)
			img.SavePng($"{GridmapsFolder}{name}.png");

		vp.QueueFree();
		return tex;
	}

	private async Task WaitOnRoot(Window root, float sec)
	{
		var timer = new Timer
		{
			WaitTime = sec,
			OneShot = true,
			Autostart = true
		};

		root.AddChild(timer);
		timer.Timeout += () => EmitSignal(SignalName.WaitToRender);

		await ToSignal(this, SignalName.WaitToRender);
		timer.QueueFree();
	}
}
