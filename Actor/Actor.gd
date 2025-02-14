extends Node3D

var State : Dictionary

signal moving
signal attacking
signal jumping
signal casting
signal eating
signal health_changed(delta:float)
signal mana_changed(delta:float)

# Called when the node enters the scene tree for the first time.
func _ready():
	if State.has("components"):
		for c in State["components"]:
			add_child(c)
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
