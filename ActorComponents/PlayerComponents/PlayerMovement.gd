extends Node3D

var _actor : Actor
var _aiming
var _last_movement_direction = Vector3.ZERO
var _shapecast : ShapeCast3D
var _tracker_visual : MeshInstance3D
# Called when the node enters the scene tree for the first time.
func _ready():
	_actor = get_parent() as Actor
	_shapecast = ShapeCast3D.new()
	_shapecast.top_level = true
	var _box : CylinderShape3D = CylinderShape3D.new()
	_box.radius = 0.4
	_box.height = 1
	_shapecast.shape = _box
	_shapecast.add_exception_rid(_actor.get_rid())
	add_child(_shapecast)
	for i in range(8):
		var c = CollisionShape3D.new()
		var s := SeparationRayShape3D.new()
		s.length = 1.5
		c.shape = s
		_actor.add_child(c)
		c.rotate_x(PI/2 - 0.01)
		c.position = Vector3(0,1.5,0.5).rotated(Vector3(0,1,0), PI/2 - PI/8.0 * i)

	var mes : MeshInstance3D = MeshInstance3D.new()
	var pil : SphereMesh = SphereMesh.new()
	mes.mesh = pil
	mes.top_level = true
	_actor.add_child(mes)
	_tracker_visual = mes

func _input(event):
	if event.is_action_pressed("aim"):
		_aiming = true
	if event.is_action_released("aim"):
		_aiming = false

func _physics_process(delta: float) -> void:
	var raw_input := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	#print(raw_input)
	var forward : Vector3 = _actor.State["camera"].global_basis.z
	var right :  Vector3 = _actor.State["camera"].global_basis.x
	
	var move_direction := forward * raw_input.y + right * raw_input.x
	move_direction.y = 0
	move_direction = move_direction.normalized()
	_actor.State["input_direction"] = move_direction
	
	var y_velocity :=  _actor.velocity.y
	_actor.velocity.y = 0.0
	_actor.velocity = _actor.velocity.move_toward(move_direction * _actor.State["move_speed"], _actor.State["acceleration"] * delta)
	if not _actor.is_on_floor():
		_actor.velocity.y = y_velocity + _actor.State["gravity"] * delta
	
	var is_starting_jump := Input.is_action_just_pressed("jump") and _actor.is_on_floor()
	if is_starting_jump:
		_actor.velocity.y += _actor.State["jump_impulse"]

	if move_direction.length() > 0.2:
		_last_movement_direction = move_direction
	if not _aiming:
		var target_angle := Vector3.BACK.signed_angle_to(_last_movement_direction, Vector3.UP)
		#_actor.global_rotation.y = lerp_angle(_actor.rotation.y, target_angle, 5 * delta)
		_actor.global_rotation.y = target_angle
	else:
		var target_angle := Vector3.BACK.signed_angle_to(-forward, Vector3.UP)
		_actor.global_rotation.y = target_angle

	_snap_up_down_check2()

	var height_component = lerp(_tracker_visual.global_position, _actor.global_position * Vector3(0, 1, 0) + Vector3(0, 1, 0), 0.2)
	_tracker_visual.global_position = lerp(_tracker_visual.global_position, _actor.global_position + Vector3(0, 1, 0), 0.3)
	_tracker_visual.global_position.y = height_component.y
	if _actor.is_on_wall():
		print("ON WALL")
	_actor.move_and_slide()


var prev_dist = 0
func _snap_up_down_check2():
	_shapecast.global_position = Vector3(0, 2, 0) + _actor.global_position
	_shapecast.target_position = Vector3(0, -3, 0)
	_shapecast.force_update_transform()
	var cur = 0
	if _shapecast.get_collision_count() > 0:
		cur = _actor.global_position.y - _shapecast.get_collision_point(0).y
		print("we get here " + str(prev_dist) + " " + str(cur))
		if(prev_dist < 0.2 and cur > 0.9):
			print("all the way in")
			cur = _actor.global_position.y - _shapecast.get_collision_point(0).y
			_actor.global_position.y -= cur;
	else:
		cur = 1
	prev_dist = cur;
