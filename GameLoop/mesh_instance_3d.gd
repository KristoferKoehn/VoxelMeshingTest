extends MeshInstance3D

var c_pos
var move_scale = 1;
# Called when the node enters the scene tree for the first time.
func _ready():
	c_pos = global_position
	#mesh = $"../GDExample".generate_and_mesh(c_pos)
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	var mov = Vector3.ZERO
	if (Input.is_key_label_pressed(KEY_LEFT)):
		mov += Vector3(0, 0, 1) * move_scale
	if (Input.is_key_label_pressed(KEY_DOWN)):
		mov += Vector3(-1, 0, 0) * move_scale
	if (Input.is_key_label_pressed(KEY_UP)):
		mov += Vector3(1, 0, 0) * move_scale
	if (Input.is_key_label_pressed(KEY_RIGHT)):
		mov += Vector3(0, 0, -1) * move_scale
	
	if (Input.is_key_label_pressed(KEY_SHIFT)):
		mov += Vector3(0, 1, 0) * move_scale
	if (Input.is_key_label_pressed(KEY_CTRL)):
		mov += Vector3(0, -1, 0) * move_scale
		
	if mov != Vector3.ZERO:
		c_pos += mov
		#mesh = $"../GDExample".generate_and_mesh(c_pos)
