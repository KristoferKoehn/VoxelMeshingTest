using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class ChunkMeshManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private static ChunkMeshManager instance = null;

    public static float SyncTimeTotal = 0;

    public static float ProcessTimeTotal = 0;

    public static int ChunkCount = 0;
    public List<Chunk> ChunksToUpdate { get; set; } = new List<Chunk>();

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
	public override void _Process(double delta)
	{
        if (ChunksToUpdate.Count > 0)
        {
            foreach (Chunk chunk in ChunksToUpdate)
            {
                float[] meshData = GenerateMeshFastBytes(chunk.ChunkData, chunk.GlobalPosition);
                chunk.ProcessBytes(meshData);
            }

            ChunksToUpdate.Clear();
        }
    }

    public void RequestChunkMeshUpdate(Chunk chunk)
    {
        if (!ChunksToUpdate.Contains(chunk))
        {
            ChunksToUpdate.Add(chunk);
        }
    }

    public static float[] GenerateMeshFastBytes(int[] Data, Vector3 ChunkPosition)
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
        int WorkGroupSide = 2;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] {ChunkSize, WorkGroupSide}, 0, DimensionBytes, 0, DimensionBytes.Length);

        byte[] PlayerPositionBytes = new byte[sizeof(float) * 3];
        Vector3 pp = PlayerTrackingManager.GetPlayerLocation() - ChunkPosition;
        //Buffer.BlockCopy(new float[] {pp.X, pp.Y, pp.Z }, 0, PlayerPositionBytes, 0, sizeof(float) * 3);
        Buffer.BlockCopy(new float[] {0, 0, 0 }, 0, PlayerPositionBytes, 0, sizeof(float) * 3);

        uint BufferSize = 402653184;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate((uint)(sizeof(int) * WorkGroups));
        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);
        Rid PlayerPositionBuffer = rd.StorageBufferCreate(sizeof(float) * 3, PlayerPositionBytes);

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

        //chunk data input uniform
        RDUniform PlayerPositionUniform = new RDUniform();
        Uniforms.Add(PlayerPositionUniform);
        PlayerPositionUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        PlayerPositionUniform.Binding = 4;
        PlayerPositionUniform.AddId(PlayerPositionBuffer);

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();


        byte[] countBytes = rd.BufferGetData(QuadCountBuffer);

        uint[] Count = new uint[WorkGroups];

        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * Count.Length);

        int QuadCount = 0;
        for (int i = 0; i < Count.Length; i++)
        {
            QuadCount += (int)Count[i];
        }

        float[] data = new float[QuadCount * 16];

        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, BufferSize);

        int CumQuad = 0;
        for(int i = 0; i < WorkGroups; i++)
        {
            if (Count[i] != 0)
            {
                int WorkGroupPosition = (int)(BufferSize / WorkGroups);
                //byte[] QBytes = rd.BufferGetData(QuadBuffer, , Count[i] * 64);
                //GD.Print($"reading from: {(uint)(WorkGroupPosition * i)} through {(uint)(WorkGroupPosition * i) + Count[i] * 64} ");
                //GD.Print($"reading from: {Count[i]} ");
                //if everything gets put back to /64 , it's still broken. Maybe reading from the wrong addresses?

                Buffer.BlockCopy(QBytes, WorkGroupPosition * i, data, CumQuad * 64, (int)Count[i] * 64);
                //GD.Print($"writing to: {CumQuad * 64} through {CumQuad * 64 + (int)Count[i] * 64} ");
                CumQuad += (int)Count[i];
            }
        }

        /*
        byte[] QuadBytes = rd.BufferGetData(QuadBuffer, 0, Count[0]*64);
        */
        //Buffer.BlockCopy(QuadBytes, 0, data, 0, (int)Count[0] * 64);

        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ShaderRID);
        rd.FreeRid(ChunkDimensionalBuffer);
        rd.FreeRid(PlayerPositionBuffer);
        rd.Free();

        return data; 
    }




}
