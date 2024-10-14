using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        ChunkSpawnManager.Instance();

        ChunkGeneratorManager.Terrain = Terrain;
        ChunkGeneratorManager.SurfaceCutoff = SurfaceCutoff;
        ChunkGeneratorManager.CutoffOffset = CutoffOffset;
        ChunkMeshManager.Instance();

        /*
		for(int i  = 0; i < 1; i++)
		{
			for (int j = 0; j < 1; j++)
			{
                uint[] data = ChunkGeneratorManager.Instance().GenerateChunk(i, 0, j);
                Face[] faces = ChunkMeshManager.GenerateMeshSlow(data);

                Chunk ch = new Chunk();
                ch.ProcessFaces(faces);
                AddChild(ch);
				ch.MeshInstance.GlobalPosition = new Vector3(i * 128, 0, j * 128);
            }
		}
		*/

        ChunkSpawnManager.Instance().GenerateWorld();

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("regenerate"))
        {
            ChunkSpawnManager.Instance().GenerateWorld();
        }
    }


}
