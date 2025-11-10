extends Node

var WORLD_ENVIRONMENT: WorldEnvironment
func _ready() -> void:
	WORLD_ENVIRONMENT = WorldEnvironment.new()
	WORLD_ENVIRONMENT.environment = load("res://assets/materials/world.tres")
	add_child(WORLD_ENVIRONMENT);
