using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkMeshManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private static ChunkMeshManager instance = null;

    public static int ChunkCount = 0;
    public ConcurrentQueue<Chunk> ChunksToUpdate { get; set; } = new ConcurrentQueue<Chunk>();

    private ChunkMeshManager() { }

    private ShaderWrapper ShaderWrapper { get; set; }

    RenderingDevice rd { get; set; }

    private byte[] VoxelData;

    public static ChunkMeshManager Instance()
	{
		if (instance == null)
		{
			instance = new ChunkMeshManager();
			SceneSwitcher.Instance().AddChild(instance);
			instance.Name = "ChunkMeshManager";
            instance.rd = RenderingServer.CreateLocalRenderingDevice();
            
        }
		return instance;
	}

	public override void _Ready()
	{
        ShaderWrapper = new ShaderWrapper();
        InitializeVoxelData();
        //HandleChunkMeshing();
    }

    async void HandleChunkMeshing()
    {
        await Task.Run(() =>
        {
            while (true)
            {
                if (ChunksToUpdate.TryDequeue(out Chunk chunk))
                {
                    Chunk ch = chunk;
                    GeneratePChunkMesh4(chunk.ChunkData, chunk);
                }

                Thread.Sleep(15);
            }
        });
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
        if (ChunksToUpdate.Count > 0)
        {
            if (ChunksToUpdate.TryDequeue(out Chunk chunk))
            {
                Chunk ch = chunk;
                GeneratePChunkMesh4(chunk.ChunkData, chunk);
            }
        }
    }

    public void RequestChunkMeshUpdate(Chunk chunk)
    {
        ChunksToUpdate.Enqueue(chunk);
    }

    public void GeneratePChunkMesh3(int[] Data, Chunk chunk)
    {
        ShaderWrapper.AssignMesh(Data, chunk);
    }

    int CallCount = 0;
    public void GeneratePChunkMesh2(int[] Data, Chunk ch)
    {
        //RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast2.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);

        long ComputeList = rd.ComputeListBegin();
        //compute uniform
        byte[] inputBytes = new byte[Data.Length * sizeof(int)];
        Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = GameConstants.CHUNK_SIZE;
        int WorkGroupSide = 64;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 4194304;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate(sizeof(int) * 2);
        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);

        Array<RDUniform> Uniforms = new Array<RDUniform>();

        //output quad uniform
        RDUniform QuadUniform = new RDUniform();
        Uniforms.Add(QuadUniform);
        QuadUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        QuadUniform.Binding = 0;
        QuadUniform.AddId(QuadBuffer);

        //output quad count uniform
        RDUniform QuadCountUniform = new RDUniform();
        Uniforms.Add(QuadCountUniform);
        QuadCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        QuadCountUniform.Binding = 1;
        QuadCountUniform.AddId(QuadCountBuffer);

        //chunk data input uniform
        RDUniform ChunkDataUniform = new RDUniform();
        Uniforms.Add(ChunkDataUniform);
        ChunkDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDataUniform.Binding = 2;
        ChunkDataUniform.AddId(ChunkDataBuffer);

        //chunk data input uniform
        RDUniform ChunkDimensionalUniform = new RDUniform();
        Uniforms.Add(ChunkDimensionalUniform);
        ChunkDimensionalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDimensionalUniform.Binding = 3;
        ChunkDimensionalUniform.AddId(ChunkDimensionalBuffer);
        
        Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        byte[] countBytes = rd.BufferGetData(QuadCountBuffer);
        int[] Count = new int[2];
        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);

        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0]*64);

        rd.BufferClear(QuadBuffer, 0, (uint)Count[0] * 64);
        rd.BufferClear(QuadCountBuffer, 0, 8);

        //ch.PChunkByteIngestion(QBytes, countBytes);
        ch.PChunkByteAssignment(QBytes);

        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ShaderRID);
        rd.FreeRid(ChunkDimensionalBuffer);
        //rd.Free();

        return;
    }

    private void InitializeVoxelData()
    {
        //rewrite this into a system that generates structs that mirror glsl structs
        Array<FaceData> faces = new Array<FaceData>();

        byte[] QuadInData = new byte[528000];
        byte[] FaceData = new byte[80000];
        VoxelData = new byte[528000 + 80000];
        //get list of resources
        int QuadOffset = 0;

        string[] FaceFiles = DirAccess.GetFilesAt("res://VoxelData/FaceData/");
        foreach (string name in FaceFiles)
        {
            if (name.Contains(".tres"))
            {
                faces.Add(ResourceLoader.Load<FaceData>($"res://VoxelData/FaceData/{name}"));
                GD.Print($"adding {name}");
            }
        }

        foreach (FaceData faceData in faces)
        {
            //get QuadInData Offset, 

            Array<Array<QuadData>> quads = faceData.GetFacesArray();

            int[] quadIndices = new int[8];
            if (faceData.transparent)
            {
                quadIndices[6] = 1;
            }

            for(int i = 0; i < 6; i++)
            {
                quadIndices[i] = QuadOffset;
                for (int j = 0; j < quads[i].Count; j++)
                {
                    if (j != quads[i].Count - 1)
                    {
                        quads[i][j].NextFace = QuadOffset + 1;
                    }
                    byte[] b = quads[i][j].Serialize();
                    Buffer.BlockCopy(b, 0, QuadInData, QuadOffset * 256, 132);
                    QuadOffset += 1;
                }
            }
            foreach(int i in  quadIndices)
            {
                GD.Print(i);
            }
            GD.Print();
            Buffer.BlockCopy(quadIndices, 0, FaceData, faceData.ID * 32, 32);
        }

        Buffer.BlockCopy(QuadInData, 0, VoxelData, 0, 528000);
        Buffer.BlockCopy(FaceData, 0, VoxelData, 528000, 80000);

        /*
        for (int j = 0; j < 12 * 132; j += 132)
        {
            for (int i = 0; i < 48; i += 4)
            {
                GD.Print($"{QuadInData[i + j]} {QuadInData[i + 1 + j]} {QuadInData[i + 2 + j]} {QuadInData[i + 3 + j]}");
            }
            GD.Print();
        }

        

        

        
                GD.Print($"{FaceData[64 + 12]} {FaceData[64 + 12 + 1]} {FaceData[64 + 12 + 2]} {FaceData[64 + 12 + 3]}");
                GD.Print($"{FaceData[64 + 16]} {FaceData[64 + 16 + 1]} {FaceData[64 + 16 + 2]} {FaceData[64 + 16 + 3]}");
                GD.Print($"{FaceData[64 + 20]} {FaceData[64 + 20 + 1]} {FaceData[64 + 20 + 2]} {FaceData[64 + 20 + 3]}");
                GD.Print($"{FaceData[64 + 24]} {FaceData[64 + 24 + 1]} {FaceData[64 + 24 + 2]} {FaceData[64 + 24 + 3]}");
                GD.Print($"{FaceData[64 + 28]} {FaceData[64 + 28 + 1]} {FaceData[64 + 28 + 2]} {FaceData[64 + 28 + 3]}");

                GD.Print($"{VoxelData[528000 + 64 + 8]} {VoxelData[528000 + 64 + 8 + 1]} {VoxelData[528000 + 64 + 8 + 2]} {VoxelData[528000 + 64 + 8 + 3]}");
                GD.Print($"{VoxelData[80000 + 132 * 1]} {VoxelData[80000 + 132 * 1 + 1]} {VoxelData[80000 + 132 * 1 + 2]} {VoxelData[80000 + 132 * 1 + 3]}");
                GD.Print($"{VoxelData[80000 + 132 * 1 + 4]} {VoxelData[80000 + 132 * 1 + 5]} {VoxelData[80000 + 132 * 1 + 6]} {VoxelData[80000 + 132 * 1 + 7]}");
                GD.Print($"{VoxelData[80000 + 132 * 1 + 8]} {VoxelData[80000 + 132 * 1 + 9]} {VoxelData[80000 + 132 * 1 + 10]} {VoxelData[80000 + 132 * 1 + 11]}");
                GD.Print($"{VoxelData[80000 + 132 * 1 + 12]} {VoxelData[80000 + 132 * 1 + 13]} {VoxelData[80000 + 132 * 1 + 14]} {VoxelData[80000 + 132 * 1 + 15]}");*/
    }

    public void GeneratePChunkMesh4(int[] Data, Chunk ch)
    {
        //RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast3.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);

        long ComputeList = rd.ComputeListBegin();
        //compute uniform
        byte[] inputBytes = new byte[Data.Length * sizeof(int)];
        Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = GameConstants.CHUNK_SIZE;
        int WorkGroupSide = 64;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 4194304;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate(sizeof(int) * 2);
        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);
        Rid VoxelDataBuffer = rd.StorageBufferCreate(528000 + 80000, VoxelData);

        Array<RDUniform> Uniforms = new Array<RDUniform>();

        //output quad uniform
        RDUniform QuadUniform = new RDUniform();
        Uniforms.Add(QuadUniform);
        QuadUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        QuadUniform.Binding = 0;
        QuadUniform.AddId(QuadBuffer);

        //output quad count uniform
        RDUniform QuadCountUniform = new RDUniform();
        Uniforms.Add(QuadCountUniform);
        QuadCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        QuadCountUniform.Binding = 1;
        QuadCountUniform.AddId(QuadCountBuffer);

        //chunk data input uniform
        RDUniform ChunkDataUniform = new RDUniform();
        Uniforms.Add(ChunkDataUniform);
        ChunkDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDataUniform.Binding = 2;
        ChunkDataUniform.AddId(ChunkDataBuffer);

        //chunk data input uniform
        RDUniform ChunkDimensionalUniform = new RDUniform();
        Uniforms.Add(ChunkDimensionalUniform);
        ChunkDimensionalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDimensionalUniform.Binding = 3;
        ChunkDimensionalUniform.AddId(ChunkDimensionalBuffer);

        //Voxel data input uniform
        RDUniform VoxelDataUniform = new RDUniform();
        Uniforms.Add(VoxelDataUniform);
        VoxelDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        VoxelDataUniform.Binding = 4;
        VoxelDataUniform.AddId(VoxelDataBuffer);

        Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        byte[] countBytes = rd.BufferGetData(QuadCountBuffer);
        int[] Count = new int[2];
        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);

        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0] * 64);

        rd.BufferClear(QuadBuffer, 0, (uint)Count[0] * 64);
        rd.BufferClear(QuadCountBuffer, 0, 8);

        //ch.PChunkByteIngestion(QBytes, countBytes);
        ch.PChunkByteAssignment(QBytes);

        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ShaderRID);
        rd.FreeRid(ChunkDimensionalBuffer);
        rd.FreeRid(VoxelDataBuffer);
        //rd.Free();

        return;
    }


}
