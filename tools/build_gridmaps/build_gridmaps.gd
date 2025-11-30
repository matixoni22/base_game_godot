#This tools build from "/models/ex_folder" the the gridmaps to asseds names acordingly to the "ex_folder" 
@tool
class_name BuildGridmaps
extends EditorScript

var gui = preload("res://tools/build_gridmaps/build_gridmaps_gui.tscn")

@export var rendering_scene = "res://sceens/rendering/rendering_3d.tscn"
@export var models_folder = "res://models/gridmaps/"
@export var gridmaps_folder = "res://assets/gridmaps/"
@export var is_preview_scene_generated : bool = false
@export var is_preview_image_generated : bool  = false

signal wait_on_generated_gui
signal await_to_render

var scene: Node
var tree: SceneTree

# Called when the script is executed (using File -> Run in Script Editor).
func _run() -> void:	
	#Create GUI
	var gui_window := Window.new()
	gui_window.close_requested.connect(func():
		gui_window.queue_free()
		pass
	)
	get_editor_interface().popup_dialog(gui_window, Rect2(Vector2(100,100), Vector2(400,200)))
	var gui_scene = gui.instantiate()
	gui_window.add_child(gui_scene)
	(gui_scene.import_input as LineEdit).text = models_folder
	(gui_scene.export_input as LineEdit).text = gridmaps_folder
	(gui_scene.is_preview_scene_input as CheckButton).button_pressed = is_preview_scene_generated
	(gui_scene.is_preview_image_input as CheckButton).button_pressed = is_preview_image_generated
	gui_scene.generated.connect(func():
		wait_on_generated_gui.emit();	
	)
	(gui_scene.generate_button as Button).grab_focus()
	
	await wait_on_generated_gui
	
	#Initialize rendering scene
	get_editor_interface().open_scene_from_path(rendering_scene)
	scene = get_scene()
	tree = scene.get_tree()
	
	#Get input data
	models_folder = gui_scene.import_input.text
	gridmaps_folder = gui_scene.export_input.text
	is_preview_scene_generated = (gui_scene.is_preview_scene_input as CheckButton).button_pressed
	is_preview_image_generated = (gui_scene.is_preview_image_input as CheckButton).button_pressed
#	
	#Do wrok
	var models_dir := DirAccess.open(models_folder)
	if !models_dir: 
		print("Folder %s not found" % models_folder)
		return
	var dirs := models_dir.get_directories()
	for dir in dirs:
		var full_dir_path = models_folder + dir
		var scene_dir := DirAccess.open(full_dir_path)
		var scene_files = filter_glb_files(scene_dir.get_files())
		var isEmpty := scene_files.is_empty();
		if isEmpty:
			continue
		var scene_path := scene_dir.get_current_dir() + "/_%s.tscn" % dir
		var is_scene_exits := scene_dir.file_exists(scene_path)
		var gridmap_scene:PackedScene
		if is_scene_exits:
			print("Updating gridmap scene %s..." % scene_path)
			DirAccess.remove_absolute(scene_path)
			gridmap_scene = create_new_gridmap_scene(scene_path, scene_files)
		else:
			print("Creating new gridmap scene %s..." % scene_path)
			gridmap_scene = create_new_gridmap_scene(scene_path, scene_files)
		
		var mesh_lib_path = gridmaps_folder + "_%s.tres" % dir
		print("Creating new mesh library %s..." % mesh_lib_path)
		var mesh_lib := await create_mesh_library_from_scene(gridmap_scene)
		ResourceSaver.save(mesh_lib, mesh_lib_path)
	gui_window.queue_free()
	pass
	
func create_new_gridmap_scene(scene_path: String, scene_objects: Array[String]) -> PackedScene:
	var new_scene := PackedScene.new()
	var tiles_node := Node3D.new()
	tiles_node.name = "Tiles"
	var scene_dir := scene_path.get_base_dir()
	for scene_object in scene_objects:
		var scene_object_path := scene_dir + "/" + scene_object
		print("... adding %s" % scene_object_path)
		var scene_object_packed_scene:PackedScene = load(scene_object_path)
		var scene_object_node := scene_object_packed_scene.instantiate()
		tiles_node.add_child(scene_object_node)
		scene_object_node.owner = tiles_node
	new_scene.pack(tiles_node)
	ResourceSaver.save(new_scene, scene_path)
	return new_scene

func filter_glb_files(files: PackedStringArray) -> Array[String]:
	var filtered_files : Array[String] = []
	for file in files:
		if file.ends_with(".glb"):
			filtered_files.append(file)
	return filtered_files
	
func create_mesh_library_from_scene(packed_scene: PackedScene) -> MeshLibrary:
	var lib := MeshLibrary.new()
	var mesh_scene := packed_scene.instantiate()
	var id_ref := [0] 
	await collect_meshes(mesh_scene, lib, id_ref)
	return lib
	
func collect_meshes(node: Node3D, lib: MeshLibrary, id_ref: Array):
	if node is MeshInstance3D and node.mesh:
		print("... adding mesh %s" % node.name)
		var id = id_ref[0]
		var preview := await generate_mesh_preview(node.mesh, node.name)
		lib.create_item(id)
		lib.set_item_name(id, node.name)
		lib.set_item_mesh(id, node.mesh)
		lib.set_item_mesh_transform(id, node.transform)
		lib.set_item_mesh_cast_shadow(id, RenderingServer.SHADOW_CASTING_SETTING_ON)
		lib.set_item_shapes(id, [node.mesh.create_trimesh_shape()])
		lib.set_item_navigation_layers(id, 1)
		lib.set_item_preview(id, preview)
		id_ref[0] += 1
		
	elif node is MultiMeshInstance3D and node.multimesh:
		print("... adding multi mesh %s" % node.name)
		var preview := await generate_mesh_preview(node.multimesh.mesh, node.name)
		var id = id_ref[0]
		lib.create_item(id)
		lib.set_item_name(id, node.name)
		lib.set_item_mesh(id, node.multimesh.mesh)
		lib.set_item_mesh_transform(id, node.transform)
		lib.set_item_shapes(id, [node.multimesh.mesh.create_trimesh_shape()])
		lib.set_item_mesh_cast_shadow(id, RenderingServer.SHADOW_CASTING_SETTING_ON)
		lib.set_item_navigation_layers(id, 1)
		lib.set_item_navigation_mesh_transform(id, Transform3D.IDENTITY.basis)
		lib.set_item_preview(id, preview)
		id_ref[0] += 1
		
	for child in node.get_children():
		await collect_meshes(child, lib, id_ref)
	pass
	
func generate_mesh_preview(mesh: Mesh, name: String) -> Texture2D:
	# Define viewport
	var vp = SubViewport.new()
	vp.name = "VP_" + name
	vp.disable_3d = false
	vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	vp.size = Vector2i(256, 256)
	vp.transparent_bg = true
	scene.add_child(vp)
	vp.owner = scene
	
	# Define mesh
	var mi = MeshInstance3D.new()
	mi.name = "MI"
	mi.mesh = mesh
	vp.add_child(mi)
	mi.owner = scene
	
	# Define light
	var light = DirectionalLight3D.new()
	light.name = "LIGHT"
	light.light_energy = 1
	vp.add_child(light)
	light.owner = scene
	
	# Define camera
	var cam = Camera3D.new()
	cam.name = "CAM"
	cam.position = Vector3(2, 1.5, 2)
	cam.rotation_degrees.x = -30.0
	cam.rotation_degrees.y = 45.0
	vp.add_child(cam)
	cam.owner = scene
	
	# Generate sceen - optional
	if is_preview_scene_generated == true:
		var vp_path = gridmaps_folder + "%s.tscn" % name
		print("saving preview scene %s..." % vp_path)
		var packed_scene := PackedScene.new()
		packed_scene.resource_name = "PackedScene_" + name
		packed_scene.pack(vp)
		ResourceSaver.save(packed_scene, vp_path)
		print("...saved!")
	
	await wait_on_root(tree.root, 0.1)
	print("...rendered!")
	
	#Define image
	var tex = vp.get_texture()
	var img_tex := ImageTexture.new()
	var img := tex.get_image()
	img_tex.set_image(img)
	
	if is_preview_image_generated == true:
		var img_path = gridmaps_folder + "%s.png" % name
		print("saving preview image %s..." % img_path)
		img.save_png(img_path)
		print("...saved!")

	#Clean
	vp.queue_free()
	scene.remove_child(vp)
	
	return img_tex

func wait_on_root(root: Window, sec: float) -> void:
	var timer := Timer.new()
	timer.wait_time = sec
	timer.one_shot = true
	timer.autostart = true
	root.add_child(timer)
	timer.owner = root
	timer.timeout.connect(func() ->  void: await_to_render.emit())
	await await_to_render
	timer.queue_free()
	pass
