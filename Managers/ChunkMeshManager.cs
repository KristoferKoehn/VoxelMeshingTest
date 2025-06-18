using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VoxelMeshingTest.Classes;

public partial class ChunkMeshManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private static ChunkMeshManager instance = null;

    public static int ChunkCount = 0;
    public ConcurrentQueue<Chunk> ChunksToUpdate { get; set; } = new ConcurrentQueue<Chunk>();

    private ChunkMeshManager() { }

    private byte[] VoxelData;

    RenderingDevice rd { get; set; }
    Rid VoxelDataBuffer;
    RDUniform VoxelDataUniform;

    public Godot.Collections.Dictionary<FaceData, Array<QuadData>> FaceQuadResourceDictionary = new();

    public static ChunkMeshManager Instance()
	{
		if (instance == null)
		{
			instance = new ChunkMeshManager();
            instance.rd = RenderingServer.CreateLocalRenderingDevice();
			SceneSwitcher.Instance().AddChild(instance);
			instance.Name = "ChunkMeshManager";
            
        }
		return instance;
	}

	public override void _Ready()
	{

    }

    public override void _Process(double delta)
	{
        
    }

    public void RequestChunkMeshUpdate(Chunk chunk, bool expedite = false)
    {
        if (ChunksToUpdate.Contains(chunk))
        {
            return;
        }
        if (expedite) { 
            ChunksToUpdate.Prepend(chunk);
        }
        ChunksToUpdate.Enqueue(chunk);
    }

    public byte[] GetVoxelDataBytes()
    {
        FaceQuadResourceDictionary.Clear();
        //rewrite this into a system that generates structs that mirror glsl structs
        Array<FaceData> faces = new Array<FaceData>();

        byte[] QuadInData = new byte[256 * 4000];
        byte[] FaceData = new byte[32 * 4000];
        VoxelData = new byte[256 * 4000 + 32 * 4000];
        //get list of resources
        int QuadOffset = 0;

        string[] FaceFiles = DirAccess.GetFilesAt("res://VoxelData/FaceData/");
        foreach (string name in FaceFiles)
        {
            if (name.Contains(".tres"))
            {
                faces.Add(ResourceLoader.Load<FaceData>($"res://VoxelData/FaceData/{name}", cacheMode: ResourceLoader.CacheMode.Ignore));
            }
        }

        foreach (FaceData faceData in faces)
        {
            Array<Array<QuadData>> quads = faceData.GetFacesArray();
            Array<QuadData> blockquad = new Array<QuadData>();

            int[] quadIndices = new int[8];
            if (faceData.transparent)
            {
                quadIndices[6] = 1;
            }

            for (int i = 0; i < 6; i++)
            {
                quadIndices[i] = QuadOffset;
                for (int j = 0; j < quads[i].Count; j++)
                {
                    if (j != quads[i].Count - 1)
                    {
                        quads[i][j].NextFace = QuadOffset + 1;
                    }
                    blockquad.Add(quads[i][j]);
                    byte[] b = quads[i][j].Serialize();
                    Buffer.BlockCopy(b, 0, QuadInData, QuadOffset * 256, 132);
                    QuadOffset += 1;
                }
            }

            Buffer.BlockCopy(quadIndices, 0, FaceData, faceData.ID * 32, 32);
            FaceQuadResourceDictionary[faceData] = blockquad;
        }

        GD.Print($"total quads on initialization: {QuadOffset}");

        Buffer.BlockCopy(QuadInData, 0, VoxelData, 0, 256 * 4000);
        Buffer.BlockCopy(FaceData, 0, VoxelData, 256 * 4000, 32 * 4000);

        return VoxelData;

    }

    public Chunk GenerateChunkMesh(Chunk ch, ChunkSpawnManager.RenderDeviceFrame RDFrame)
    {
        Stopwatch sw = Stopwatch.StartNew();
        long ComputeList = RDFrame.MesherRenderDevice.ComputeListBegin();
        //compute uniform
        byte[] inputBytes = new byte[ch.ChunkData.Length * sizeof(int)];
        Buffer.BlockCopy(ch.ChunkData, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = GameConstants.CHUNK_SIZE;
        int WorkGroupSide = GameConstants.WORKGROUPS;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        //uint BufferSection = 786432 * 4;

        Rid QuadCountBuffer = RDFrame.MesherRenderDevice.StorageBufferCreate(sizeof(int) * 4);
        Rid ChunkDataBuffer = RDFrame.MesherRenderDevice.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = RDFrame.MesherRenderDevice.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);
        //output quad count uniform
        RDUniform QuadCountUniform = new RDUniform();
        QuadCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        QuadCountUniform.Binding = 1;
        QuadCountUniform.AddId(QuadCountBuffer);

        //chunk data input uniform
        RDUniform ChunkDataUniform = new RDUniform();
        ChunkDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDataUniform.Binding = 2;
        ChunkDataUniform.AddId(ChunkDataBuffer);

        //chunk data input uniform
        RDUniform ChunkDimensionalUniform = new RDUniform();
        ChunkDimensionalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDimensionalUniform.Binding = 3;
        ChunkDimensionalUniform.AddId(ChunkDimensionalBuffer);

        Array<RDUniform> Uniforms = new()
        {
            RDFrame.VoxelDataUniform,
            RDFrame.QuadUniform,
            RDFrame.GreedyUniform,
            ChunkDimensionalUniform,
            ChunkDataUniform,
            QuadCountUniform,
            RDFrame.CompressorUniform,
        };

        float Setupsw = sw.ElapsedMilliseconds;

        Rid pipelineRID = RDFrame.MesherRenderDevice.ComputePipelineCreate(RDFrame.MesherShaderRID);
        Rid UniformSet = RDFrame.MesherRenderDevice.UniformSetCreate(Uniforms, RDFrame.MesherShaderRID, 0);
        RDFrame.MesherRenderDevice.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
        RDFrame.MesherRenderDevice.ComputeListBindComputePipeline(ComputeList, pipelineRID);
        RDFrame.MesherRenderDevice.ComputeListDispatch(ComputeList, (uint)WorkGroupSide, (uint)WorkGroupSide, (uint)WorkGroupSide);
        RDFrame.MesherRenderDevice.ComputeListEnd();
        /*
        RDFrame.MesherRenderDevice.Submit();
        RDFrame.MesherRenderDevice.Sync(); */

        float initmeshingsw = sw.ElapsedMilliseconds;

        int[] Count = new int[4];

        long GreedyComputeList = RDFrame.MesherRenderDevice.ComputeListBegin();
        Rid GreedyPipelineRID = RDFrame.MesherRenderDevice.ComputePipelineCreate(RDFrame.GreedyShaderRID);
        Rid GreedyUniformSet = RDFrame.MesherRenderDevice.UniformSetCreate(Uniforms, RDFrame.GreedyShaderRID, 0);
        RDFrame.MesherRenderDevice.ComputeListBindUniformSet(GreedyComputeList, GreedyUniformSet, 0);
        RDFrame.MesherRenderDevice.ComputeListBindComputePipeline(GreedyComputeList, GreedyPipelineRID);
        RDFrame.MesherRenderDevice.ComputeListDispatch(GreedyComputeList, 64 * 6, 1, 1);
        RDFrame.MesherRenderDevice.ComputeListEnd();
        /*
        RDFrame.MesherRenderDevice.Submit();
        RDFrame.MesherRenderDevice.Sync();*/

        float greedysw = sw.ElapsedMilliseconds;

        long CompressorComputeList = RDFrame.MesherRenderDevice.ComputeListBegin();
        Rid CompressorPipelineRID = RDFrame.MesherRenderDevice.ComputePipelineCreate(RDFrame.CompressorShaderRID);
        Rid CompressorUniformSet = RDFrame.MesherRenderDevice.UniformSetCreate(Uniforms, RDFrame.CompressorShaderRID, 0);
        RDFrame.MesherRenderDevice.ComputeListBindUniformSet(CompressorComputeList, CompressorUniformSet, 0);
        RDFrame.MesherRenderDevice.ComputeListBindComputePipeline(CompressorComputeList, CompressorPipelineRID);
        RDFrame.MesherRenderDevice.ComputeListDispatch(CompressorComputeList, 32, 32, 32);
        RDFrame.MesherRenderDevice.ComputeListEnd();
        /*
        RDFrame.MesherRenderDevice.Submit();
        RDFrame.MesherRenderDevice.Sync();*/

        float compressorTime = sw.ElapsedMilliseconds;

        byte[] countBytes = RDFrame.MesherRenderDevice.BufferGetData(QuadCountBuffer);
        Buffer.BlockCopy(countBytes, 0, Count, 0, sizeof(uint) * 4);
        
        if (Count[0] == 0)
        {
            RDFrame.MesherRenderDevice.FreeRid(UniformSet);
            RDFrame.MesherRenderDevice.FreeRid(CompressorUniformSet);
            RDFrame.MesherRenderDevice.FreeRid(CompressorPipelineRID);
            RDFrame.MesherRenderDevice.FreeRid(pipelineRID);
            RDFrame.MesherRenderDevice.FreeRid(QuadCountBuffer);
            RDFrame.MesherRenderDevice.FreeRid(ChunkDataBuffer);
            RDFrame.MesherRenderDevice.FreeRid(ChunkDimensionalBuffer);
            return null;
        }

        byte[] DataBytes = RDFrame.MesherRenderDevice.BufferGetData(RDFrame.CompressorBuffer, 0, (uint)Count[0] * (12 + 12 + 16 + 8 + 6 + 18) * 4);
        
        int vertices_offset = 0; 
        int normals_offset = vertices_offset + 12 * Count[0] * 4;
        int colors_offset = normals_offset + Count[0] * 12 * 4;
        int uv_offset = colors_offset + Count[0] * 16 * 4;
        int collision_offset = uv_offset + Count[0] * 8 * 4;

        byte[] VBuffer = new byte[Count[0] * 12 * 4];
        byte[] NBuffer = new byte[Count[0] * 12 * 4];
        byte[] CBuffer = new byte[Count[0] * 16 * 4];
        byte[] ColBuffer = new byte[Count[0] * 18 * 4]; 

        Buffer.BlockCopy(DataBytes, 0, VBuffer, 0, VBuffer.Length);
        Buffer.BlockCopy(DataBytes, normals_offset, NBuffer, 0, NBuffer.Length);
        Buffer.BlockCopy(DataBytes, colors_offset, CBuffer, 0, CBuffer.Length);
        Buffer.BlockCopy(DataBytes, collision_offset, ColBuffer, 0, ColBuffer.Length);

        float datapullingsw = sw.ElapsedMilliseconds;

        Godot.Collections.Array ar = new Godot.Collections.Array();
        ar.Resize((int)Mesh.ArrayType.Max);

        byte[] Colbytes = new byte[8 + (uint)Count[0] * 72];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedVector3Array, Count[0] * 6 }, 0, Colbytes, 0, 8);
        Buffer.BlockCopy(ColBuffer, 0, Colbytes, 8, Count[0] * 72);

        Vector3[] Collision = (Vector3[])GD.BytesToVar(Colbytes);
        Vector3[] Vertices = BytesToVec3(VBuffer, Count[0]);
        Vector3[] Normals = BytesToVec3(NBuffer, Count[0]);
        //Vector2[] UVs = BytesToVec2(UVBuffer, Count[0]);
        Color[] Colors = BytesToColor(CBuffer, Count[0]);

        float dataconversionsw = sw.ElapsedMilliseconds;

        //GD.Print($"initial meshing: {initmeshingsw}, greedy: {greedysw}, data compression: {compressorTime}, data pulling: {datapullingsw}, data conversion: {dataconversionsw}"); 
        
        int[] Indices = new int[Count[0] * 6];
        Buffer.BlockCopy(RDFrame.ints, 0, Indices, 0, Indices.Length * 4);

        ar[(int)Mesh.ArrayType.Vertex] = Vertices;
        ar[(int)Mesh.ArrayType.Normal] = Normals;
        //ar[(int)Mesh.ArrayType.TexUV] = UVs;
        ar[(int)Mesh.ArrayType.Index] = Indices;
        ar[(int)Mesh.ArrayType.Color] = Colors;

        RDFrame.MesherRenderDevice.BufferClear(QuadCountBuffer, 0, 16);
        RDFrame.MesherRenderDevice.BufferClear(RDFrame.GreedyBuffer, 0, 6291456 + GameConstants.BUFFER_SIZE);

        if (ch.MeshInstance == null)
        {
            ch.MeshInstance = new MeshInstance3D();
            ch.CallDeferred("add_child", ch.MeshInstance);
        }

        ch.ConcavePolygon.CallDeferred("set_faces", Collision);

        ch.MeshData = ar;

        RDFrame.MesherRenderDevice.FreeRid(UniformSet);
        RDFrame.MesherRenderDevice.FreeRid(CompressorUniformSet);
        RDFrame.MesherRenderDevice.FreeRid(CompressorPipelineRID);
        RDFrame.MesherRenderDevice.FreeRid(pipelineRID);
        RDFrame.MesherRenderDevice.FreeRid(QuadCountBuffer);
        RDFrame.MesherRenderDevice.FreeRid(ChunkDataBuffer);
        RDFrame.MesherRenderDevice.FreeRid(ChunkDimensionalBuffer);

        return ch;
    }

    public Vector3[] BytesToVec3(byte[] buffer, int count)
    {
        byte[] Vbytes = new byte[8 + (uint)count * 48];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedVector3Array, count * 4 }, 0, Vbytes, 0, 8);
        Buffer.BlockCopy(buffer, 0, Vbytes, 8, count * 48);
        return (Vector3[])GD.BytesToVar(Vbytes);
    }

    public Vector2[] BytesToVec2(byte[] buffer, int count)
    {
        byte[] Vbytes = new byte[8 + (uint)count * 32];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedVector2Array, count * 4 }, 0, Vbytes, 0, 8);
        Buffer.BlockCopy(buffer, 0, Vbytes, 8, count * 32);

        return (Vector2[])GD.BytesToVar(Vbytes);
    }

    public Color[] BytesToColor(byte[] buffer, int count)
    {
        byte[] Vbytes = new byte[8 + (uint)count * 64];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedColorArray, count * 4 }, 0, Vbytes, 0, 8);
        Buffer.BlockCopy(buffer, 0, Vbytes, 8, count * 64);

        return (Color[])GD.BytesToVar(Vbytes);
    }
}
