extends Node3D

var _actor : Actor
var _last_movement_direction = Vector3.ZERO
var _shapecast : ShapeCast3D


func _ready():
	_actor = get_parent() as Actor
	_shapecast = ShapeCast3D.new()
	_shapecast.top_level = true
	var _box : CylinderShape3D = CylinderShape3D.new()
	_box.radius = 0.3
	_box.height = 1
	_shapecast.shape = _box
	_shapecast.add_exception_rid(_actor.get_rid())

	var f = CollisionShape3D.new()
	var w : SphereShape3D = SphereShape3D.new()
	f.shape = w
	w.radius = 0.5
	_actor.add_child(f)

	f.position = Vector3(0, 1.4, 0)

	
	add_child(_shapecast)
	for i in range(6):
		var c = CollisionShape3D.new()
		var s := SeparationRayShape3D.new()
		s.length = 1
		c.shape = s
		_actor.add_child(c)
		c.rotate_x(PI/2 - 0.02)
		c.position = Vector3(0, 1, 0.4).rotated(Vector3(0, 1, 0), PI/2 - PI/3.0 * (i))


func _physics_process(delta: float) -> void:
	var y_velocity :=  _actor.velocity.y
	_actor.velocity.y = 0.0
	_actor.velocity = _actor.velocity.move_toward(_actor.State["input_direction"] * _actor.State["move_speed"], _actor.State["acceleration"] * delta)
	if not _actor.is_on_floor():
		_actor.velocity.y = y_velocity + _actor.State["gravity"] * delta
	
	var is_starting_jump := Input.is_action_just_pressed("jump") and _actor.is_on_floor()
	if is_starting_jump:
		_actor.velocity.y += _actor.State["jump_impulse"]

	if _actor.State["input_direction"].length() > 0.2:
		_last_movement_direction = _actor.State["input_direction"]
	if not _actor.State["aiming"]:
		var target_angle := Vector3.BACK.signed_angle_to(_last_movement_direction, Vector3.UP)
		_actor.global_rotation.y = target_angle
	_snap_up_down_check()
	#var height_component = lerp(_tracker_visual.global_position, _actor.global_position * Vector3(0, 1, 0) + Vector3(0, 1, 0), 0.2)
	#_tracker_visual.global_position = lerp(_tracker_visual.global_position, _actor.global_position + Vector3(0, 1, 0), 0.3)
	#_tracker_visual.global_position.y = height_component.y
	_actor.move_and_slide()

var prev_dist = 0
func _snap_up_down_check():
	_shapecast.global_position = Vector3(0, 2, 0) + _actor.global_position
	_shapecast.target_position = Vector3(0, -3, 0)
	_shapecast.force_update_transform()
	var cur = 0
	if _shapecast.get_collision_count() > 0:
		cur = _actor.global_position.y - _shapecast.get_collision_point(0).y
		if(prev_dist < 0.2 and cur > 0.9):
			cur = _actor.global_position.y - _shapecast.get_collision_point(0).y
			_actor.global_position.y -= cur;
	else:
		cur = 1
	prev_dist = cur;
