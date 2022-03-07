extends ColorRect

var camera: Spatial
var world: Spatial

func _ready():
	camera = get_node("/root/Node/Camera")
	world = get_node("/root/Node/World")

func _process(_delta):
	$FPSLabel.text = str(Engine.get_frames_per_second())
	$WorldPosLabel.text = str(world.global_transform.origin)
	$CameraPosLabel.text = str(camera.global_transform.origin)
