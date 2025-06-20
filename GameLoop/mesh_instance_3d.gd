extends MeshInstance3D


# Called when the node enters the scene tree for the first time.
func _ready():
	mesh = $"../GDExample".generate_and_mesh(Vector3())
	
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass
