using Godot;
using System.Collections.Generic;

public partial class GameLoop : Node3D
{
    [Export]
    public FastNoiseLite Terrain { get; set; }
    [Export]
    public FastNoiseLite SurfaceCutoff { get; set; }

    [Export]
    public Vector2 CutoffOffset { get; set; }

    public List<Chunk> Chunks { get; set; } = new List<Chunk>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        RNGManager.Instance();
        ChunkGeneratorManager.Instance();

        ChunkGeneratorManager.Terrain = Terrain;
        ChunkGeneratorManager.SurfaceCutoff = SurfaceCutoff;
        ChunkGeneratorManager.CutoffOffset = CutoffOffset;
        ChunkMeshManager.Instance();
        //ChunkGeneratorManager.Instance().PreGenerate();
        ChunkSpawnManager.Instance();

        //ChunkSpawnManager.Instance().GenerateWorld();

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("regenerate"))
        {
            //ChunkSpawnManager.Instance().GenerateWorld();
        }
    }


}
