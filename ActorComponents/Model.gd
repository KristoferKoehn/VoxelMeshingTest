extends Node3D

var MODEL_PATH = "model"
var _actor : Actor
var _model
var _anim : AnimationPlayer
var _anim_tree : AnimationTree
var _anim_coyote_time = 0

func _enter_tree():
	_actor = get_parent()
	_actor.State[Dict.ARMS_ANIM] = ""
	_actor.State[Dict.ANIM_TREE] = null

func _ready():
	_model = ResourceLoader.load(_actor.State[MODEL_PATH]).instantiate()
	_model.top_level = true
	_anim_tree = AnimationTree.new()
	_actor.State[Dict.ANIM_TREE] = _anim_tree
	_anim_tree.tree_root = ResourceLoader.load("res://Resources/animations/Kaykit_animtree.tres")
	_actor.add_child(_model)
	_anim = _model.get_node("AnimationPlayer")
	_actor.add_child(_anim_tree)
	_anim.remove_animation_library("")
	_anim.add_animation_library("Kaykit_anim_library", ResourceLoader.load("res://Resources/animations/AnimationLibrary/Kaykit_anim_library.tres"))
	_anim_tree.anim_player = _anim.get_path()
	_anim_tree.set("parameters/LocomotionBlend/blend_amount", 1)
	_actor.State["skeleton"] = _model.get_node("Rig/Skeleton3D")
	var br : BoneAttachment3D = BoneAttachment3D.new()
	var bl : BoneAttachment3D = BoneAttachment3D.new()
	_actor.State["skeleton"].add_child(br)
	_actor.State["skeleton"].add_child(bl)
	br.bone_name = "handslot.r"
	bl.bone_name = "handslot.l"
	br.name = "right_hand"
	bl.name = "left_hand"

	var attachment_dictionary : Dictionary
	attachment_dictionary[br.name] = br
	attachment_dictionary[bl.name] = bl
	_actor.State["attachment_slots"] = attachment_dictionary

func _process(_delta):
	pass

func _physics_process(_delta):

	var y = lerp(_model.global_position.y, _actor.global_position.y, 0.5)
	_model.global_position = lerp(_model.global_position, _actor.global_position, 0.4)
	_model.global_position.y = y

	if _actor.State["aiming"]:
		var g = _anim_tree.get("parameters/Legs/locomotion/blend_position")
		_anim_tree.set("parameters/Legs/locomotion/blend_position", lerp( g,  _actor.State["raw_input"], 0.1))
		_anim_tree.set("parameters/Arms/locomotion/blend_position", lerp( g,  _actor.State["raw_input"], 0.1))
		_model.global_rotation.y = lerp_angle(_model.global_rotation.y, _actor.State["target_angle"], 0.1)
	else:
		var g = _anim_tree.get("parameters/Legs/locomotion/blend_position")
		_anim_tree.set("parameters/Legs/locomotion/blend_position", Vector2(0, -lerp(g.length(),  _actor.State["raw_input"].length(), 0.3)))
		_anim_tree.set("parameters/Arms/locomotion/blend_position", Vector2(0, -lerp(g.length(),  _actor.State["raw_input"].length(), 0.3)))
		_model.global_rotation.y = lerp_angle(_model.global_rotation.y, _actor.State["target_angle"], 0.1)

	if _anim_coyote_time > 3 and _anim_tree["parameters/Legs/playback"].get_current_node() != "Jump_Idle":
		_anim_tree["parameters/Legs/playback"].travel("Jump_Idle")
		_anim_tree["parameters/Arms/playback"].travel("Jump_Idle")
	if _actor.is_on_floor() and _anim_tree["parameters/Legs/playback"].get_current_node() != "locomotion":
		_anim_tree["parameters/Legs/playback"].travel("locomotion")
		_anim_tree["parameters/Arms/playback"].travel("locomotion")
	
	if not _actor.is_on_floor():
		_anim_coyote_time = _anim_coyote_time + 1
	else:
		_anim_coyote_time = 0
