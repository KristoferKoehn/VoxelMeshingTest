extends Node3D

var _actor : Actor

@export_range(0.0, 1.0) var sensitivity: float = 0.25

# Mouse state
var _mouse_position = Vector2(0.0, 0.0)
var _total_pitch = 0.0

var _camera : Camera3D
var _gimbal : Node3D

func _enter_tree():
	top_level = true

# Called when the node enters the scene tree for the first time.
func _ready():
	_actor = get_parent() as Actor
	_gimbal = load("res://ActorUtilityScenes/CameraGimbal.tscn").instantiate()
	_camera = _gimbal.get_node("SpringArm3D/Camera3D")
	_actor.State["camera"] = _camera
	add_child(_gimbal)
	pass # Replace with function body.

func _process(_delta):
	_update_mouselook()

func _physics_process(_delta):
	global_position = lerp(global_position, _actor.global_position, 0.2)

func _input(event):

	if event is InputEventMouseMotion:
		_mouse_position = event.relative
	if event is InputEventKey and Input.is_key_pressed(KEY_P):
		var vp = get_viewport()
		vp.debug_draw = (vp.debug_draw + 1 ) % 6

	if event.is_action_pressed("view_toggle"):
		if Input.mouse_mode == Input.MOUSE_MODE_VISIBLE:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
		else:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

# Updates mouse look 
func _update_mouselook():
	# Only rotates mouse if the mouse is captured
	if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		_mouse_position *= sensitivity
		var yaw = _mouse_position.x
		var pitch = _mouse_position.y
		_mouse_position = Vector2(0, 0)
		
		# Prevents looking up/down too far
		pitch = clamp(pitch, -90 - _total_pitch, 90 - _total_pitch)
		_total_pitch += pitch
	
		_gimbal.rotate_y(deg_to_rad(-yaw))
		_camera.rotate_object_local(Vector3(1,0,0), deg_to_rad(-pitch))
