using Godot;
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

    void GenerateChunk(Chunk ch, int x, int y, int z)
    {
        int[] data = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        float[] meshBytes = ChunkMeshManager.GenerateMeshFastBytes(data, new Vector3(x,y,z));
        
        ChunkList.Add(ch);
        ch.ProcessBytes(meshBytes);
        //AddChild(chunk);
        /*
        CallDeferred("add_child", chunk);
        chunk.GlobalPosition = new Vector3(x * 128, y * 128, z * 128);
        */
    }

    void GeneratePChunk(int x, int y, int z)
    {
        int[] data = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        ChunkMeshManager.Instance().GeneratePChunkMesh(data, new Vector3(x,y,z), Chunks[x][y][z]);
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
            chunk.ChunkPosition = new Vector3(x * 128, y * 128, z * 128);
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
        AddChunk(ch, x, y, z);


        ch.ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);

        //AddChild(ch);
    }

    void GenerateChunkMesh(int x, int y, int z)
    {
        if (CheckChunk(x, y, z))
        {
            if (!Chunks[x][y][z].Generated)
            {
                Chunks[x][y][z].Generated = true;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    int xC = x;
                    int yC = y;
                    int zC = z;

                    GeneratePChunk(xC, yC, zC);
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

            GD.Print("Chunk Loading thread start");
            while (true)
            {
                Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
                Vector3 ChunkPos = pos / 128;

                for (int i = (int)ChunkPos.X - 6; i < (int)ChunkPos.X + 6; i++)
                {
                    for (int j = (int)ChunkPos.Z - 6; j < (int)ChunkPos.Z + 6; j++)
                    {
                        if ((ChunkPos - new Vector3(i, 0, j)).Length() < 5)
                        {
                            GD.Print($"initializing chunk {i}, {j}...");
                            InitializeChunk(i, 0, j);
                        }
                    }
                }


                Vector3 ChunkPosCopy = ChunkPos;

                for (int i = (int)ChunkPosCopy.X - 4; i < (int)ChunkPosCopy.X + 4; i++)
                {
                    for (int j = (int)ChunkPosCopy.Z - 4; j < (int)ChunkPosCopy.Z + 4; j++)
                    {
                        if ((ChunkPosCopy - new Vector3(i, 0, j)).Length() < 3)
                        {
                            GenerateChunkMesh(i, 0, j);
                        }
                    }
                }
            }

            
        });
    }
}
