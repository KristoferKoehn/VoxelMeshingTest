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
		int side = 128;

		int ChunkSize = side*side*side;


		uint[] chunkData = new uint[ChunkSize];

		for (int i = 0; i < ChunkSize; i++)
		{
			chunkData[i] = RNGManager.Instance().rng.Randi() % 2;
		}
		
		return chunkData;
	}
}
