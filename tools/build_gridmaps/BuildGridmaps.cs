using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

[Tool]
[GlobalClass]
public partial class BuildGridmaps : EditorScript
{
	private BuildGridmapsGui _gui = GD.Load<BuildGridmapsGui>("res://tools/build_gridmaps/build_gridmaps_gui.tscn");
	private PackedScene gui =
		GD.Load<PackedScene>("res://tools/build_gridmaps/build_gridmaps_gui.tscn");

	[Export] public string rendering_scene = "res://sceens/rendering/rendering_3d.tscn";
	[Export] public string models_folder = "res://models/gridmaps/";
	[Export] public string gridmaps_folder = "res://assets/gridmaps/";
	[Export] public bool is_preview_scene_generated = false;
	[Export] public bool is_preview_image_generated = false;

	[Signal] public delegate void wait_on_generated_guiEventHandler();
	[Signal] public delegate void await_to_renderEventHandler();

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

		var guiScene = gui.Instantiate();
		guiWindow.AddChild(guiScene);

		// Load default values
		guiScene.GetNode<LineEdit>("VBoxContainer/ImportPath/ImportInput").Text = models_folder;
		guiScene.GetNode<LineEdit>("export_input").Text = gridmaps_folder;
		guiScene.GetNode<CheckButton>("is_preview_scene_input").ButtonPressed =
			is_preview_scene_generated;
		guiScene.GetNode<CheckButton>("is_preview_image_input").ButtonPressed =
			is_preview_image_generated;

		guiScene.Connect(
			"generated",
			Callable.From(() => EmitSignal(SignalName.wait_on_generated_gui))
		);

		guiScene.GetNode<Button>("generate_button").GrabFocus();

		await ToSignal(this, SignalName.wait_on_generated_gui);

		// Initialize rendering scene
		GetEditorInterface().OpenSceneFromPath(rendering_scene);
		scene = GetScene();
		tree = scene.GetTree();

		// Read inputs
		models_folder = guiScene.GetNode<LineEdit>("import_input").Text;
		gridmaps_folder = guiScene.GetNode<LineEdit>("export_input").Text;
		is_preview_scene_generated =
			guiScene.GetNode<CheckButton>("is_preview_scene_input").ButtonPressed;
		is_preview_image_generated =
			guiScene.GetNode<CheckButton>("is_preview_image_input").ButtonPressed;

		// Work
		var modelsDir = DirAccess.Open(models_folder);
		if (modelsDir == null)
		{
			GD.Print($"Folder {models_folder} not found");
			return;
		}

		foreach (string dir in modelsDir.GetDirectories())
		{
			var fullDirPath = models_folder + dir;
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

			var meshLibPath = $"{gridmaps_folder}_{dir}.tres";
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

		if (is_preview_scene_generated)
		{
			var vpPath = $"{gridmaps_folder}{name}.tscn";
			var ps = new PackedScene();
			ps.Pack(vp);
			ResourceSaver.Save(ps, vpPath);
		}

		await WaitOnRoot(tree!.Root, 0.1f);

		var img = vp.GetTexture().GetImage();
		var tex = ImageTexture.CreateFromImage(img);

		if (is_preview_image_generated)
			img.SavePng($"{gridmaps_folder}{name}.png");

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
		timer.Timeout += () => EmitSignal(SignalName.await_to_render);

		await ToSignal(this, SignalName.await_to_render);
		timer.QueueFree();
	}
}
