using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> Chunks = new Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>>();

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
        for (int i = -3; i < 3; i++)
        {
            for (int j = -3; j < 3; j++)
            {
                if ((new Vector3I(0,0,0) - new Vector3(i, 0, j)).Length() < 5)
                {
                    InitializeChunk(i, 0, j);
                }
            }
        }*/

        HandleChunkLoading();        
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

    }

    public void GenerateWorld()
    {
        foreach(Chunk chunk in ChunkList)
        {
            chunk.QueueFree();
        }
        ChunkList.Clear();

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
                        AddChunk(new Chunk(), i, j, k);
                        //GenerateChunk(xCopy, yCopy, zCopy);
                        GeneratePChunk(xCopy, yCopy, zCopy);
                    }
                }
            }
        });
    }

    void GeneratePChunk(int x, int y, int z)
    {
        int[] data = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Chunks[x][y][z].ChunkData = data;
        ChunkMeshManager.Instance().RequestChunkMeshUpdate(Chunks[x][y][z]);
    }

    void AddChunk(Chunk chunk, int x, int y, int z)
    {
        if (!Chunks.ContainsKey(x))
        {
            Chunks[x] = new Dictionary<int, Dictionary<int, Chunk>>();
        } 
        if (!Chunks[x].ContainsKey(y))
        {
            Chunks[x][y] = new Dictionary<int, Chunk>();
        }
        if (!Chunks[x][y].ContainsKey(z))
        {
            Chunks[x][y][z] = chunk;
            chunk.ChunkPosition = new Vector3(x * 32, y * 32, z * 32);
            //chunk.Visible = false;
        }

        chunk.Generated = false;
        ChunkList.Add(chunk);
        CallDeferred("add_child", chunk);
    }

    bool CheckChunk(int x, int y, int z)
    {
        if (!Chunks.ContainsKey(x))
        {
            return false;
        }
        if (!Chunks[x].ContainsKey(y))
        {
            return false;
        }
        if (!Chunks[x][y].ContainsKey(z))
        {
            return false;
        }

        return true;
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
        if (CheckChunk(x, y, z))
        {
            if (!Chunks[x][y][z].Generated)
            {
                Chunks[x][y][z].Generated = true;
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

        } else
        {
            GD.PrintErr("Nonexistent chunk attempted to generate at " + new Vector3(x,y,z) + "!");
        }
    }

    async void HandleChunkLoading()
    {
        await Task.Run(() =>
        {
            while (true)
            {
                Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
                Vector3 ChunkPos = (pos + new Vector3(16, 0, 16)) / 32;

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

                for (int i = (int)ChunkPosCopy.X - 5; i < (int)ChunkPosCopy.X + 5; i++)
                {
                    for (int j = (int)ChunkPosCopy.Z - 5; j < (int)ChunkPosCopy.Z + 5; j++)
                    {
                        if ((ChunkPosCopy - new Vector3(i, 0, j)).Length() < 5)
                        {
                            GenerateChunkMesh(i, 0, j);
                        }
                    }
                }
            }
        });
    }
}
