using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public partial class ChunkMeshManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private static ChunkMeshManager instance = null;

    public static int ChunkCount = 0;
    public ConcurrentQueue<Chunk> ChunksToUpdate { get; set; } = new ConcurrentQueue<Chunk>();

    private ChunkMeshManager() { }

	public static ChunkMeshManager Instance()
	{
		if (instance == null)
		{
			instance = new ChunkMeshManager();
			SceneSwitcher.Instance().AddChild(instance);
			instance.Name = "ChunkMeshManager";
		}
		return instance;
	}


	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
    int count = 2;
	public override void _Process(double delta)
	{
        if (count % 4 == 0)
        {
            if (count >= Int32.MaxValue)
            {
                count = 0;
            }

            if (ChunksToUpdate.TryDequeue(out Chunk chunk))
            {
                Task.Run(() =>
                {
                    Chunk ch = chunk;
                    GeneratePChunkMesh2(chunk.ChunkData, chunk);
                });
            }
        }
        count++;
    }

    public void RequestChunkMeshUpdate(Chunk chunk)
    {
        ChunksToUpdate.Enqueue(chunk);
    }

    public void GeneratePChunkMesh(int[] Data, Vector3 ChunkPosition, Chunk ch)
    {
        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);
        Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

        long ComputeList = rd.ComputeListBegin();

        //compute uniform
        byte[] inputBytes = new byte[Data.Length * sizeof(int)];
        Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = 128;
        int WorkGroupSide = 64;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 402653184;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate((uint)(sizeof(int) * WorkGroups));
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

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        byte[] countBytes = rd.BufferGetData(QuadCountBuffer);
        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, BufferSize);

        uint[] Count = new uint[WorkGroups];
        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * WorkGroups);

        int QuadCount = 0;
        for (int i = 0; i < Count.Length; i++)
        {
            QuadCount += (int)Count[i];
        }

        byte[] CondensedQuads = new byte[QuadCount * 64];

        int CumQuad = 0;
        for (int i = 0; i < WorkGroups; i++)
        {
            if (Count[i] != 0)
            {
                int WorkGroupPosition = (int)(BufferSize / WorkGroups);
                Buffer.BlockCopy(QBytes, WorkGroupPosition * i, CondensedQuads, CumQuad * 64, (int)Count[i] * 64);
                CumQuad += (int)Count[i];
            }
        }

        ch.PChunkByteIngestion(CondensedQuads, countBytes);

        Task.Run(() => {
            rd.FreeRid(pipelineRID);
            rd.FreeRid(QuadBuffer);
            rd.FreeRid(ChunkDataBuffer);
            rd.FreeRid(QuadCountBuffer);
            rd.FreeRid(ShaderRID);
            rd.FreeRid(ChunkDimensionalBuffer);
            rd.Free();
        });


        return;
    }

    public void GeneratePChunkMesh2(int[] Data, Chunk ch)
    {
        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast2.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);
        Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

        long ComputeList = rd.ComputeListBegin();

        //compute uniform
        byte[] inputBytes = new byte[Data.Length * sizeof(int)];
        Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = 32;
        int WorkGroupSide = 32;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 402653184;

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

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        rd.ComputeListEnd();

        rd.Submit();

        //rd.Sync();
        Thread.Sleep(4);
        
        byte[] countBytes = rd.BufferGetData(QuadCountBuffer);
        int[] Count = new int[2];
        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);

        int loopcount = 0;
        while (Count[1] == 0)
        {
            countBytes = rd.BufferGetData(QuadCountBuffer);
            Count = new int[2];
            Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 2);
            loopcount++;
        }

        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0]*64);

        //ch.PChunkByteIngestion(QBytes, countBytes);
        ch.PChunkByteAssignment(QBytes);

        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ShaderRID);
        rd.FreeRid(ChunkDimensionalBuffer);
        rd.Free();

        return;
    }


}
