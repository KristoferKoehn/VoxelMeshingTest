using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VoxelMeshingTest.Classes;

public partial class ChunkSpawnManager : Node
{

    [Signal]
    public delegate void DeleteAllChunksEventHandler();

    ConcurrentDictionary<Vector3I, Chunk> Chunks = new ConcurrentDictionary<Vector3I, Chunk>();

    private static ChunkSpawnManager instance;

    private object lockObj = new object();
    private object GenerateLock = new object();

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
        //ChangeThreading(GameConstants.CHUNK_THREADS);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
        if (Input.IsActionJustPressed("regenerate"))
        {
            GameConstants.CHUNK_THREADS = 0;
            EmitSignal(SignalName.DeleteAllChunks);
            GetTree().CreateTimer(0.4).Timeout += () =>
            {
                EmitSignal(SignalName.DeleteAllChunks);
                GameConstants.CHUNK_THREADS = 1;
            };
        }

        ChangeThreading(GameConstants.CHUNK_THREADS);
    }

    public void ChangeThreading(int threads)
    {
        lock (lockObj)
        {
            ThreadCountTarget = threads;

            if (threads > ThreadList.Count)
            {
                // Start new threads
                for (int i = ThreadList.Count; i < threads; i++)
                {
                    int threadID = i;  // Capture loop variable safely
                    Thread thread = new Thread(() => ChunkerThread(threadID));
                    thread.IsBackground = true;
                    ThreadList.Add(thread);
                    GD.Print($"starting new thread {threadID}");
                    thread.Start();
                }
            }
            else if (threads < ThreadList.Count)
            {
                // Let extra threads exit gracefully
                GD.Print("Removing ended threads");
                ThreadList.RemoveAll(t => !t.IsAlive);
            }
            else
            {
                //do nothing
            }
        }

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
            Chunks.TryRemove(new KeyValuePair<Vector3I,Chunk>(pos, chunk));
        }
    }

    public struct RenderDeviceFrame {
        public RenderingDevice MesherRenderDevice;
        public RenderingDevice GeneratorRenderDevice;

        public int[] ints;

        public Rid MesherShaderRID;
        public Rid GreedyShaderRID;
        public Rid TerrainShaderRID;
        public Rid CompressorShaderRID;

        public Rid VoxelDataBuffer;
        public RDUniform VoxelDataUniform;

        public Rid QuadBuffer;
        public RDUniform QuadUniform;

        public Rid GreedyBuffer;
        public RDUniform GreedyUniform;

        public Rid CompressorBuffer;
        public RDUniform CompressorUniform;
    }

    void ChunkerThread(int threadID)
    {
        GD.Print($"thread {threadID} initializing");
        //initialize the RD 
        RenderDeviceFrame rdFrame = new RenderDeviceFrame();
        rdFrame.MesherRenderDevice = RenderingServer.CreateLocalRenderingDevice();
        rdFrame.GeneratorRenderDevice = RenderingServer.CreateLocalRenderingDevice();

        RDShaderFile shaderFile = ResourceLoader.Load<RDShaderFile>("res://Compute/ChunkMesherFast5.glsl", cacheMode: ResourceLoader.CacheMode.Ignore);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        rdFrame.MesherShaderRID = rdFrame.MesherRenderDevice.ShaderCreateFromSpirV(shaderBytecode);

        RDShaderFile GreedyShaderFile = ResourceLoader.Load<RDShaderFile>("res://Compute/GreedyMesher.glsl", cacheMode: ResourceLoader.CacheMode.Ignore);
        RDShaderSpirV GreedyShaderBytecode = GreedyShaderFile.GetSpirV();
        rdFrame.GreedyShaderRID = rdFrame.MesherRenderDevice.ShaderCreateFromSpirV(GreedyShaderBytecode);

        RDShaderFile TerrainShaderFile = ResourceLoader.Load<RDShaderFile>("res://Compute/ChunkGen.glsl", cacheMode: ResourceLoader.CacheMode.Ignore);
        RDShaderSpirV TerrainShaderBytecode = TerrainShaderFile.GetSpirV();
        rdFrame.TerrainShaderRID = rdFrame.GeneratorRenderDevice.ShaderCreateFromSpirV(TerrainShaderBytecode);

        RDShaderFile CompressorShaderFile = ResourceLoader.Load<RDShaderFile>("res://Compute/Compressor.glsl", cacheMode: ResourceLoader.CacheMode.Ignore);
        RDShaderSpirV CompressorShaderBytecode = CompressorShaderFile.GetSpirV();
        rdFrame.CompressorShaderRID = rdFrame.MesherRenderDevice.ShaderCreateFromSpirV(CompressorShaderBytecode);

        rdFrame.QuadBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(GameConstants.BUFFER_SIZE);
        //output quad uniform
        rdFrame.QuadUniform = new RDUniform();
        rdFrame.QuadUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.QuadUniform.Binding = 0;
        rdFrame.QuadUniform.AddId(rdFrame.QuadBuffer);

        rdFrame.GreedyBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(6291456 + GameConstants.BUFFER_SIZE); //6291456
        rdFrame.GreedyUniform = new RDUniform();
        rdFrame.GreedyUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.GreedyUniform.Binding = 5;
        rdFrame.GreedyUniform.AddId(rdFrame.GreedyBuffer);

        rdFrame.VoxelDataBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(256 * 4000 + 32 * 4000, ChunkMeshManager.Instance().GetVoxelDataBytes());
        rdFrame.VoxelDataUniform = new RDUniform();
        rdFrame.VoxelDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.VoxelDataUniform.Binding = 4;
        rdFrame.VoxelDataUniform.AddId(rdFrame.VoxelDataBuffer);

        rdFrame.CompressorBuffer = rdFrame.MesherRenderDevice.StorageBufferCreate(264 * (64*64*64) / 2);
        rdFrame.CompressorUniform = new RDUniform();
        rdFrame.CompressorUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        rdFrame.CompressorUniform.Binding = 6;
        rdFrame.CompressorUniform.AddId(rdFrame.CompressorBuffer);
        rdFrame.ints = new int[66000 * 4];

        for (int i = 0; i < 11000 * 4; i++)
        {
            rdFrame.ints[i * 6 + 0] = i * 4 + 0;
            rdFrame.ints[i * 6 + 1] = i * 4 + 1;
            rdFrame.ints[i * 6 + 2] = i * 4 + 2;
            rdFrame.ints[i * 6 + 3] = i * 4 + 0;
            rdFrame.ints[i * 6 + 4] = i * 4 + 2;
            rdFrame.ints[i * 6 + 5] = i * 4 + 3;
        }


        Vector3I PlayerCoordinateLast = new Vector3I(-20, 20, -4000);
        GD.Print($"{threadID} starting loop");
        while (IsInsideTree() && !IsQueuedForDeletion())
        {
            Stopwatch sw = Stopwatch.StartNew();
            
            lock (lockObj)
            {
                if (threadID >= ThreadCountTarget)
                {
                    //dispose of rdFrame
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.QuadBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.GreedyBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.MesherShaderRID);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.GreedyShaderRID);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.CompressorBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.VoxelDataBuffer);
                    rdFrame.MesherRenderDevice.FreeRid(rdFrame.CompressorShaderRID);
                    rdFrame.MesherRenderDevice.Free();
                    rdFrame.GeneratorRenderDevice.FreeRid(rdFrame.TerrainShaderRID);
                    rdFrame.GeneratorRenderDevice.Free();
                    GD.Print($"Thread {threadID} disposing...");
                    break;
                }
            }

            List<Vector3I> PosList = new List<Vector3I>();
            Vector3 playerPos = PlayerTrackingManager.Instance().GetPlayerLocation() - PlayerTrackingManager.Instance().GetPlayerVelocity() * 30; // + velocity * 30 to only look in the direction we're moving
            Vector3I PlayerCoordinate = (Vector3I)(playerPos / GameConstants.CHUNK_SIZE);
            PlayerCoordinate = new Vector3I(PlayerCoordinate.X, 0, PlayerCoordinate.Z);

            for (int i = -GameConstants.SPAWN_RADIUS; i < GameConstants.SPAWN_RADIUS; i++)
            {
                for (int j = -GameConstants.SPAWN_RADIUS; j < GameConstants.SPAWN_RADIUS; j++)
                {
                    for (int k = GameConstants.WORLD_DEPTH; k < GameConstants.WORLD_HEIGHT; k++)
                    {

                        Vector3I Pos = PlayerCoordinate + new Vector3I(i, k, j);

                        if ((Pos - PlayerCoordinate).Length() > GameConstants.SPAWN_RADIUS)
                        {
                            continue;
                        }

                        if (Chunks.ContainsKey(Pos))
                        {
                            if (Chunks.ContainsKey(Pos) && Chunks[Pos] != null) //it keeps breaking on these so I have to check containskey a bunch
                            {
                                if (Chunks.ContainsKey(Pos) && Chunks[Pos].Stale == true)
                                {

                                    PosList.Add(Pos);

                                } //else do nothing
                            } //else do nothing
                        }
                        else
                        {

                            PosList.Add(Pos);

                        } //else do nothing

                    }
                }
            }
            
            PlayerCoordinateLast = PlayerCoordinate;
        

            double CandidateList = sw.ElapsedMilliseconds;

            Vector3I Candidate = Vector3I.Zero;


            //while can get chunks
            if (PosList.Count == 0)
            {
                continue;
            }

            Vector3 PlayerOffset = PlayerTrackingManager.Instance().GetPlayerLocation();
            Candidate = FindBestPosition(PlayerTrackingManager.Instance().GetPlayerBasis().GetRotationQuaternion() * new Vector3(0, 0, -1), PlayerOffset, PosList);
            Chunk chunk = new Chunk();


            if (!Chunks.TryAdd(Candidate, chunk))
            {
                if (Chunks.ContainsKey(Candidate) && Chunks[Candidate] != null)
                {
                    if (IsInstanceValid(Chunks[Candidate]) && !Chunks[Candidate].IsQueuedForDeletion() && Chunks[Candidate].Stale)
                    {
                        //Chunks[Candidate].CallDeferred("queue_free");
                        //just refresh the fkin chunkie and return
                        ChunkMeshManager.Instance().GenerateChunkMesh(Chunks[Candidate].ChunkData, Chunks[Candidate], rdFrame);
                        //GD.Print("updating stale chunk");
                        Chunks[Candidate].Stale = false;
                        continue;
                    }
                }
                //GD.Print("BAIL, CHUNK ALREADY EXISTS");
                continue;
            }

            double FindBestPositionStamp = sw.ElapsedMilliseconds;

            int[,,] ChData;
            double GenerateStamp;
            //do the stuff
            lock (GenerateLock)
            {
                ChData = ChunkGeneratorManager.Instance().ComputeGenerateChunk(Candidate, rdFrame);
                GenerateStamp = sw.ElapsedMilliseconds;
            
                chunk.ChunkData = ChData;
                chunk.ChunkPosition = new Vector3(Candidate.X * GameConstants.CHUNK_SIZE, Candidate.Y * GameConstants.CHUNK_SIZE, Candidate.Z * GameConstants.CHUNK_SIZE);
                chunk.ChunkCoordinates = Candidate;
            
                ChunkMeshManager.Instance().GenerateChunkMesh(ChData, chunk, rdFrame);
            }
            double MeshStamp = sw.ElapsedMilliseconds;
            DeleteAllChunks += chunk.SpecialDispose;
            CallDeferred("add_child", chunk);

            //GD.Print($"adding chunk from thread {threadID} at {Candidate}, Candidate List: {CandidateList}, FindBestPosition: {FindBestPositionStamp}, Generate Stamp: {GenerateStamp}, Mesh Stamp: {MeshStamp}");
        }
    }

 
    Vector3I FindBestPosition(Vector3 forwardView, Vector3 PlayerPosition, List<Vector3I> positions)
    {
        Vector3I bestPosition = positions[0];
        float bestScore = float.MinValue;

        float speedFactor = Mathf.Clamp(PlayerTrackingManager.Instance().GetPlayerVelocity().Length() / 10f, 0f, 30f);
        float dynamicWeight = Mathf.Lerp(0.001f, 0.05f, speedFactor);

        foreach (var pos in positions)
        {
            Vector3 worldPos = new Vector3(pos.X, pos.Y, pos.Z) * 64;
            float distance = (PlayerPosition - worldPos).Length();
            float alignment = (worldPos - PlayerPosition).Normalized().Dot(forwardView);

            // Weighted score: prioritize alignment but still consider distance
            float score = alignment - (distance * GameConstants.ALIGNMENT_SCORE_WEIGHT); // Adjust weighting as needed

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = pos;
            }
        }

        return bestPosition;
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
