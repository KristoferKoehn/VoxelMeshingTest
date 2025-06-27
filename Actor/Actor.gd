extends CharacterBody3D
class_name Actor

var State : Dictionary

func _ready():
	if State.has("components"):
		for c in State["components"]:
			add_child(c.new())
	pass # Replace with function body.

func _process(_delta):
	pass

func _physics_process(_delta):
	pass
