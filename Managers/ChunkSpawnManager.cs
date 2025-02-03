using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkSpawnManager : Node
{

    Dictionary<Vector3I, Chunk> Chunks = new Dictionary<Vector3I, Chunk>();

    private static ChunkSpawnManager instance;

    private object lockObj = new object();
    private List<Thread> ThreadList = new List<Thread>();
    private int ThreadCountTarget = GameConstants.CHUNK_THREADS;

    private ConcurrentQueue<Vector3I> ChunkCandidates = new ConcurrentQueue<Vector3I>();

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
        lock (lockObj)
        {
            ThreadCountTarget = GameConstants.CHUNK_THREADS;

            if (GameConstants.CHUNK_THREADS > ThreadList.Count)
            {
                // Start new threads
                for (int i = ThreadList.Count; i < GameConstants.CHUNK_THREADS; i++)
                {
                    Thread thread = new Thread(() => ChunkerThread(i));
                    thread.IsBackground = true;
                    ThreadList.Add(thread);
                    thread.Start();
                }
            }
            else if (GameConstants.CHUNK_THREADS < ThreadList.Count)
            {
                // Let extra threads exit gracefully
                ThreadList.RemoveAll(t => !t.IsAlive);
            }
        }
    }

    void GeneratePChunk(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        //Chunks[pos].ChunkData = ChunkGeneratorManager.Instance().GenerateChunk(x, y, z);
        Chunks[pos].ChunkCoordinates = pos;
        Chunks[pos].ChunkData = ChunkGeneratorManager.Instance().ComputeGenerateChunk(pos);
        ChunkMeshManager.Instance().RequestChunkMeshUpdate(Chunks[pos]);
    }

    void AddChunk(Chunk chunk, int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);

        Chunks[pos] = chunk;
        chunk.ChunkPosition = pos * GameConstants.CHUNK_SIZE;
        chunk.Generated = false;
        CallDeferred("add_child", chunk);
        //AddChild(chunk);
    }

    public void DeregisterChunk(Chunk chunk, Vector3I pos)
    {

        if (chunk.ChunkData == null)
        {
            GD.Print("Deleting ungenerated chunk");
            return;
        }

        if (Chunks.ContainsKey(pos))
        {
            Chunks.Remove(pos);
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
                            float bestFacing = -1.0f;
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

    public struct RenderDeviceFrame {
        public RenderingDevice MesherRenderDevice;
        public RenderingDevice GeneratorRenderDevice;
        public Rid MesherShaderRID;
        public Rid GreedyShaderRID;
        public Rid TerrainShaderRID;

        public Rid VoxelDataBuffer;
        public RDUniform VoxelDataUniform;

        public Rid QuadBuffer;
        public RDUniform QuadUniform;

        public Rid GreedyBuffer;
        public RDUniform GreedyUniform;
    }


    void ChunkerThread(int threadID)
    {
        //initialize the RD 
        RenderDeviceFrame rdFrame = new RenderDeviceFrame();
        rdFrame.MesherRenderDevice = RenderingServer.CreateLocalRenderingDevice();
        rdFrame.GeneratorRenderDevice = RenderingServer.CreateLocalRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast5.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        rdFrame.MesherShaderRID = rdFrame.MesherRenderDevice.ShaderCreateFromSpirV(shaderBytecode);

        RDShaderFile GreedyShaderFile = GD.Load<RDShaderFile>("res://Compute/GreedyMesher.glsl");
        RDShaderSpirV GreedyShaderBytecode = GreedyShaderFile.GetSpirV();
        rdFrame.GreedyShaderRID = rdFrame.MesherRenderDevice.ShaderCreateFromSpirV(GreedyShaderBytecode);

        RDShaderFile TerrainShaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkGen.glsl");
        RDShaderSpirV TerrainShaderBytecode = TerrainShaderFile.GetSpirV();
        rdFrame.TerrainShaderRID = rdFrame.GeneratorRenderDevice.ShaderCreateFromSpirV(TerrainShaderBytecode);

        rdFrame.QuadBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(GameConstants.BUFFER_SIZE);
        //output quad uniform
        rdFrame.QuadUniform = new RDUniform();
        rdFrame.QuadUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.QuadUniform.Binding = 0;
        rdFrame.QuadUniform.AddId(rdFrame.QuadBuffer);

        rdFrame.GreedyBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(6291456 + GameConstants.BUFFER_SIZE); //6291456
        //greedy uniform
        rdFrame.GreedyUniform = new RDUniform();
        rdFrame.GreedyUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.GreedyUniform.Binding = 5;
        rdFrame.GreedyUniform.AddId(rdFrame.GreedyBuffer);

        rdFrame = ChunkMeshManager.Instance().InitializeVoxelData(rdFrame);


        while (IsInsideTree() && !IsQueuedForDeletion())
        {
            lock (lockObj)
            {
                if (threadID >= ThreadCountTarget)
                {
                    //dispose of rdFrame
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.QuadBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.GreedyBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.MesherShaderRID);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.GreedyShaderRID);
                    rdFrame.MesherRenderDevice.Free();

                    rdFrame.GeneratorRenderDevice.FreeRid(rdFrame.TerrainShaderRID);
                    rdFrame.GeneratorRenderDevice.Free();

                    break;
                }
                
            }
            //do the stuff
            int[,,] ChData = ChunkGeneratorManager.Instance().ComputeGenerateChunk2(new Vector3I(0, 0, 0), rdFrame);
            
        }

    }

    public void GetCandidateChunks()
    {
        Vector3 playerPos = PlayerTrackingManager.Instance().GetPlayerLocation();
        Vector3I PlayerCoordinate = (Vector3I)(playerPos / GameConstants.CHUNK_SIZE);
        for (int i = 0; i < GameConstants.SPAWN_RADIUS * 2; i++)
        {
            for (int j = 0; j < GameConstants.SPAWN_RADIUS * 2; j++)
            {
                Vector3I Pos = PlayerCoordinate + new Vector3I(i, 0, j);
                if (Chunks.ContainsKey(Pos))
                {
                    if (Chunks[Pos] != null)
                    {
                        if (Chunks[Pos].Stale == true)
                        {
                            ChunkCandidates.Enqueue(Pos);
                        }
                    }
                }
                else
                {
                     ChunkCandidates.Enqueue(Pos);
                }
            }
        }
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
