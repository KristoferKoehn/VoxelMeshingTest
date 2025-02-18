extends Node3D

var _actor : Actor
var _right_hand : BoneAttachment3D = null
var _hitbox : Area3D
var _hit_shape : CollisionShape3D

var _animation_name = "1H_Melee_Attack_Chop"

var _actively_executing = false

func _enter_tree():
	_actor = get_parent()
	#add this node to attack 
	_actor.State[Dict.ATTACK_NODES][self] = false
	_actor.State[Dict.ATTACK_LIST].append(self)
	_hitbox = Area3D.new()
	var t : BoxShape3D = BoxShape3D.new()
	_hit_shape = CollisionShape3D.new()
	_hit_shape.shape = t
	t.size = Vector3(0.5, 0.5, 0.5)
	_hitbox.collision_mask = 2
	_hitbox.collision_layer = 0
	_hitbox.add_child(_hit_shape)
	

func _ready():
	pass

func _physics_process(_delta):
	if _right_hand == null and _actor.State["skeleton"].get_node("right_hand") != null:
		_right_hand = _actor.State["skeleton"].get_node("right_hand")
		_right_hand.add_child(_hitbox)
	
	if _right_hand == null:
		return

	#check if we're attacking
	if _actor.State[Dict.ATTACK_NODES][self] and not _actively_executing:
		_actively_executing = true
		_actor.State[Dict.INPUT_BLOCK] = true
		_actor.State[Dict.ANIM_TREE]["parameters/Arms/playback"].travel(_animation_name)
		_actor.State[Dict.ANIM_TREE].animation_finished.connect(completed_action)
		_actor.State[Dict.ANIM_TREE]["parameters/TimeScale/scale"] = 1.3
		if _actor.is_on_floor():
			create_tween().tween_property(_actor, "State:input_direction", Vector3(0,0,0), 0.3)
		

func completed_action(animation : StringName):
	if animation.contains(_animation_name):
		_actively_executing = false
		_actor.State[Dict.ATTACK_NODES][self] = false
		_actor.State[Dict.INPUT_BLOCK] = false
		_actor.State[Dict.ANIM_TREE].animation_finished.disconnect(completed_action)
		_actor.State[Dict.ANIM_TREE]["parameters/Arms/locomotion/blend_position"] = Vector2(0,0)
		_actor.State[Dict.ANIM_TREE]["parameters/TimeScale/scale"] = 1
		_actor.State["input_direction"] = Vector3.ZERO
		_actor.State["raw_input"] = Vector3.ZERO
		
