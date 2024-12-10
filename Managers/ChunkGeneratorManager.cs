using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkGeneratorManager : Node
{

	private static ChunkGeneratorManager instance = null;

	private ChunkGeneratorManager() { }

	public static FastNoiseLite Terrain {  get; set; }
	public static FastNoiseLite SurfaceCutoff {  get; set; }
	public static Vector2 CutoffOffset { get; set; }
	public Dictionary<Vector3I, int[,,]> GeneratedChunks { get; set; } = new Dictionary<Vector3I, int[,,]>();

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

    public int[,,] GenerateChunk(int x, int y, int z) {

		//get the data from some bullshit elsewhere. 
		int side = GameConstants.CHUNK_SIZE;
		Vector3I pos = new Vector3I(x, y, z);
		int dataSideLength = GameConstants.CHUNK_DATA_SIZE;
		int ChunkSize = dataSideLength * dataSideLength * dataSideLength;

		int[,,] chunkData;

        GeneratedChunks.TryGetValue(pos, out chunkData);
		if (chunkData != null) { 
			return chunkData;
		}

		chunkData = new int[GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE];

        Parallel.For (0, dataSideLength, k =>
		{
			Parallel.For(0, dataSideLength, j =>
			{
				Parallel.For(0, dataSideLength, i =>
				{

					float cutoffmod = (SurfaceCutoff.GetNoise2D(i + (x * side) + CutoffOffset.X, k + (z * side) + CutoffOffset.Y) * 128) / 30;

					//chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = (uint)RNGManager.Instance().rng.Randi() % 2;


					if (j > 15 + cutoffmod && j < 128 + cutoffmod || j == 65 || j == 0)// && j < 96 + cutoffmod)
					{
						chunkData[i, j, k] = 0;
					}
					else if (Terrain.GetNoise3D(i + (x * side), j + (y * side), k + (z * side)) > 0.5)
					{/*
                        chunkData[k + j * dataSideLength + i * dataSideLength * dataSideLength] = 1;
                        */
						if (j + cutoffmod > 10)
						{
                            chunkData[i, j, k] = 1;
						}
						else
						{
                            chunkData[i, j, k] = 1;
						}
                    }
				});
			});
        });
		
		/*
        for (int i = 0; i < dataSideLength; i++)
		{
			for (int j = 0; j < dataSideLength; j++)
			{
				for (int k = 0; k < dataSideLength; k++)
				{
					
					//chunkData[i + j * side + k*side*side] = 1;
				}
			}
        } */

		GeneratedChunks[pos] = chunkData;
		
		return chunkData;
	}
}
