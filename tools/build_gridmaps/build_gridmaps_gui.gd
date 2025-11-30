@tool
extends MarginContainer

signal generated

@onready var import_input: LineEdit = $VBoxContainer/ImportPath/ImportInput
@onready var export_input: LineEdit = $VBoxContainer/ExportPath/ExportInput
@onready var is_preview_scene_input: CheckButton = $VBoxContainer/IsSceneGenerated/IsPreviewSceneInput
@onready var is_preview_image_input: CheckButton = $VBoxContainer/IsPeviewImageGenerated/IsPreviewImageInput
@onready var generate_button: Button = $VBoxContainer/HBoxContainer/GenerateButton

func _ready() -> void:
	generate_button.pressed.connect(_on_button_pressed)
	
func _on_button_pressed():
	generated.emit()
