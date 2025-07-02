using Godot;
using System;
using VoxelMeshingTest.Classes;

public partial class C_Chunk : MeshInstance3D
{

    Timer t = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Mesh.SurfaceSetMaterial(0, GD.Load<ShaderMaterial>("res://Resources/ChunkMaterial/ChunkMaterial.tres"));
		Tween tween = CreateTween();
		tween.TweenProperty(this, "global_position", GlobalPosition + new Vector3(0, 32, 0), 0.5).SetTrans(Tween.TransitionType.Circ);
        AddChild(t);
        t.Start(1.3);
        t.Timeout += DisposalCheck;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	void DisposalCheck() {

        Vector3I chunkCoord = new Vector3I(
        Mathf.FloorToInt(GlobalPosition.X / 64.0f),
        Mathf.FloorToInt(GlobalPosition.Y / 64.0f),
        Mathf.FloorToInt(GlobalPosition.Z / 64.0f)
        );

        Vector3I CameraChunkCoord = new Vector3I(
            Mathf.FloorToInt(GetTree().Root.GetViewport().GetCamera3D().GlobalPosition.X / 64.0f),
            Mathf.FloorToInt(0),
            Mathf.FloorToInt(GetTree().Root.GetViewport().GetCamera3D().GlobalPosition.Z / 64.0f)
        );

        if ((CameraChunkCoord - chunkCoord).Length() > GameConstants.DESPAWN_RADIUS)
        {


            Tween tween = CreateTween();
            tween.TweenProperty(this, "global_position", GlobalPosition - new Vector3(0, 32, 0), 0.5).SetTrans(Tween.TransitionType.Linear);
            tween.Finished += () =>
            {
                ChunkManager.Instance().DeregisterChunk(chunkCoord);
                this.QueueFree();
            };


        }
    }
}
