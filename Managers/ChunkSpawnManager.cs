using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkSpawnManager : Node
{

    /*
     * 
     * 
     * convert dictionary to <Vector3I, Chunk> for keeping track of shit better
     * 
     * 
     * 
     * Building out the system such that:
     * Chunks initialize and know their own data in a large range
     * within a smaller range, the chunks become meshed
     * 
     * outside the initialization range, dispose of the chunk. !! BE CAREFUL, DELETING CHUNKS WHILE WORKING IN ANOTHER STEP IS A HAZARD
     * 
     * I think these three things can happen on their own threads, just need lockout booleans. 
     * 
     * 
     * 
     * gotta spawn in chunks around the player.
     * 
     * get player location, divide by 128
     * 
     * loop over x, y square, check if within render distance. 
     * 
     * if 1.5 times render distance, spawn a chunk
     * 
     * if 1 times render distance, check files and/or generate
     * 
     * 
     * 
     */

    Dictionary<Vector3I, Chunk> Chunks = new Dictionary<Vector3I, Chunk>();

    List<Chunk> ChunkList = new List<Chunk>();

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

        /*
        for (int i = -1; i < 0; i++)
        {
            for (int j = -1; j < 0; j++)
            {
                if ((new Vector3I(0,0,0) - new Vector3(i, 0, j)).Length() < 2)
                {
                    InitializeChunk(i, 0, j);
                    GenerateChunkMesh(i, 0, j);
                }
            }
        }
        */
        HandleChunkLoading();        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{

    }

    void GeneratePChunk(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);
        int[] data = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Chunks[pos].ChunkData = data;
        ChunkMeshManager.Instance().RequestChunkMeshUpdate(Chunks[pos]);
    }

    void AddChunk(Chunk chunk, int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        Chunks[pos] = chunk;
        chunk.ChunkPosition = pos * GameConstants.CHUNK_SIZE;
        chunk.Generated = false;
        ChunkList.Add(chunk);
        CallDeferred("add_child", chunk);
    }

    bool CheckChunk(int x, int y, int z)
    {
        return Chunks.ContainsKey(new Vector3I(x, y, z));
    }

    void InitializeChunk(int x, int y, int z)
    {
        if (CheckChunk(x, y, z))
        {
            return;
        }

        Chunk ch = new Chunk();
        AddChunk(ch, x, y, z); //adds chunk as child in here

        ch.ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
    }

    void GenerateChunkMesh(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        if (!Chunks[pos].Generated)
        {
            Chunks[pos].Generated = true;
            ThreadPool.QueueUserWorkItem(async state =>
            {
                await Task.Run(() => {
                    int xC = x;
                    int yC = y;
                    int zC = z;

                    GeneratePChunk(xC, yC, zC);
                });
            });
        } else
        {
            //already generated
            return;
        }
    }

    async void HandleChunkLoading()
    {
        await Task.Run(() =>
        {

            while (true)
            {
                Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
                Vector3 ChunkPos = (pos + new Vector3(GameConstants.CHUNK_SIZE/2, 0, GameConstants.CHUNK_SIZE / 2)) / GameConstants.CHUNK_SIZE;

                for (int i = (int)ChunkPos.X - 13; i < (int)ChunkPos.X + 13; i++)
                {
                    for (int j = (int)ChunkPos.Z - 13; j < (int)ChunkPos.Z + 13; j++)
                    {
                        if ((ChunkPos - new Vector3(i, 0, j)).Length() < 13)
                        {
                            InitializeChunk(i, 0, j);
                        }
                    }
                }

                Vector3 ChunkPosCopy = ChunkPos;

                for (int i = (int)ChunkPosCopy.X - 10; i < (int)ChunkPosCopy.X + 10; i++)
                {
                    for (int j = (int)ChunkPosCopy.Z - 10; j < (int)ChunkPosCopy.Z + 10; j++)
                    {
                        if ((ChunkPosCopy - new Vector3(i, 0, j)).Length() < 10)
                        {
                            GenerateChunkMesh(i, 0, j);
                        }
                    }
                }

                Thread.Sleep(100);
            }
        });
    }
}
