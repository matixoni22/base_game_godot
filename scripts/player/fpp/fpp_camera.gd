extends Node3D

const SENSE: float  = 0.0005

func _ready() -> void:
	DisplayServer.mouse_set_mode(DisplayServer.MOUSE_MODE_CAPTURED);
	
func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		get_parent().rotate_y(-event.relative.x * SENSE)
		rotate_x(-event.relative.y * SENSE)
		rotation.x = clamp(rotation.x, deg_to_rad(-90), deg_to_rad(90))
