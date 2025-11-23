@tool
extends EditorPlugin

var renderer: Node

func _enter_tree():
	renderer = preload("res://addons/glb-thumbnailer/preview-renderer.gd").new()
	renderer.name = "GLBPreviewRenderer"
	#add_child(renderer)
	EditorInterface.get_base_control().add_child(renderer)
	print("[PLUGIN] - [GLB Thumbnailer] Loaded")

func _exit_tree():
	renderer.queue_free()


# Called when user presses a custom menu button
func _apply_to_folder(folder: String):
	var dir := DirAccess.open(folder)
	if dir == null:
		return

	dir.list_dir_begin()
	var file = dir.get_next()

	while file != "":
		if file.ends_with(".glb"):
			var glb_path = folder + "/" + file
			var png_path := folder + "/" + file.get_basename() + "_thumbnail.png"

			_generate_thumbnail(glb_path, png_path)

		file = dir.get_next()

	dir.list_dir_end()
	print("[GLB Thumbnailer] Done")


func _generate_thumbnail(glb_path: String, png_path: String):
	print("Rendering: ", glb_path)
	var img: Image = await renderer.render_glb(glb_path)
	img.save_png(png_path)
	print("Saved: ", png_path)
