@tool
extends Node

func _ready():
	pass

func render_glb(path: String):
	var svp : SubViewport = load(path).instantiate()
	svp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	# Render viewport
	print("await rendering... %s" % path)
	await get_tree().process_frame
	var img : Image = svp.get_texture().get_image()
	print("...rendered - %s" % img)
	img.save_png("res://models/gridmaps/sandbox/0001-Block-col.png")
