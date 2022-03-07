extends Spatial

var cam
var camDefaultOffset = Vector3(0, 2, -10)
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
	# Enable wireframe mode in game view:
	VisualServer.set_debug_generate_wireframes(true)
	# Set viewport to draw wireframe:
	#get_viewport().debug_draw = Viewport.DEBUG_DRAW_WIREFRAME
	
	cam = $CamV/Cam
	camV = $CamV
	switch_car_cam()
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _process(delta):
	# Capturing/Freeing the cursor
	if Input.is_action_just_pressed("ui_cancel"):
		if Input.get_mouse_mode() == Input.MOUSE_MODE_VISIBLE:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
		else:
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	# ----------------------------------

	if Input.is_action_just_pressed("f1"):
		var vp = get_viewport()
		if vp.debug_draw == Viewport.DEBUG_DRAW_WIREFRAME:
			vp.debug_draw = Viewport.DEBUG_DRAW_DISABLED
		else:
			vp.debug_draw = Viewport.DEBUG_DRAW_WIREFRAME
	
	if Input.is_action_just_pressed("f3"):
		switch_car_cam()

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
	if car != null:
		if Input.is_action_pressed("scroll_f"):
			cam.translation += cam.transform.basis.z * -0.5
		elif Input.is_action_pressed("scroll_b"):
			cam.translation += cam.transform.basis.z * 0.5
	else:
		if Input.is_action_just_pressed("scroll_f"):
			move_speed = clamp(move_speed + move_speed * 0.1, 0.01, 1000)
			print("Move speed: " + str(move_speed))
		elif Input.is_action_just_pressed("scroll_b"):
			move_speed = clamp(move_speed - move_speed * 0.1, 0.01, 1000)
			print("Move speed: " + str(move_speed))

func switch_car_cam():
	if (car != null):
		car = null
		cam.translation = Vector3.ZERO
	else:
		car = get_parent().get_node_or_null("Car")
		car.linear_velocity = Vector3.ZERO
		car.transform = transform
		car.translation = translation
		car.linear_velocity = Vector3.ZERO
		cam.translation = camDefaultOffset
