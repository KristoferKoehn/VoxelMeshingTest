using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    private byte[] VoxelData;
    RenderingDevice rd { get; set; }
    Rid VoxelDataBuffer;

    public Godot.Collections.Dictionary<FaceData, Array<QuadData>> FaceQuadResourceDictionary = new();

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
        HandleChunkMeshing();
    }

    /* 
     async void HandleChunkMeshing()
     {
         await Task.Run(() =>
         {
             while (true)
             {
                 if (ChunksToUpdate.TryDequeue(out Chunk chunk))
                 {
                     if (chunk != null)
                     {
                         Chunk ch = chunk;
                         GeneratePChunkMesh4(chunk.ChunkData, chunk);
                         Thread.Sleep(15);
                     }
                 }
             }
         });
     }
    
      * Loop through list,  
      * if not visible, throw out
      * if not within sight, don't use unless no in-sight chunks need updating
      * if there are more in-sight chunks grab the closest one to the player
      * update one, then put everything back.*/
     async void HandleChunkMeshing()
     {
         await Task.Run(() =>
         {
             while (true)
             {
                 List<Chunk> chunks = new List<Chunk>();
                 while (ChunksToUpdate.TryDequeue(out Chunk chunk))
                 {
                     if (chunk.Visibility)
                     {
                         chunks.Add(chunk);
                     }
                 }
                 if (chunks.Count == 0) { continue; }
                 Chunk ChunkToUpdate = null;
                 for (int i = 0; i < chunks.Count; i++)
                 {
                     Basis pBasis = PlayerTrackingManager.Instance().GetPlayerBasis();

                     Vector3 distance = chunks[i].ChunkPosition - PlayerTrackingManager.Instance().GetPlayerLocation() + pBasis * new Vector3(0, 0, -30);

                     Vector3 FacingAngle = pBasis * new Vector3(0, 0, 1); /// hopefully this makes sense. rotate a south ray to camera
                     if (distance.Dot(FacingAngle) < -0.5) {
                         if (ChunkToUpdate == null)
                         {
                             ChunkToUpdate = chunks[i];
                         }
                         else if (distance.Length() < (ChunkToUpdate.ChunkPosition - PlayerTrackingManager.Instance().GetPlayerLocation()).Length())
                         {
                             ChunkToUpdate = chunks[i];
                         }
                     }
                 }

                 if (ChunkToUpdate != null) {
                     chunks.Remove(ChunkToUpdate);
                     GeneratePChunkMesh5(ChunkToUpdate.ChunkData, ChunkToUpdate);
                     Thread.Sleep(10);
                 }

                 foreach (Chunk chunk in chunks)
                 {
                     ChunksToUpdate.Enqueue(chunk);
                 }

             }
         });
     }

    public override void _Process(double delta)
	{
        /*
        if (ChunksToUpdate.Count > 0)
        {
            if (ChunksToUpdate.TryDequeue(out Chunk chunk))
            {
                if (chunk != null)
                {
                    Chunk ch = chunk;
                    GeneratePChunkMesh4(chunk.ChunkData, chunk);
                }
            }
        } 
        */
    }

    public void RequestChunkMeshUpdate(Chunk chunk, bool expidite = false)
    {
        if (ChunksToUpdate.Contains(chunk))
        {
            return;
        }
        if (expidite) { 
            ChunksToUpdate.Prepend(chunk);
        }
        ChunksToUpdate.Enqueue(chunk);
    }

    public void GeneratePChunkMesh3(int[] Data, Chunk chunk)
    {
        ShaderWrapper.AssignMesh(Data, chunk);
    }

    public void InitializeVoxelData()
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
                GD.Print($"adding {name}");
            }
        }

        foreach (FaceData faceData in faces)
        {
            //get QuadInData Offset, 
            
            Array<Array<QuadData>> quads = faceData.GetFacesArray();
            Array<QuadData> blockquad = new Array<QuadData>();

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
                    blockquad.Add(quads[i][j]);
                    byte[] b = quads[i][j].Serialize();
                    Buffer.BlockCopy(b, 0, QuadInData, QuadOffset * 256, 132);
                    QuadOffset += 1;
                }
            }

            Buffer.BlockCopy(quadIndices, 0, FaceData, faceData.ID * 32, 32);
            FaceQuadResourceDictionary[faceData] = blockquad;
        }

        Buffer.BlockCopy(QuadInData, 0, VoxelData, 0, 256 * 4000);
        Buffer.BlockCopy(FaceData, 0, VoxelData, 256 * 4000, 32 * 4000);

    }

    public void GeneratePChunkMesh4(int[,,] Data, Chunk ch)
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
        int WorkGroupSide = GameConstants.WORKGROUPS;
        int WorkGroups = WorkGroupSide * WorkGroupSide * WorkGroupSide;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 4194304 * 16;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate(sizeof(int) * 2);
        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);
        Rid VoxelDataBuffer = rd.StorageBufferCreate(256 * 4000 + 32 * 4000, VoxelData);
        Rid GreedyStorageBuffer = rd.StorageBufferCreate((uint)(128 * Math.Pow(GameConstants.CHUNK_SIZE, 3) * 6));
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

        RDUniform GreedyStorageUniform = new RDUniform();
        Uniforms.Add(GreedyStorageUniform);
        GreedyStorageUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        GreedyStorageUniform.Binding = 5;
        GreedyStorageUniform.AddId(GreedyStorageBuffer);

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

        byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0] * 128);

        rd.BufferClear(QuadBuffer, 0, (uint)Count[0] * 128);
        rd.BufferClear(QuadCountBuffer, 0, 8);
        rd.BufferClear(GreedyStorageBuffer, 0, (uint)(128 * Math.Pow(GameConstants.CHUNK_SIZE, 3) * 6));
        
        ch.PChunkByteAssignment(QBytes);

        rd.FreeRid(UniformSet);
        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(ChunkDimensionalBuffer);
        rd.FreeRid(VoxelDataBuffer);
        rd.FreeRid(GreedyStorageBuffer);
        rd.FreeRid(ShaderRID);
        //rd.Free();

        return;
    }

    public void GeneratePChunkMesh5(int[,,] Data, Chunk ch)
    {
        //RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesherFast4.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);

        long ComputeList = rd.ComputeListBegin();
        //compute uniform
        byte[] inputBytes = new byte[Data.Length * sizeof(int)];
        Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        int ChunkSize = GameConstants.CHUNK_SIZE;
        int WorkGroupSide = GameConstants.WORKGROUPS;

        byte[] DimensionBytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(new int[] { ChunkSize, WorkGroupSide }, 0, DimensionBytes, 0, DimensionBytes.Length);

        uint BufferSize = 786432 * 8 * 4;
        uint BufferSection = 786432 * 4;

        Rid QuadBuffer = rd.StorageBufferCreate(BufferSize);
        Rid QuadCountBuffer = rd.StorageBufferCreate(sizeof(int) * 2);
        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
        Rid ChunkDimensionalBuffer = rd.StorageBufferCreate(sizeof(int) * 2, DimensionBytes);
        Rid VoxelDataBuffer = rd.StorageBufferCreate(256 * 4000 + 32 * 4000, VoxelData);
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

        //byte[] QBytes = rd.BufferGetData(QuadBuffer, 0, (uint)Count[0] * 128);
        byte[] VBuffer = rd.BufferGetData(QuadBuffer, BufferSection * 0, (uint)Count[0] * 48);
        byte[] NBuffer = rd.BufferGetData(QuadBuffer, BufferSection * 1, (uint)Count[0] * 48);
        byte[] ColBuffer = rd.BufferGetData(QuadBuffer, BufferSection * 5, (uint)Count[0] * 72);
        byte[] IBuffer = rd.BufferGetData(QuadBuffer, BufferSection * 7, (uint)Count[0] * (4 * 6));
        
        Godot.Collections.Array ar = new Godot.Collections.Array();
        ar.Resize((int)Mesh.ArrayType.Max);

        byte[] Vbytes = new byte[8 + (uint)Count[0] * 48];
        Buffer.BlockCopy(new int[]{ (int)Variant.Type.PackedVector3Array, Count[0] * 4 }, 0, Vbytes, 0, 8);
        Buffer.BlockCopy(VBuffer, 0, Vbytes, 8, Count[0] * 48);

        byte[] Nbytes = new byte[8 + (uint)Count[0] * 48];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedVector3Array, Count[0] * 4 }, 0, Nbytes, 0, 8);
        Buffer.BlockCopy(NBuffer, 0, Nbytes, 8, Count[0] * 48);

        byte[] Colbytes = new byte[8 + (uint)Count[0] * 72];
        Buffer.BlockCopy(new int[] { (int)Variant.Type.PackedVector3Array, Count[0] * 6 }, 0, Colbytes, 0, 8);
        Buffer.BlockCopy(ColBuffer, 0, Colbytes, 8, Count[0] * 72);


        Vector3[] Vertices = (Vector3[])GD.BytesToVar(Vbytes);
        Vector3[] Normals = (Vector3[])GD.BytesToVar(Nbytes);
        Vector3[] Collision = (Vector3[])GD.BytesToVar(Colbytes);
        int[] Indices = new int[Count[0] * 6];
        Buffer.BlockCopy(IBuffer, 0, Indices, 0, IBuffer.Length);
        //Buffer.BlockCopy(NBytes, 0, Normals, 0, NBytes.Length);
        //Buffer.BlockCopy(IndBytes, 0, Indices, 0, IndBytes.Length);

        ar[(int)Mesh.ArrayType.Vertex] = Vertices;
        ar[(int)Mesh.ArrayType.Normal] = Normals;
        ar[(int)Mesh.ArrayType.Index] = Indices;

        rd.BufferClear(QuadBuffer, 0, (uint)Count[0] * 48);
        rd.BufferClear(QuadCountBuffer, 0, 8);
        
        //ch.PChunkByteAssignment(QBytes);
        if (ch.MeshInstance == null)
        {
            ch.MeshInstance = new MeshInstance3D();
            ch.CallDeferred("add_child", ch.MeshInstance);
        }

        ch.ConcavePolygon.SetFaces(Collision);
        ArrayMesh am = new ArrayMesh();

        am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, ar);
        ch.MeshInstance.Mesh = am;


        rd.FreeRid(UniformSet);
        rd.FreeRid(pipelineRID);
        rd.FreeRid(QuadBuffer);
        rd.FreeRid(QuadCountBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(ChunkDimensionalBuffer);
        rd.FreeRid(VoxelDataBuffer);
        rd.FreeRid(ShaderRID);
        //rd.Free();

        return;
    }
}
