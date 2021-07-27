extends Label

func _ready():
	pass # Replace with function body.


func _process(delta):
	text = str(Engine.get_frames_per_second())
