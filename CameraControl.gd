extends Camera

const MOUSE_SENSITIVITY = 0.002

# The camera movement speed (tweakable using the mouse wheel)
var move_speed := 0.5

# Stores where the camera is wanting to go (based on pressed keys and speed modifier)
var motion := Vector3()

# Stores the effective camera velocity
var velocity := Vector3()

# The initial camera node rotation
var initial_rotation := rotation.y

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _input(event: InputEvent) -> void:
	# Mouse look (effective only if the mouse is captured)
	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		# Horizontal mouse look
		rotation.y -= event.relative.x * MOUSE_SENSITIVITY
		# Vertical mouse look, clamped to -90..90 degrees
		rotation.x = clamp(rotation.x - event.relative.y * MOUSE_SENSITIVITY, deg2rad(-90), deg2rad(90))

	if event.is_action_pressed("scroll_f"):
		move_speed = min(1.5, move_speed + 0.1)

	if event.is_action_pressed("scroll_b"):
		move_speed = max(0.1, move_speed - 0.1)


func _process(delta: float) -> void:
	# Toggle mouse capture (only while the menu is not visible)
	if Input.is_action_just_released("ui_cancel"):
		if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
		else:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	if Input.is_action_pressed("move_f"):
		motion.z = -1
	elif Input.is_action_pressed("move_b"):
		motion.z = 1
	else:
		motion.z = 0
	
	if Input.is_action_pressed("move_l"):
		motion.x = -1
	elif Input.is_action_pressed("move_r"):
		motion.x = 1
	else:
		motion.x = 0
	
	motion.y = 0

	# Normalize motion
	# (prevents diagonal movement from being `sqrt(2)` times faster than straight movement)
	motion = motion.normalized()

	# Speed modifier
	if Input.is_action_pressed("move_shift"):
		motion *= 2

	# Rotate the motion based on the camera angle
	motion = motion \
		.rotated(Vector3(0, 1, 0), rotation.y - initial_rotation) \
		.rotated(Vector3(1, 0, 0), cos(rotation.y) * rotation.x) \
		.rotated(Vector3(0, 0, 1), -sin(rotation.y) * rotation.x)

	# Add motion, apply friction and velocity
	velocity += motion * move_speed
	velocity *= 0.9
	translation += velocity * delta


func _exit_tree() -> void:
	# Restore the mouse cursor upon quitting
	Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
