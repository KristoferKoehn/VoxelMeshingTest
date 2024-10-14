using Godot;
using System.Collections.Generic;
using System.Threading;

public partial class ChunkSpawnManager : Node
{

    List<Chunk> Chunks = new List<Chunk>();

    private static ChunkSpawnManager instance;

    private ChunkSpawnManager() { 

    }

    public static ChunkSpawnManager Instance()
    {
        if (instance == null)
        {
            instance = new ChunkSpawnManager();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "ChunkSpawnManager";
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


    public void GenerateWorld()
    {

        foreach (Chunk ch in Chunks)
        {
            ch.QueueFree();
        }
        Chunks.Clear();


        ThreadPool.QueueUserWorkItem(state =>
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {

                        int xCopy = i;
                        int yCopy = j;
                        int zCopy = k;

                        GenerateChunk(xCopy, yCopy, zCopy);
                    }
                }
            }
        });
    }

    void GenerateChunk(int x, int y, int z)
    {

        int[] data = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        float[] meshBytes = ChunkMeshManager.GenerateMeshFastBytes(data, new Vector3(x,y,z));
        Chunk chunk = new Chunk();
        Chunks.Add(chunk);
        chunk.ProcessBytes(meshBytes);
        //AddChild(chunk);
        CallDeferred("add_child", chunk);
        chunk.GlobalPosition = new Vector3(x * 128, y * 128, z * 128);
    }
}
