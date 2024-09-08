using Godot;
using System;

public partial class ChunkGeneratorManager : Node
{

	private static ChunkGeneratorManager instance = null;

	private ChunkGeneratorManager() { }

	
	public static ChunkGeneratorManager Instance()
	{
		if (instance == null)
		{
			instance = new ChunkGeneratorManager();
			SceneSwitcher.root.AddChild(instance);
			instance.Name = "ChunkGeneratorManager";
		}

		return instance;
	}



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}


	public uint[] GenerateChunk(int x, int y, int z) {
		//get the data from some bullshit elsewhere. 

		uint[] chunkData = new uint[256];

		for (int i = 0; i < 256; i++)
		{
			chunkData[i] = RNGManager.Instance().rng.Randi() % 2;
		}
		
		return chunkData;
	}
}
