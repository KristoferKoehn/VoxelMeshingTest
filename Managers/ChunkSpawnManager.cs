using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkSpawnManager : Node
{

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
        //HandleChunkLoading();        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{

    }

    void GeneratePChunk(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        Chunks[pos].ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Chunks[pos].ChunkCoordinates = pos;
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

    public void DeregisterChunk(Chunk chunk, Vector3I pos)
    {
        Chunks.Remove(pos);
        ChunkList.Remove(chunk);
        chunk.QueueFree();
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

        if (CheckChunk(x,y,z) && !Chunks[pos].Generated)
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

    public async void HandleChunkLoading()
    {
        await Task.Run(() =>
        {
            //nonsense starting value
            Vector3 lastPos = new Vector3(12,12,12);
            while (true)
            {

                Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
                if (pos != lastPos)
                {
                    Vector3 ChunkPos = (pos + new Vector3(GameConstants.CHUNK_SIZE / 2, 0, GameConstants.CHUNK_SIZE / 2)) / GameConstants.CHUNK_SIZE;

                    for (int i = (int)ChunkPos.X - 8; i < (int)ChunkPos.X + 8; i++)
                    {
                        for (int j = (int)ChunkPos.Z - 8; j < (int)ChunkPos.Z + 8; j++)
                        {
                            if ((ChunkPos - new Vector3(i, 0, j)).Length() < 8)
                            {
                                InitializeChunk(i, 0, j);
                            }
                        }
                    }

                    Vector3 ChunkPosCopy = ChunkPos;

                    for (int i = (int)ChunkPosCopy.X - 6; i < (int)ChunkPosCopy.X + 6; i++)
                    {
                        for (int j = (int)ChunkPosCopy.Z - 6; j < (int)ChunkPosCopy.Z + 6; j++)
                        {
                            if ((ChunkPosCopy - new Vector3(i, 0, j)).Length() < 6)
                            {
                                GenerateChunkMesh(i, 0, j);
                            }
                        }
                    }
                    lastPos = pos;
                }
            }
        });
    }

    public Chunk GetChunk(Vector3I pos)
    {
        if (!Chunks.ContainsKey(pos))
        {
            return null;
        }
        return Chunks[pos];
    }
}
