using Godot;
using System;
using VoxelMeshingTest.Classes;

public partial class DebugPanel : Panel2
{

    [Export] SpinBox Spawn;
    [Export] SpinBox DespawnMargin;
    [Export] SpinBox Threads;
    [Export] SpinBox Depth;
    [Export] SpinBox Height;
    [Export] SpinBox Align;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		base._Ready();

        Spawn.SetValueNoSignal(GameConstants.SPAWN_RADIUS);
        Threads.SetValueNoSignal(GameConstants.CHUNK_THREADS);
        Depth.SetValueNoSignal(GameConstants.WORLD_DEPTH);
        Height.SetValueNoSignal(GameConstants.WORLD_HEIGHT);
        Align.SetValueNoSignal(GameConstants.ALIGNMENT_SCORE_WEIGHT);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			Visible = false;
		}
		else
		{
            Visible = true;

        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
    }

	public void SpawnRadiusChange(float radius)
	{
		GameConstants.SPAWN_RADIUS = (int)radius;
        GameConstants.DESPAWN_RADIUS = GameConstants.SPAWN_RADIUS + GameConstants.DESPAWN_MARGIN;
	}

    public void DespawnMarginChange(float radius)
    {
        GameConstants.DESPAWN_MARGIN = (int)radius;
        GameConstants.DESPAWN_RADIUS = GameConstants.SPAWN_RADIUS + GameConstants.DESPAWN_MARGIN;
    }

    public void ChunkThreadsChange(float threads)
    {
        GameConstants.CHUNK_THREADS = (int)threads;
    }

    public void WorldDepthChange(float depth)
    {
        GameConstants.WORLD_DEPTH = (int)depth;
    }

    public void WorldHeightChange(float height)
    {
        GameConstants.WORLD_HEIGHT = (int)height;
    }

    public void AlignmentChange(float height)
    {
        GameConstants.ALIGNMENT_SCORE_WEIGHT = height;
    }
}
