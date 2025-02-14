extends Node


var _actor : Actor

# Called when the node enters the scene tree for the first time.
func _ready():
	_actor = get_parent() as Actor
	_actor.moving.connect(motivate)
	pass # Replace with function body.

func motivate():
	var p = get_parent() as Actor
	p.velocity = p.State["movement"]

