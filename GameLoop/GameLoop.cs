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

		uint[] data = ChunkGeneratorManager.Instance().GenerateChunk(0, 0, 0);
		Face[] faces = ChunkMeshManager.GenerateMesh(data);
		
		Chunk ch = new Chunk();
		ch.ProcessFaces(faces);
		AddChild(ch);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
