using Godot;
using System;

public partial class MetalBox : RigidBody3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Up)) {
			this.LinearVelocity = Vector3.Zero;
			GlobalPosition = GlobalPosition + new Vector3(0,10,0);
		}
	}

	public void Collision(Node body)
	{
		GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D").Play();
	}
}
