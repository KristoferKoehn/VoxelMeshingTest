@tool
extends Node3D


@export var explode : bool = false
@export var interior_shatter : GPUParticles3D
@export var exterior_shatter : GPUParticles3D

var speed = 0.5
var angular_speed = PI



func _ready():
	pass
	
func _process(delta: float) -> void:
	rotate_y(speed * 1.8024 * delta)
	rotate_x(speed * 1.5766 * delta)


	if !Engine.is_editor_hint():
		#test();
		pass



func _on_area_3d_body_entered(_body:Node3D):
	print("what")
	interior_shatter.emitting = true
	exterior_shatter.emitting = true
	$Interior.visible = false
	$Exterior.visible = false
	$OmniLight3D.visible = false
	$OmniLight3D2.visible = false

func test():
	if explode:
		interior_shatter.emitting = true
		exterior_shatter.emitting = true
		$Interior.visible = false
		$Exterior.visible = false
		$OmniLight3D.visible = false
		$OmniLight3D2.visible = false
	else:
		interior_shatter.emitting = false
		exterior_shatter.emitting = false
		$Interior.visible = true
		$Exterior.visible = true
