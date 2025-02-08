extends CharacterBody3D


@export var Model : Node3D

@export_group("Animation")
@export var AnimTree : AnimationTree

@export_group("Camera")
@export var Camera : Camera3D
@export var CameraPivot : Node3D
@export var SpringArm : SpringArm3D
@export_range(0.0, 1.0) var mouse_sensitivity := 0.25

@export_group("Movement")
@export var move_speed := 8
@export var acceleration := 20.0
@export var rotation_speed := 12.0
@export var jump_impulse := 12.0
@export var ForwardStepCollision : CollisionShape3D
@export var LeftStepCollision : CollisionShape3D
@export var LeftStepCollision2 : CollisionShape3D
@export var RightStepCollision : CollisionShape3D
@export var RightStepCollision2 : CollisionShape3D
@export var max_step_down := -1
@export var step_up_radius := 0.5


var springarm_length = 7

var _camera_input_direction := Vector2.ZERO
var _last_movement_direction := Vector3.BACK
var _gravity := -30.0

var aiming := false

func _enter_tree():
	_gravity = 0.0
	var function = func(): 
		_gravity = -30.0
	get_tree().create_timer(5).timeout.connect(function)

func _input(event):
	if event.is_action_pressed("click"):
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	if event.is_action_pressed("ui_cancel"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	if event.is_action_pressed("aim"):
		var Springarm_tween : Tween = create_tween()
		Springarm_tween.tween_property(SpringArm, "spring_length", 4, 0.2)
		var pivot_tween : Tween = create_tween()
		pivot_tween.tween_property(SpringArm, "position", Vector3(-2, 0, 0), 0.2)
		var camera_tween : Tween = create_tween()
		camera_tween.tween_property(Camera, "fov", 60, 0.2)
		aiming = true
	if event.is_action_released("aim"):
		var Springarm_tween : Tween = create_tween()
		Springarm_tween.tween_property(SpringArm, "spring_length", springarm_length, 0.2)
		var pivot_tween : Tween = create_tween()
		pivot_tween.tween_property(SpringArm, "position", Vector3(0, 0, 0), 0.2)
		var camera_tween : Tween = create_tween()
		camera_tween.tween_property(Camera, "fov", 75, 0.2)
		aiming = false

	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_WHEEL_UP: # Increases max velocity
				springarm_length = clamp(springarm_length * 1.1, 6, 12)
				if not aiming:
					SpringArm.spring_length = springarm_length
			MOUSE_BUTTON_WHEEL_DOWN: # Decereases max velocity
				springarm_length = clamp(springarm_length / 1.1, 6, 12)
				if not aiming:
					SpringArm.spring_length = springarm_length
	var is_camera_motion := (
		event is InputEventMouseMotion and 
		Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED
	)
	if is_camera_motion: 
		_camera_input_direction = event.screen_relative * mouse_sensitivity

var _was_on_floor_last_frame = false
var _snapped_to_stairs_last_frame = false
func _snap_down_to_stairs_check():
	var did_snap = false
	if not is_on_floor() and velocity.y <= 0 and (_was_on_floor_last_frame or _snapped_to_stairs_last_frame):
		var body_test_result = PhysicsTestMotionResult3D.new()
		var params = PhysicsTestMotionParameters3D.new()
		params.from = self.global_transform
		params.motion = Vector3(0, max_step_down, 0)
		if PhysicsServer3D.body_test_motion(self.get_rid(), params, body_test_result):
			#self.position.y += body_test_result.get_travel().y
			velocity.y += -5
			did_snap = true

	_was_on_floor_last_frame = is_on_floor_only()
	_snapped_to_stairs_last_frame = did_snap

func _process(delta):
	CameraPivot.rotation.x += _camera_input_direction.y * delta
	CameraPivot.rotation.x = clamp(CameraPivot.rotation.x, -PI/6.0, PI/3.0)
	CameraPivot.rotation.y -= _camera_input_direction.x * delta
	_camera_input_direction = Vector2.ZERO

func _physics_process(delta: float) -> void:

	var state_machine : AnimationNodeStateMachinePlayback = AnimTree["parameters/AnimationNodeStateMachine/playback"]
	if is_on_floor():
		if aiming: 
			if state_machine.get_current_node() != "groundlocomotion":
				state_machine.travel("groundlocomotion")
		else:
			if velocity.length() < 0.1:
				if state_machine.get_current_node() != "idle":
					state_machine.travel("idle")
			else:
				if state_machine.get_current_node() != "running":
					state_machine.travel("running")
	else:
		if state_machine.get_current_node() != "jumping":
			state_machine.travel("jumping")

	var raw_input := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var forward := Camera.global_basis.z
	var right := Camera.global_basis.x
	
	var move_direction := forward * raw_input.y + right * raw_input.x
	move_direction.y = 0
	move_direction = move_direction.normalized()
	
	var y_velocity := velocity.y
	velocity.y = 0.0
	velocity = velocity.move_toward(move_direction * move_speed, acceleration * delta)
	velocity.y = y_velocity + _gravity * delta
	
	var is_starting_jump := Input.is_action_just_pressed("jump") and is_on_floor()
	if is_starting_jump:
		velocity.y += jump_impulse
	
	move_and_slide()
	_snap_down_to_stairs_check()
	var curr_blend = AnimTree["parameters/AnimationNodeStateMachine/groundlocomotion/blend_position"]
	AnimTree["parameters/AnimationNodeStateMachine/groundlocomotion/blend_position"] = lerp(curr_blend, raw_input, 0.5)
	if move_direction.length() > 0.2:
		_last_movement_direction = move_direction
	if not aiming:
		var target_angle := Vector3.BACK.signed_angle_to(_last_movement_direction, Vector3.UP)
		Model.global_rotation.y = lerp_angle(Model.rotation.y, target_angle, rotation_speed * delta)
	else:
		var target_angle := Vector3.BACK.signed_angle_to(-forward, Vector3.UP)
		Model.global_rotation.y = lerp_angle(Model.rotation.y, target_angle, rotation_speed * delta)
		
	if Vector3(velocity.x, 0, velocity.z).length() > 0.4:
		ForwardStepCollision.position = Vector3(0,1.1,0) + Vector3(velocity.x, 0, velocity.z).normalized() * step_up_radius
		LeftStepCollision.position = Vector3(0,1.1,0) + Vector3(velocity.x, 0, velocity.z).normalized().rotated(Vector3.UP, PI/4) * step_up_radius
		LeftStepCollision2.position = Vector3(0,1.1,0) + Vector3(velocity.x, 0, velocity.z).normalized().rotated(Vector3.UP, PI/8) * step_up_radius
		RightStepCollision.position = Vector3(0,1.1,0) + Vector3(velocity.x, 0, velocity.z).normalized().rotated(Vector3.UP, -PI/4) * step_up_radius
		RightStepCollision2.position = Vector3(0,1.1,0) + Vector3(velocity.x, 0, velocity.z).normalized().rotated(Vector3.UP, -PI/8) * step_up_radius
	else:
		ForwardStepCollision.position = Vector3(0,1.1,0) + Vector3(move_direction.x, 0, move_direction.z).normalized() * step_up_radius
		LeftStepCollision.position = Vector3(0,1.1,0) + Vector3(move_direction.x, 0, move_direction.z).normalized().rotated(Vector3.UP, PI/4) * step_up_radius
		LeftStepCollision2.position = Vector3(0,1.1,0) + Vector3(move_direction.x, 0, move_direction.z).normalized().rotated(Vector3.UP, PI/8) * step_up_radius
		RightStepCollision.position = Vector3(0,1.1,0) + Vector3(move_direction.x, 0, move_direction.z).normalized().rotated(Vector3.UP, -PI/4) * step_up_radius
		RightStepCollision2.position = Vector3(0,1.1,0) + Vector3(move_direction.x, 0, move_direction.z).normalized().rotated(Vector3.UP, -PI/8) * step_up_radius
