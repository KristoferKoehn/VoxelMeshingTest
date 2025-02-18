using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelMeshingTest.Managers
{
    internal partial class PlayerManager : Node
    {
        public PlayerManager() { }

        private static PlayerManager instance;

        public static PlayerManager Instance()
        {
            if (instance == null)
            {
                instance = new PlayerManager();
                SceneSwitcher.Instance().AddChild(instance);
                instance.Name = "PlayerManager";
            }
            return instance;
        }

        void SpawnBasicPlayer()
        {
            Godot.Collections.Array components = new Godot.Collections.Array()
            {
                ResourceLoader.Load("res://ActorComponents/PlayerComponents/PlayerCamera.gd"),
                ResourceLoader.Load("res://ActorComponents/PlayerComponents/PlayerInput.gd"),
                ResourceLoader.Load("res://ActorComponents/Movement.gd"),
                ResourceLoader.Load("res://ActorComponents/Model.gd"),
                ResourceLoader.Load("res://ActorComponents/Melee.gd"),
            };



            Dictionary dict = new()
            {
                { "gravity", -25},
                { "components", components},
                { "move_speed", 8},
                { "acceleration", 50},
                { "jump_impulse", 12},
                { "input_direction", new Vector3()},
                { "model", "res://assets/character/Skeleton_Rogue.glb"},
                { "attack_nodes", new Dictionary() },
                { "attack_list", new Godot.Collections.Array() },
            };
            var actor = ResourceLoader.Load<PackedScene>("res://Controllers/PlayerActor.tscn").Instantiate();
            actor.Set("State", dict);
            SceneSwitcher.Instance().SceneStack.Peek().AddChild(actor);
            actor.Set("global_position", new Vector3(0, 0, 0));
        }

        public override void _Ready()
        {
            SpawnBasicPlayer();
        }

    }
}
