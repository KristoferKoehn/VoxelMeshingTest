using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
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
        //rd = RenderingServer.CreateLocalRenderingDevice();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
    int count = 2;
	public override void _Process(double delta)
	{

        if (ChunksToUpdate.Count != 0)
        {
            if (count >= Int32.MaxValue)
            {
                count = 0;
            }

            if (ChunksToUpdate.TryDequeue(out Chunk chunk))
            {
                Chunk ch = chunk;
                GeneratePChunkMesh2(chunk.ChunkData, chunk);
            }
        }
        count++;
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


}
