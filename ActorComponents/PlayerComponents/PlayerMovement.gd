extends Node3D

var _actor : Actor
var _aiming

# Called when the node enters the scene tree for the first time.
func _ready():
	_actor = get_parent() as Actor
	pass # Replace with function body.

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
	_actor.velocity.y = y_velocity +  _actor.State["gravity"] * delta
	
	var is_starting_jump := Input.is_action_just_pressed("jump") and _actor.is_on_floor()
	if is_starting_jump:
		_actor.velocity.y += _actor.State["jump_impulse"]
	_snap_down_to_stairs_check()
	_snap_up_to_stairs_check()
	_actor.move_and_slide()
	
var _was_on_floor_last_frame = false
var _snapped_to_stairs_last_frame = false
func _snap_down_to_stairs_check():
	var did_snap = false
	if _actor.is_on_floor() and _actor.velocity.y <= 0 and (_was_on_floor_last_frame or _snapped_to_stairs_last_frame):
		var body_test_result = PhysicsTestMotionResult3D.new()
		var params = PhysicsTestMotionParameters3D.new()
		params.from = self.global_transform
		params.motion = Vector3(0, -1, 0)
		if PhysicsServer3D.body_test_motion(_actor.get_rid(), params, body_test_result):
			#self.position.y += body_test_result.get_travel().y
			_actor.velocity.y += -5
			did_snap = true

	_was_on_floor_last_frame = _actor.is_on_floor_only()
	_snapped_to_stairs_last_frame = did_snap

var _was_on_wall_last_frame = false
var _snapped_to_up_step_last_frame = false
func _snap_up_to_stairs_check():

	var did_snap = false
	if _actor.is_on_wall() and _actor.velocity.y <= 0 and !_was_on_wall_last_frame:
		var body_test_result = PhysicsTestMotionResult3D.new()
		var params = PhysicsTestMotionParameters3D.new()
		var transf : Transform3D = _actor.global_transform
		params.from = transf.translated(Vector3(_actor.velocity.x * 3, 2.1, _actor.velocity.z * 3))
		params.motion =  Vector3(0, -1.5, 0) # moving down
		if PhysicsServer3D.body_test_motion(_actor.get_rid(), params, body_test_result):
			if (body_test_result.get_travel().y < -1.0):
				_actor.global_position.y -= body_test_result.get_travel().y
				did_snap = true
			else:
				print("stepup failed" + str(body_test_result.get_travel().y))

	_was_on_wall_last_frame = _actor.is_on_wall()
	_snapped_to_up_step_last_frame = did_snap
