extends Node

##dictionary<Node, bool> for checking if an attack is happening
##@experimental:
var ATTACK_NODES = "attack_nodes"

##Array of attack nodes for indexing
var ATTACK_LIST = "attack_list"

##access a Node3D, should be a Camera3D
##@experimental:
var CAMERA = "camera"

##boolean, to block player input node from assigning values to state dictionary
var INPUT_BLOCK = "input_block"

##string, name of arms animation state to transition to
var ARMS_ANIM = "arms_anim"

##animationtree, reference to the animation tree node of the model
var ANIM_TREE = "anim_tree"

##Skeleton3D node of the model
var SKELETON = "skeleton"