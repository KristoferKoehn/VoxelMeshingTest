using Godot;
using System.Collections.Generic;

public partial class ChunkGeneratorManager : Node
{

	private static ChunkGeneratorManager instance = null;

	private ChunkGeneratorManager() { }

	public static FastNoiseLite Terrain {  get; set; }
	public static FastNoiseLite SurfaceCutoff {  get; set; }

	public static Vector2 CutoffOffset { get; set; }


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

    int ModifyByteInInt(int currentInt, int z, byte newValue)
    {
        // Calculate the bit shift amount based on the byte index
        int shiftAmount = (3 - z % 4) * 8;

        // Clear the byte at the specified index
        int mask = ~(0xFF << shiftAmount);
        currentInt &= mask;

        // Set the new byte value at the specified index
        currentInt |= (newValue << shiftAmount);

        return currentInt;
    }

    public int[] GenerateChunk(int x, int y, int z) {
		//get the data from some bullshit elsewhere. 
		int side = 128;

		int dataSideLength = side + 2;
		int ChunkSize = dataSideLength * dataSideLength * dataSideLength;

		int[] chunkData = new int[ChunkSize];

        for (int i = 0; i < dataSideLength; i++)
		{
            for (int j = 0; j < dataSideLength; j++)
            {
				for(int k = 0; k < dataSideLength; k++)
				{

                    float cutoffmod = (SurfaceCutoff.GetNoise2D(i + (x * side) + CutoffOffset.X, k + (z * side) + CutoffOffset.Y) * 512) / 30;

					//chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = (uint)RNGManager.Instance().rng.Randi() % 2;

					//if (j > 32 + cutoffmod && j < 96 + cutoffmod)

					if (j > 32 + cutoffmod) // && j < 96 + cutoffmod)
                    {
                        chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = 0;
                    }
                    else if (Terrain.GetNoise3D(i + (x * side), j + (y * side), k + (z * side)) > 0.5)
                    //else if (Terrain.GetNoise3D(i + (x * side), j + (y * side), k + (z * side)) > 0.5)
                    {
						if (j + cutoffmod > 10)
						{
                            chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = 1;
                        } else
						{
                            chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = 2;
                        }

                    } 
                    //chunkData[i + j * side + k*side*side] = 1;
                }
            }
        }

		/*
		for (int i = 0; i < ChunkSize; i++)
		{
			//chunkData[i] = (uint)i % 2;
			//chunkData[i] = (uint)RNGManager.Instance().rng.Randi() % 2;
        }
		*/
		
		return chunkData;
	}
}
