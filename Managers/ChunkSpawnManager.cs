using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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

        //Chunks[pos].ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Chunks[pos].ChunkData = ChunkGeneratorManager.Instance().ComputeGenerateChunk(pos);
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
        //AddChild(chunk);
    }

    public void DeregisterChunk(Chunk chunk, Vector3I pos)
    {
        if (Chunks.ContainsKey(pos))
        {
            Chunks.Remove(pos);
            ChunkList.Remove(chunk);
            chunk.QueueFree();
        }
    }

    bool CheckChunk(int x, int y, int z)
    {
        if (Chunks.ContainsKey(new Vector3I(x, y, z)) && Chunks[new Vector3I(x, y, z)].ChunkData != null)
        {
            return true;
        }
        return false;
    }

    void InitializeChunk(int x, int y, int z)
    {
        if (CheckChunk(x, y, z))
        {
            return;
        }

        Chunk ch = new Chunk();
        AddChunk(ch, x, y, z); //adds chunk as child in here

        //ch.ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Vector3I position = new Vector3I(x,y,z);
        ch.ChunkData = ChunkGeneratorManager.Instance().ComputeGenerateChunk(position);
        //Task.Run(() => {
          //  Vector3I position = new Vector3I(x, y, z);
            //ch.ChunkData = ChunkGeneratorManager.Instance().ComputeGenerateChunk(position); });

    }

    void GenerateChunkMesh(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        if (CheckChunk(x,y,z) && !Chunks[pos].Generated)
        {
            Chunks[pos].Generated = true;
            GeneratePChunk(x, y, z);
        } else
        {
            return;
        }
    }

    public async void HandleChunkLoading()
    {
        await Task.Run(() =>
        {
            //nonsense starting value
            Vector3 lastPos = new Vector3(12,12,12);
            while (IsInsideTree() && !IsQueuedForDeletion())
            {
                List<Vector3I> chunks = new List<Vector3I>();
                Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
                if (pos != lastPos)
                {
                    Vector3 ChunkPos = (pos + new Vector3(GameConstants.CHUNK_SIZE / 2, 0, GameConstants.CHUNK_SIZE / 2)) / GameConstants.CHUNK_SIZE;
                    for (int i = (int)ChunkPos.X - GameConstants.SPAWN_RADIUS; i < (int)ChunkPos.X + GameConstants.SPAWN_RADIUS; i++)
                    {
                        for (int j = (int)ChunkPos.Z - GameConstants.SPAWN_RADIUS; j < (int)ChunkPos.Z + GameConstants.SPAWN_RADIUS; j++)
                        {
                            if ((ChunkPos - new Vector3(i, 0, j)).Length() < GameConstants.SPAWN_RADIUS)
                            {
                                if (!chunks.Contains(new Vector3I(i, 0, j)) && !Chunks.ContainsKey(new Vector3I(i, 0, j))) {
                                    chunks.Add(new Vector3I(i, 0, j));
                                }
                                /*
                                if (!chunks.Contains(new Vector3I(i, 1, j)) && !Chunks.ContainsKey(new Vector3I(i, 1, j)))
                                {
                                    chunks.Add(new Vector3I(i, 1, j));
                                }*/
                                //InitializeChunk(i, 0, j);
                                //GenerateChunkMesh(i, 0, j);
                            }
                        }
                    }

                    while (chunks.Count > 0) {
                        lastPos = pos;
                        Vector3I ChunkToUpdate = new Vector3I();
                        bool hasChunk = false;
                        Basis pBasis = PlayerTrackingManager.Instance().GetPlayerBasis();
                        
                        Vector3 FacingAngle = pBasis * new Vector3(0, 0, 1); /// hopefully this makes sense. rotate a south ray to camera
                        for (int i = 0; i < chunks.Count; i++)
                        {

                            Vector3 distance = chunks[i] * GameConstants.CHUNK_SIZE - PlayerTrackingManager.Instance().GetPlayerLocation();
                            float bestFacing = 1.0f;
                            float dotFacing = distance.Dot(FacingAngle);
                            if (bestFacing > dotFacing)
                            {
                                if (!hasChunk)
                                {
                                    ChunkToUpdate = chunks[i];
                                    hasChunk = true;
                                    bestFacing = dotFacing;
                                }
                                else if (distance.Length() < (ChunkToUpdate * GameConstants.CHUNK_SIZE - PlayerTrackingManager.Instance().GetPlayerLocation()).Length())
                                {
                                    ChunkToUpdate = chunks[i];
                                    hasChunk = true;
                                    bestFacing = dotFacing;
                                }
                            }
                            else if (distance.Length() < (ChunkToUpdate * GameConstants.CHUNK_SIZE - PlayerTrackingManager.Instance().GetPlayerLocation()).Length())
                            {
                                ChunkToUpdate = chunks[i];
                                hasChunk = true;
                                bestFacing = dotFacing;
                            }
                            else if (!hasChunk)
                            {
                                ChunkToUpdate = chunks[i];
                                hasChunk = true;
                                bestFacing = dotFacing;
                            }
                            
                        }
                        chunks.Remove(ChunkToUpdate);
                        InitializeChunk(ChunkToUpdate.X, ChunkToUpdate.Y, ChunkToUpdate.Z);
                        GenerateChunkMesh(ChunkToUpdate.X, ChunkToUpdate.Y, ChunkToUpdate.Z);
                    }
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
