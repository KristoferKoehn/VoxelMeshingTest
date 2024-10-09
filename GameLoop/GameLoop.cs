using Godot;
using System;

public partial class GameLoop : Node3D
{



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RNGManager.Instance();
		ChunkGeneratorManager.Instance();
		ChunkMeshManager.Instance();



		for(int i  = 0; i < 1; i++)
		{
			for (int j = 0; j < 1; j++)
			{
                uint[] data = ChunkGeneratorManager.Instance().GenerateChunk(i, 0, j);
                Face[] faces = ChunkMeshManager.GenerateMesh(data);

                Chunk ch = new Chunk();
                ch.ProcessFaces(faces);
                AddChild(ch);
				ch.MeshInstance.GlobalPosition = new Vector3(i * 128, 0, j * 128);
            }
		}


    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
