extends Node

var _actor : Actor

var _last_movement_direction = Vector3.ZERO
var _sprinting = false

func _enter_tree():
	_actor = get_parent()
	_actor.State["input_direction"] = Vector3()
	_actor.State["raw_input"] = Vector2()
	_actor.State["target_angle"] = 0
	_actor.State["aiming"] = false
	_actor.State["sprinting"] = false
	_actor.State[Dict.INPUT_BLOCK] = false

func _process(_delta):
	pass

func _input(_event):


	if Input.is_action_pressed("aim") and Input.mouse_mode != Input.MOUSE_MODE_VISIBLE:
		_actor.State["aiming"] = true
	else:
		_actor.State["aiming"] = false

	if Input.is_action_just_released("sprint"):
		_actor.State["sprinting"] = true
		_sprinting = true

	if Input.is_action_just_pressed(("sprint")):
		_actor.State["sprinting"] = false
		_sprinting = false

	if Input.is_action_just_pressed("attack1"):
		var _attack_node = _actor.State[Dict.ATTACK_LIST][0]
		_actor.State[Dict.ATTACK_NODES][_attack_node] = true

func _physics_process(_delta):

	if _actor.State[Dict.INPUT_BLOCK]:
		return

	var raw_input := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var forward : Vector3 = _actor.State["camera"].global_basis.z
	var right :  Vector3 = _actor.State["camera"].global_basis.x
	var move_direction := forward * raw_input.y + right * raw_input.x
	move_direction.y = 0
	move_direction = move_direction.normalized()
	
	if _sprinting:
		_actor.State["raw_input"] = raw_input
		_actor.State["input_direction"] = move_direction
	else:
		_actor.State["raw_input"] = raw_input * 0.6
		_actor.State["input_direction"] = move_direction * 0.6

	if move_direction.length() > 0.2:
		_last_movement_direction = move_direction
	if _actor.State["aiming"]:
		var target_angle := Vector3.BACK.signed_angle_to(-forward, Vector3.UP)
		_actor.State["target_angle"] = target_angle
	else:
		var target_angle := Vector3.BACK.signed_angle_to(_last_movement_direction, Vector3.UP)
		_actor.State["target_angle"] = target_angle
	
