extends Spatial

var camV
var MOUSE_SENSITIVITY = 0.05
var car

# The camera movement speed (tweakable using the mouse wheel)
var move_speed: float = 1.0

# Stores where the camera is wanting to go (based on pressed keys and speed modifier)
var motion: Vector3

# Stores the effective camera velocity
var velocity: Vector3

# The initial camera node rotation
var initial_rotation: float = rotation.y

func _ready():
	camV = $CamV
	car = get_parent().get_node_or_null("Car")
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _process(delta):
	# Capturing/Freeing the cursor
	if Input.is_action_just_pressed("ui_cancel"):
		if Input.get_mouse_mode() == Input.MOUSE_MODE_VISIBLE:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
		else:
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	# ----------------------------------

	if Input.is_action_just_pressed("f3"):
		car = get_parent().get_node_or_null("Car")

	if car != null:
		translation = car.translation
	else:
		if Input.is_action_pressed("ui_up"):
			motion.z = 1
		elif Input.is_action_pressed("ui_down"):
			motion.z = -1
		else:
			motion.z = 0
	
		if Input.is_action_pressed("ui_left"):
			motion.x = 1
		elif Input.is_action_pressed("ui_right"):
			motion.x = -1
		else:
			motion.x = 0
	
		motion.y = 0

		# Normalize motion
		# (prevents diagonal movement from being `sqrt(2)` times faster than straight movement)
		motion = motion.normalized()

		# Speed modifier
		if Input.is_action_pressed("shift"):
			motion *= 2

		# Rotate the motion based on the camera angle
		motion = motion \
			.rotated(Vector3(0, 1, 0), self.rotation.y - initial_rotation) \
			.rotated(Vector3(1, 0, 0), cos(self.rotation.y) * camV.rotation.x) \
			.rotated(Vector3(0, 0, 1), -sin(self.rotation.y) * camV.rotation.x)

		# Add motion, apply friction and velocity
		velocity += motion * move_speed
		velocity *= 0.9 # slow down smoothly
		translation += velocity * delta

func _input(event):
	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		camV.rotate_x(deg2rad(event.relative.y * MOUSE_SENSITIVITY))
		self.rotate_y(deg2rad(event.relative.x * MOUSE_SENSITIVITY * -1))

		var camera_rot = camV.rotation_degrees
		camera_rot.x = clamp(camera_rot.x, -70, 70)
		camV.rotation_degrees = camera_rot