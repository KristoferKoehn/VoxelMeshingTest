using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkGeneratorManager : Node
{

	private static ChunkGeneratorManager instance = null;

	private ChunkGeneratorManager() { }

	public static FastNoiseLite Terrain1 {  get; set; }
	public static FastNoiseLite Terrain2 {  get; set; }
	public static FastNoiseLite Terrain3 {  get; set; }
    public static FastNoiseLite SurfaceCutoff {  get; set; }
    public static FastNoiseLite BiomeTemp { get; set; }
    public static FastNoiseLite BiomeQual { get; set; }
    //RenderingDevice rd { get; set; }
    public System.Collections.Generic.Dictionary<Vector3I, int[,,]> GeneratedChunks { get; set; } = new System.Collections.Generic.Dictionary<Vector3I, int[,,]>();
	bool Generating = false;

    ConcurrentQueue<RenderingDevice> ConcurrentRenderDevices { get; set; } = new();

    private object GeneratedChunksLock = new object();

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
        //instance.rd = RenderingServer.CreateLocalRenderingDevice();
        ConcurrentRenderDevices.Enqueue(RenderingServer.CreateLocalRenderingDevice());
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public int[,,] ComputeGenerateChunk(Vector3I pos)
	{
        
        int[,,] chunk;
        
        GeneratedChunks.TryGetValue(pos, out chunk);
        if (chunk != null)
        {
            return chunk;
        }
        

        RenderingDevice rd = null;
        do {
            ConcurrentRenderDevices.TryDequeue(out rd);
        } while (rd == null);

        Vector3I pos2D = new Vector3I(pos.X, pos.Z, 0);

        SurfaceCutoff.Offset = pos2D * GameConstants.CHUNK_SIZE;
        BiomeTemp.Offset = pos2D * GameConstants.CHUNK_SIZE;
        BiomeQual.Offset = pos2D * GameConstants.CHUNK_SIZE;

        Terrain1.Offset = pos * GameConstants.CHUNK_SIZE;
        Terrain2.Offset = pos * GameConstants.CHUNK_SIZE;
        Terrain3.Offset = pos * GameConstants.CHUNK_SIZE;

        uint vec4_size = 4;

        uint ImageByteSize = (uint)(vec4_size * (GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE));
        uint CutoffLayersSize = ImageByteSize * 3;
        uint TerrainSize = (uint)(CutoffLayersSize * GameConstants.CHUNK_DATA_SIZE);
		
        byte[] NoiseData = new byte[TerrainSize];
        Array<Image> Terrain1Noise = Terrain1.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);
        Array<Image> Terrain2Noise = Terrain2.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);
        Array<Image> Terrain3Noise = Terrain3.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);

        int i = 0;
        foreach (Image im in Terrain1Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }
        
        foreach (Image im in Terrain2Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }
  
        foreach (Image im in Terrain3Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }

        byte[] cutoffbytes = new byte[CutoffLayersSize];

        Image cutoff = SurfaceCutoff.GetImage(66, 66, normalize: false);
        Image temperature = BiomeTemp.GetImage(66, 66, normalize: false);
        Image quality = BiomeQual.GetImage(66, 66, normalize: false);

        cutoff.Convert(Image.Format.Rf);
        temperature.Convert(Image.Format.Rf);
        quality.Convert(Image.Format.Rf);
        
        Buffer.BlockCopy(cutoff.GetData(), 0, cutoffbytes, 0, 66 * 66 * 4);
        Buffer.BlockCopy(temperature.GetData(), 0, cutoffbytes, 66 * 66 * 4, 66 * 66 * 4);
        Buffer.BlockCopy(quality.GetData(), 0, cutoffbytes, 66 * 66 * 4 * 2, 66 * 66 * 4);


        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkGen.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);
        long ComputeList = rd.ComputeListBegin();

        Rid CutoffBuffer = rd.StorageBufferCreate(CutoffLayersSize, cutoffbytes);
		Rid GenBuffer = rd.StorageBufferCreate(TerrainSize, NoiseData);
        Rid ChunkBuffer = rd.StorageBufferCreate((uint)(GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE) * 4);

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { GameConstants.CHUNK_DATA_SIZE, 66 }, 0, DimensionBytes, 0, DimensionBytes.Length);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);

        RDUniform CutoffUniform = new RDUniform();
        CutoffUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        CutoffUniform.Binding = 0;
        CutoffUniform.AddId(CutoffBuffer);

        RDUniform GenUniform = new RDUniform();
        GenUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        GenUniform.Binding = 1;
        GenUniform.AddId(GenBuffer);

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
            CutoffUniform,
            GenUniform,
            ChunkUniform,
            ChunkDimensionalUniform
        };


        Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

        rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rd.ComputeListDispatch(ComputeList, 66, 66, 66);

        rd.ComputeListEnd();
        rd.Submit();
        rd.Sync();
		
		byte[] chunkData = rd.BufferGetData(ChunkBuffer);

        ConcurrentRenderDevices.Enqueue(rd);
        
        chunk = new int[66, 66, 66];

		Buffer.BlockCopy(chunkData, 0, chunk, 0, chunkData.Length);

        GeneratedChunks[pos] = chunk;

        rd.FreeRid(UniformSet);
		rd.FreeRid(pipelineRID);
		rd.FreeRid(ChunkBuffer);
        rd.FreeRid(CutoffBuffer);
        rd.FreeRid(GenBuffer);
		rd.FreeRid(ChunkDimensionalBuffer);
        rd.FreeRid(ShaderRID);
        return chunk;
	}

    public int[,,] ComputeGenerateChunk2(Vector3I pos, ChunkSpawnManager.RenderDeviceFrame rdFrame)
    {
        int[,,] chunk;
        /*
        lock (GeneratedChunksLock)
        {
            GeneratedChunks.TryGetValue(pos, out chunk);
        }
        if (chunk != null)
        {
            GD.Print($"returning chunk at {pos}");
            return chunk;
        } */

        Vector3I pos2D = new Vector3I(pos.X, pos.Z, 0);

        SurfaceCutoff.Offset = pos2D * GameConstants.CHUNK_SIZE;
        BiomeTemp.Offset = pos2D * GameConstants.CHUNK_SIZE;
        BiomeQual.Offset = pos2D * GameConstants.CHUNK_SIZE;

        Terrain1.Offset = pos * GameConstants.CHUNK_SIZE;
        Terrain2.Offset = pos * GameConstants.CHUNK_SIZE;
        Terrain3.Offset = pos * GameConstants.CHUNK_SIZE;

        uint vec4_size = 4;

        uint ImageByteSize = (uint)(vec4_size * (GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE));
        uint CutoffLayersSize = ImageByteSize * 3;
        uint TerrainSize = (uint)(CutoffLayersSize * GameConstants.CHUNK_DATA_SIZE);

        byte[] NoiseData = new byte[TerrainSize];
        Array<Image> Terrain1Noise = Terrain1.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);
        Array<Image> Terrain2Noise = Terrain2.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);
        Array<Image> Terrain3Noise = Terrain3.GetImage3D(GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, GameConstants.CHUNK_DATA_SIZE, normalize: false);

        int i = 0;
        foreach (Image im in Terrain1Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }

        foreach (Image im in Terrain2Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }

        foreach (Image im in Terrain3Noise)
        {
            im.Convert(Image.Format.Rf);
            byte[] data = im.GetData();
            Buffer.BlockCopy(data, 0, NoiseData, (data.Length * i), data.Length);
            i++;
        }

        byte[] cutoffbytes = new byte[CutoffLayersSize];

        Image cutoff = SurfaceCutoff.GetImage(66, 66, normalize: false);
        Image temperature = BiomeTemp.GetImage(66, 66, normalize: false);
        Image quality = BiomeQual.GetImage(66, 66, normalize: false);

        cutoff.Convert(Image.Format.Rf);
        temperature.Convert(Image.Format.Rf);
        quality.Convert(Image.Format.Rf);

        Buffer.BlockCopy(cutoff.GetData(), 0, cutoffbytes, 0, 66 * 66 * 4);
        Buffer.BlockCopy(temperature.GetData(), 0, cutoffbytes, 66 * 66 * 4, 66 * 66 * 4);
        Buffer.BlockCopy(quality.GetData(), 0, cutoffbytes, 66 * 66 * 4 * 2, 66 * 66 * 4);

        long ComputeList = rdFrame.GeneratorRenderDevice.ComputeListBegin();

        Rid CutoffBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate(CutoffLayersSize, cutoffbytes);
        Rid GenBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate(TerrainSize, NoiseData);
        Rid ChunkBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate((uint)(GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE) * 4);

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { GameConstants.CHUNK_DATA_SIZE, 66 }, 0, DimensionBytes, 0, DimensionBytes.Length);
        Rid ChunkDimensionalBuffer = rdFrame.GeneratorRenderDevice.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);

        RDUniform CutoffUniform = new RDUniform();
        CutoffUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        CutoffUniform.Binding = 0;
        CutoffUniform.AddId(CutoffBuffer);

        RDUniform GenUniform = new RDUniform();
        GenUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        GenUniform.Binding = 1;
        GenUniform.AddId(GenBuffer);

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
            CutoffUniform,
            GenUniform,
            ChunkUniform,
            ChunkDimensionalUniform
        };


        Rid pipelineRID = rdFrame.GeneratorRenderDevice.ComputePipelineCreate(rdFrame.TerrainShaderRID);

        Rid UniformSet = rdFrame.GeneratorRenderDevice.UniformSetCreate(Uniforms, rdFrame.TerrainShaderRID, 0);

        rdFrame.GeneratorRenderDevice.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        rdFrame.GeneratorRenderDevice.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        rdFrame.GeneratorRenderDevice.ComputeListDispatch(ComputeList, 66, 66, 66);

        rdFrame.GeneratorRenderDevice.ComputeListEnd();
        rdFrame.GeneratorRenderDevice.Submit();
        rdFrame.GeneratorRenderDevice.Sync();

        byte[] chunkData = rdFrame.GeneratorRenderDevice.BufferGetData(ChunkBuffer);

        rdFrame.GeneratorRenderDevice.BufferClear(GenBuffer, 0, TerrainSize);
        rdFrame.GeneratorRenderDevice.BufferClear(CutoffBuffer, 0, CutoffLayersSize);
        rdFrame.GeneratorRenderDevice.BufferClear(ChunkBuffer, 0, (uint)(GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE) * 4);

        chunk = new int[66, 66, 66];

        Buffer.BlockCopy(chunkData, 0, chunk, 0, chunkData.Length);
        /*
        lock (GeneratedChunksLock)
        {
            GeneratedChunks[pos] = chunk;
        }*/
        rdFrame.GeneratorRenderDevice.FreeRid(UniformSet);
        rdFrame.GeneratorRenderDevice.FreeRid(pipelineRID);
        rdFrame.GeneratorRenderDevice.FreeRid(ChunkBuffer);
        rdFrame.GeneratorRenderDevice.FreeRid(CutoffBuffer);
        rdFrame.GeneratorRenderDevice.FreeRid(GenBuffer);
        rdFrame.GeneratorRenderDevice.FreeRid(ChunkDimensionalBuffer);
        return chunk;
    }
}
