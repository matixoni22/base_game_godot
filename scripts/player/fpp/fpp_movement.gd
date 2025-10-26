extends CharacterBody3D

@export var JUMP_VELOCITY = 4.5
@export var BASE_SPEED: float = 5.0
@export var SPRINT_MOMENTUM: float = 2.0
@export var MIN_SPRINT_RESOURCE: float = 0
@export var MAX_SPRINT_RESOURCE: float = 100
@export var SPRINT_RESOURCE_LOST_STEP: float = 20
@export var SPRINT_RESOURCE_RESTORE_STEP: float = 20

var SPRINT_BAR: Node
var SPRINT_RESOURCE: float

func _ready() -> void:
	SPRINT_RESOURCE = MAX_SPRINT_RESOURCE
	SPRINT_BAR = get_node("HUD/SprintBar")

func _physics_process(delta: float) -> void:
	# Add the gravity.
	if SPRINT_RESOURCE != MAX_SPRINT_RESOURCE:
		SPRINT_BAR.visible = true
	
	if not is_on_floor():
		velocity += get_gravity() * delta

	# Handle jump.
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "forward", "backward")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	var momentum: float = 1.0;
	if (Input.is_action_pressed("sprint")):
		if (SPRINT_RESOURCE > MIN_SPRINT_RESOURCE):
			momentum = SPRINT_MOMENTUM
			SPRINT_RESOURCE -= SPRINT_RESOURCE_LOST_STEP * delta
			if (SPRINT_RESOURCE < MIN_SPRINT_RESOURCE):
				SPRINT_RESOURCE = MIN_SPRINT_RESOURCE;
			SPRINT_BAR.value = SPRINT_RESOURCE
	else:
		if (SPRINT_RESOURCE < MAX_SPRINT_RESOURCE):
			SPRINT_RESOURCE += SPRINT_RESOURCE_RESTORE_STEP * delta
			if (SPRINT_RESOURCE > MAX_SPRINT_RESOURCE):
				SPRINT_RESOURCE = MAX_SPRINT_RESOURCE;
			SPRINT_BAR.value = SPRINT_RESOURCE
	
	if direction:
		velocity.x = direction.x * BASE_SPEED * momentum
		velocity.z = direction.z * BASE_SPEED * momentum
	else:
		velocity.x = move_toward(velocity.x, 0, BASE_SPEED * momentum)
		velocity.z = move_toward(velocity.z, 0, BASE_SPEED * momentum)
	
	move_and_slide()
