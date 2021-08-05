extends KinematicBody

const GRAVITY = -24.8
export var use_gravity : bool = false
var vel = Vector3()
const MIN_SPEED = 1
const MAX_SPEED = 300
const JUMP_SPEED = 18
const ACCEL = 4.5

var dir = Vector3()

const DEACCEL= 16
const MAX_SLOPE_ANGLE = 40

var camera
var cam_rotator

var current_speed = 20
var keepMoving = false
var input_movement_vector

var MOUSE_SENSITIVITY = 0.05

func _ready():
	camera = $CamRotator/Camera
	cam_rotator = $CamRotator

	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _process(delta):
	process_input(delta)

func _physics_process(delta):
	process_movement(delta)

func process_input(delta):

	# ----------------------------------
	# Walking
	dir = Vector3()
	var cam_xform = camera.get_global_transform()

	if not keepMoving:
		input_movement_vector = Vector2()

	if Input.is_action_pressed("move_f"):
		input_movement_vector.y += 1
	if Input.is_action_pressed("move_b"):
		input_movement_vector.y -= 1
	if Input.is_action_pressed("move_l"):
		input_movement_vector.x -= 1
	if Input.is_action_pressed("move_r"):
		input_movement_vector.x += 1
	if Input.is_action_just_pressed("move_shift"):
		keepMoving = !keepMoving
	
	input_movement_vector.x = clamp(input_movement_vector.x, -1, 1);
	input_movement_vector.y = clamp(input_movement_vector.y, -1, 1);
	input_movement_vector = input_movement_vector.normalized()

	# Basis vectors are already normalized.
	dir += -cam_xform.basis.z * input_movement_vector.y
	dir += cam_xform.basis.x * input_movement_vector.x
	# ----------------------------------

	# ----------------------------------
	# Jumping
	if Input.is_action_just_pressed("jump"):
		if is_on_floor():
			vel.y = JUMP_SPEED
		else:
			use_gravity = !use_gravity
	# ----------------------------------

	# ----------------------------------
	# Capturing/Freeing the cursor
	if Input.is_action_just_pressed("ui_cancel"):
		if Input.get_mouse_mode() == Input.MOUSE_MODE_VISIBLE:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
		else:
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	# ----------------------------------

func process_movement(delta):
	dir.y = 0
	dir = dir.normalized()

	if use_gravity:
		vel.y += delta * GRAVITY

	var hvel = vel
	hvel.y = 0

	var target = dir
	target *= current_speed

	var accel
	if dir.dot(hvel) > 0:
		accel = ACCEL
	else:
		accel = DEACCEL

	hvel = hvel.linear_interpolate(target, accel * delta)
	vel.x = hvel.x
	vel.z = hvel.z
	vel = move_and_slide(vel, Vector3(0, 1, 0), 0.05, 4, deg2rad(MAX_SLOPE_ANGLE))

func _input(event):
	if event is InputEventMouseMotion and Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		cam_rotator.rotate_x(deg2rad(-event.relative.y * MOUSE_SENSITIVITY))
		self.rotate_y(deg2rad(event.relative.x * MOUSE_SENSITIVITY * -1))

		var camera_rot = cam_rotator.rotation_degrees
		camera_rot.x = clamp(camera_rot.x, -70, 70)
		cam_rotator.rotation_degrees = camera_rot
	
	if event.is_action_pressed("scroll_f"):
		current_speed = min(MAX_SPEED, current_speed + current_speed * 0.1)
	
	if event.is_action_pressed("scroll_b"):
		current_speed = max(MIN_SPEED, current_speed - current_speed * 0.1)
