using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Xml.Linq;
using VoxelMeshingTest.Classes;

public partial class ChunkGeneratorManager : Node
{

	private static ChunkGeneratorManager instance = null;

	private ChunkGeneratorManager() { }

    public static ChunkGeneratorManager Instance()
	{
		if (instance == null)
		{
			instance = new ChunkGeneratorManager();
			SceneSwitcher.root.AddChild(instance);
			instance.Name = "ChunkGeneratorManager";
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

	}

    public bool ComputeGenerateChunk(Vector3I pos, Chunk ch, ChunkSpawnManager.RenderDeviceFrame rdFrame)
    {
        int[,,] chunk;
      
        long ComputeList = rdFrame.GeneratorRenderDevice.ComputeListBegin();

        Rid ChunkBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate((uint)(GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE) * 4 + 8);

        byte[] DimensionBytes = new byte[32];
        Buffer.BlockCopy(new float[] { GameConstants.CHUNK_DATA_SIZE, 33, 0, 0, pos.X, pos.Y, pos.Z, 0 }, 0, DimensionBytes, 0, 32);

        Rid ChunkDimensionalBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate((uint)DimensionBytes.Length, DimensionBytes);

        RDUniform ChunkUniform = new RDUniform();
        ChunkUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkUniform.Binding = 2;
        ChunkUniform.AddId(ChunkBuffer);

        //chunk data input uniform
        RDUniform ChunkDimensionalUniform = new RDUniform();
        ChunkDimensionalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDimensionalUniform.Binding = 3;
        ChunkDimensionalUniform.AddId(ChunkDimensionalBuffer);

        Array<RDUniform> Uniforms = new Array<RDUniform>() {
            ChunkUniform,
            ChunkDimensionalUniform
        };

        Rid pipelineRID = rdFrame.GeneratorRenderDevice.ComputePipelineCreate(rdFrame.TerrainShaderRID);

        Rid UniformSet = rdFrame.GeneratorRenderDevice.UniformSetCreate(Uniforms, rdFrame.TerrainShaderRID, 0);

        rdFrame.GeneratorRenderDevice.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rdFrame.GeneratorRenderDevice.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rdFrame.GeneratorRenderDevice.ComputeListDispatch(ComputeList, 33, 33, 33);

        rdFrame.GeneratorRenderDevice.ComputeListEnd();
        rdFrame.GeneratorRenderDevice.Submit();
        rdFrame.GeneratorRenderDevice.Sync();

        byte[] chunkData = rdFrame.GeneratorRenderDevice.BufferGetData(ChunkBuffer); 

        rdFrame.GeneratorRenderDevice.BufferClear(ChunkBuffer, 0, (uint)(GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE) * 4 + 8);

        chunk = new int[66, 66, 66];

        int[] count = { 0, 0 };
        Buffer.BlockCopy(chunkData, chunkData.Length - 8, count, 0 , 8);

        Buffer.BlockCopy(chunkData, 0, chunk, 0, chunkData.Length - 8);

        int blockCount = 0;
        foreach (int block in chunk)
        {
            if (block == 0)
                blockCount++;
        }

        //GD.Print($"getting this many zeros {blockCount} out of {66* 66* 66} blocks");
        rdFrame.GeneratorRenderDevice.FreeRid(UniformSet);
        rdFrame.GeneratorRenderDevice.FreeRid(pipelineRID);
        rdFrame.GeneratorRenderDevice.FreeRid(ChunkBuffer);
        rdFrame.GeneratorRenderDevice.FreeRid(ChunkDimensionalBuffer);

        ch.ChunkData = chunk;
        if (count[0] == 0 || count[1] == 66 * 66 * 66)
        {
            return false;
        }

        return true;
    }
}
