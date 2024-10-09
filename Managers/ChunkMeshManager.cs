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

    }

    public static Face[] GenerateMesh(uint[] Data)
	{

        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Compute/ChunkMesher.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid ShaderRID = rd.ShaderCreateFromSpirV(shaderBytecode);

		Rid pipelineRID = rd.ComputePipelineCreate(ShaderRID);

		long ComputeList = rd.ComputeListBegin();

		//compute uniform
		byte[] inputBytes = new byte[Data.Length * sizeof(uint)];
		System.Buffer.BlockCopy(Data, 0, inputBytes, 0, inputBytes.Length);

        uint BufferSize = 16777216 * 2;

		Rid TopVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid TopNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid TopUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid TopCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid NorthVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid NorthNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid NorthUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid NorthCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid EastVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid EastNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid EastUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid EastCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid SouthVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid SouthNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid SouthUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid SouthCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid WestVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid WestNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid WestUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid WestCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid BottomVertexBuffer = rd.StorageBufferCreate(BufferSize);
        Rid BottomNormalBuffer = rd.StorageBufferCreate(BufferSize);
        Rid BottomUVBuffer = rd.StorageBufferCreate(BufferSize);
        Rid BottomCountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);

        Array<RDUniform> Uniforms = new Array<RDUniform>();

        //top Vertex uniform
        RDUniform TopFaceVertexUniform = new RDUniform();
		Uniforms.Add(TopFaceVertexUniform);
        TopFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        TopFaceVertexUniform.Binding = 0;
        TopFaceVertexUniform.AddId(TopVertexBuffer);

        //top Normal uniform
        RDUniform TopNormalUniform = new RDUniform();
        Uniforms.Add(TopNormalUniform);
        TopNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        TopNormalUniform.Binding = 1;
        TopNormalUniform.AddId(TopNormalBuffer);

        //top UV uniform
        RDUniform TopUVUniform = new RDUniform();
        Uniforms.Add(TopUVUniform);
        TopUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        TopUVUniform.Binding = 2;
        TopUVUniform.AddId(TopUVBuffer);

        //top FaceCount uniform
        RDUniform TopCountUniform = new RDUniform();
        Uniforms.Add(TopCountUniform);
        TopCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        TopCountUniform.Binding = 3;
        TopCountUniform.AddId(TopCountBuffer);

        //North Vertex uniform
        RDUniform NorthFaceVertexUniform = new RDUniform();
        Uniforms.Add(NorthFaceVertexUniform);
        NorthFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        NorthFaceVertexUniform.Binding = 5;
        NorthFaceVertexUniform.AddId(NorthVertexBuffer);

        //North Normal uniform
        RDUniform NorthNormalUniform = new RDUniform();
        Uniforms.Add(NorthNormalUniform);
        NorthNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        NorthNormalUniform.Binding = 6;
        NorthNormalUniform.AddId(NorthNormalBuffer);

        //North UV uniform
        RDUniform NorthUVUniform = new RDUniform();
        Uniforms.Add(NorthUVUniform);
        NorthUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        NorthUVUniform.Binding = 7;
        NorthUVUniform.AddId(NorthUVBuffer);

        //North FaceCount uniform
        RDUniform NorthCountUniform = new RDUniform();
        Uniforms.Add(NorthCountUniform);
        NorthCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        NorthCountUniform.Binding = 8;
        NorthCountUniform.AddId(NorthCountBuffer);

        //East Vertex uniform
        RDUniform EastFaceVertexUniform = new RDUniform();
        Uniforms.Add(EastFaceVertexUniform);
        EastFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        EastFaceVertexUniform.Binding = 9;
        EastFaceVertexUniform.AddId(EastVertexBuffer);

        //East Normal uniform
        RDUniform EastNormalUniform = new RDUniform();
        Uniforms.Add(EastNormalUniform);
        EastNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        EastNormalUniform.Binding = 10;
        EastNormalUniform.AddId(EastNormalBuffer);

        //East UV uniform
        RDUniform EastUVUniform = new RDUniform();
        Uniforms.Add(EastUVUniform);
        EastUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        EastUVUniform.Binding = 11;
        EastUVUniform.AddId(EastUVBuffer);

        //East FaceCount uniform
        RDUniform EastCountUniform = new RDUniform();
        Uniforms.Add(EastCountUniform);
        EastCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        EastCountUniform.Binding = 12;
        EastCountUniform.AddId(EastCountBuffer);

        //South Vertex uniform
        RDUniform SouthFaceVertexUniform = new RDUniform();
        Uniforms.Add(SouthFaceVertexUniform);
        SouthFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        SouthFaceVertexUniform.Binding = 13;
        SouthFaceVertexUniform.AddId(SouthVertexBuffer);

        //South Normal uniform
        RDUniform SouthNormalUniform = new RDUniform();
        Uniforms.Add(SouthNormalUniform);
        SouthNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        SouthNormalUniform.Binding = 14;
        SouthNormalUniform.AddId(SouthNormalBuffer);

        //South UV uniform
        RDUniform SouthUVUniform = new RDUniform();
        Uniforms.Add(SouthUVUniform);
        SouthUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        SouthUVUniform.Binding = 15;
        SouthUVUniform.AddId(SouthUVBuffer);

        //South FaceCount uniform
        RDUniform SouthCountUniform = new RDUniform();
        Uniforms.Add(SouthCountUniform);
        SouthCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        SouthCountUniform.Binding = 16;
        SouthCountUniform.AddId(SouthCountBuffer);

        //West Vertex uniform
        RDUniform WestFaceVertexUniform = new RDUniform();
        Uniforms.Add(WestFaceVertexUniform);
        WestFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        WestFaceVertexUniform.Binding = 17;
        WestFaceVertexUniform.AddId(WestVertexBuffer);

        //West Normal uniform
        RDUniform WestNormalUniform = new RDUniform();
        Uniforms.Add(WestNormalUniform);
        WestNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        WestNormalUniform.Binding = 18;
        WestNormalUniform.AddId(WestNormalBuffer);

        //West UV uniform
        RDUniform WestUVUniform = new RDUniform();
        Uniforms.Add(WestUVUniform);
        WestUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        WestUVUniform.Binding = 19;
        WestUVUniform.AddId(WestUVBuffer);

        //West FaceCount uniform
        RDUniform WestCountUniform = new RDUniform();
        Uniforms.Add(WestCountUniform);
        WestCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        WestCountUniform.Binding = 20;
        WestCountUniform.AddId(WestCountBuffer);

        //Bottom Vertex uniform
        RDUniform BottomFaceVertexUniform = new RDUniform();
        Uniforms.Add(BottomFaceVertexUniform);
        BottomFaceVertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        BottomFaceVertexUniform.Binding = 21;
        BottomFaceVertexUniform.AddId(BottomVertexBuffer);

        //Bottom Normal uniform
        RDUniform BottomNormalUniform = new RDUniform();
        Uniforms.Add(BottomNormalUniform);
        BottomNormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        BottomNormalUniform.Binding = 22;
        BottomNormalUniform.AddId(BottomNormalBuffer);

        //Bottom UV uniform
        RDUniform BottomUVUniform = new RDUniform();
        Uniforms.Add(BottomUVUniform);
        BottomUVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        BottomUVUniform.Binding = 23;
        BottomUVUniform.AddId(BottomUVBuffer);

        //Bottom FaceCount uniform
        RDUniform BottomCountUniform = new RDUniform();
        Uniforms.Add(BottomCountUniform);
        BottomCountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        BottomCountUniform.Binding = 24;
        BottomCountUniform.AddId(BottomCountBuffer);

        //Bottom data input uniform
        RDUniform ChunkDataUniform = new RDUniform();
        Uniforms.Add(ChunkDataUniform);
        ChunkDataUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        ChunkDataUniform.Binding = 4;
        ChunkDataUniform.AddId(ChunkDataBuffer);

        Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

		rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
		rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
		rd.ComputeListDispatch(ComputeList, 1, 1, 1);
		rd.ComputeListEnd();

        float sync = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        rd.Submit();
		rd.Sync();

        byte[] TopVertexBytes = rd.BufferGetData(TopVertexBuffer);
        byte[] TopNormalBytes = rd.BufferGetData(TopNormalBuffer);
        byte[] TopUVBytes =     rd.BufferGetData(TopUVBuffer);
        byte[] TopCountBytes =  rd.BufferGetData(TopCountBuffer);

        byte[] NorthVertexBytes = rd.BufferGetData(NorthVertexBuffer);
        byte[] NorthNormalBytes = rd.BufferGetData(NorthNormalBuffer);
        byte[] NorthUVBytes =     rd.BufferGetData(NorthUVBuffer);
        byte[] NorthCountBytes =  rd.BufferGetData(NorthCountBuffer);

        byte[] EastVertexBytes = rd.BufferGetData(EastVertexBuffer);
        byte[] EastNormalBytes = rd.BufferGetData(EastNormalBuffer);
        byte[] EastUVBytes =     rd.BufferGetData(EastUVBuffer);
        byte[] EastCountBytes =  rd.BufferGetData(EastCountBuffer);

        byte[] SouthVertexBytes = rd.BufferGetData(SouthVertexBuffer);
        byte[] SouthNormalBytes = rd.BufferGetData(SouthNormalBuffer);
        byte[] SouthUVBytes =     rd.BufferGetData(SouthUVBuffer);
        byte[] SouthCountBytes =  rd.BufferGetData(SouthCountBuffer);

        byte[] WestVertexBytes = rd.BufferGetData(WestVertexBuffer);
        byte[] WestNormalBytes = rd.BufferGetData(WestNormalBuffer);
        byte[] WestUVBytes =     rd.BufferGetData(WestUVBuffer);
        byte[] WestCountBytes =  rd.BufferGetData(WestCountBuffer);

        byte[] BottomVertexBytes = rd.BufferGetData(BottomVertexBuffer);
        byte[] BottomNormalBytes = rd.BufferGetData(BottomNormalBuffer);
        byte[] BottomUVBytes =     rd.BufferGetData(BottomUVBuffer);
        byte[] BottomCountBytes =  rd.BufferGetData(BottomCountBuffer);

        float[] TopFaceVertexList = new float[BufferSize];
        float[] TopNormalList = new float[BufferSize];
        float[] TopUVList = new float[BufferSize];
        int[]   TopCount = new int[TopCountBytes.Length];

        float[] NorthFaceVertexList = new float[BufferSize];
        float[] NorthNormalList = new float[BufferSize];
        float[] NorthUVList = new float[BufferSize];
        int[]   NorthCount = new int[NorthCountBytes.Length];

        float[] EastFaceVertexList = new float[BufferSize];
        float[] EastNormalList = new float[BufferSize];
        float[] EastUVList = new float[BufferSize];
        int[]   EastCount = new int[EastCountBytes.Length];

        float[] SouthFaceVertexList = new float[BufferSize];
        float[] SouthNormalList = new float[BufferSize];
        float[] SouthUVList = new float[BufferSize];
        int[] SouthCount = new int[TopCountBytes.Length];

        float[] WestFaceVertexList = new float[BufferSize];
        float[] WestNormalList = new float[BufferSize];
        float[] WestUVList = new float[BufferSize];
        int[] WestCount = new int[NorthCountBytes.Length];

        float[] BottomFaceVertexList = new float[BufferSize];
        float[] BottomNormalList = new float[BufferSize];
        float[] BottomUVList = new float[BufferSize];
        int[] BottomCount = new int[EastCountBytes.Length];


        rd.FreeRid(pipelineRID);
        rd.FreeRid(TopVertexBuffer);
        rd.FreeRid(TopNormalBuffer);
        rd.FreeRid(TopUVBuffer);
        rd.FreeRid(TopCountBuffer);
        rd.FreeRid(ChunkDataBuffer);
        rd.FreeRid(ShaderRID);
        rd.Free();


        Buffer.BlockCopy(TopVertexBytes, 0, TopFaceVertexList, 0, TopVertexBytes.Length);
        Buffer.BlockCopy(TopNormalBytes, 0, TopNormalList,     0, TopNormalBytes.Length);
        Buffer.BlockCopy(TopUVBytes,     0, TopUVList,         0, TopUVBytes.Length);
        Buffer.BlockCopy(TopCountBytes,  0, TopCount,          0, TopCountBytes.Length);

        Buffer.BlockCopy(NorthVertexBytes, 0, NorthFaceVertexList, 0, NorthVertexBytes.Length);
        Buffer.BlockCopy(NorthNormalBytes, 0, NorthNormalList,     0, NorthNormalBytes.Length);
        Buffer.BlockCopy(NorthUVBytes,     0, NorthUVList,         0, NorthUVBytes.Length);
        Buffer.BlockCopy(NorthCountBytes,  0, NorthCount,          0, NorthCountBytes.Length);

        Buffer.BlockCopy(EastVertexBytes, 0, EastFaceVertexList, 0, EastVertexBytes.Length);
        Buffer.BlockCopy(EastNormalBytes, 0, EastNormalList,     0, EastNormalBytes.Length);
        Buffer.BlockCopy(EastUVBytes,     0, EastUVList,         0, EastUVBytes.Length);
        Buffer.BlockCopy(EastCountBytes,  0, EastCount,          0, EastCountBytes.Length);

        Buffer.BlockCopy(SouthVertexBytes, 0, SouthFaceVertexList, 0, SouthVertexBytes.Length);
        Buffer.BlockCopy(SouthNormalBytes, 0, SouthNormalList,     0, SouthNormalBytes.Length);
        Buffer.BlockCopy(SouthUVBytes,     0, SouthUVList,         0, SouthUVBytes.Length);
        Buffer.BlockCopy(SouthCountBytes,  0, SouthCount,          0, SouthCountBytes.Length);

        Buffer.BlockCopy(WestVertexBytes, 0, WestFaceVertexList, 0, WestVertexBytes.Length);
        Buffer.BlockCopy(WestNormalBytes, 0, WestNormalList,     0, WestNormalBytes.Length);
        Buffer.BlockCopy(WestUVBytes,     0, WestUVList,         0, WestUVBytes.Length);
        Buffer.BlockCopy(WestCountBytes,  0, WestCount,          0, WestCountBytes.Length);

        Buffer.BlockCopy(BottomVertexBytes, 0, BottomFaceVertexList, 0, BottomVertexBytes.Length);
        Buffer.BlockCopy(BottomNormalBytes, 0, BottomNormalList,     0, BottomNormalBytes.Length);
        Buffer.BlockCopy(BottomUVBytes,     0, BottomUVList,         0, BottomUVBytes.Length);
        Buffer.BlockCopy(BottomCountBytes,  0, BottomCount,          0, BottomCountBytes.Length);

        Face[] faces = new Face[TopCount[0] + NorthCount[0] + EastCount[0] + SouthCount[0] + WestCount[0] + BottomCount[0]];

        for(int i = 0, j = 0; i < TopCount[0]; i++ , j+=12)
        {
            Face tempFace = new Face();
            
            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(TopFaceVertexList[j], TopFaceVertexList[j + 1], TopFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(TopFaceVertexList[j + 3], TopFaceVertexList[j + 4], TopFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(TopFaceVertexList[j + 6], TopFaceVertexList[j + 7], TopFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(TopFaceVertexList[j + 9], TopFaceVertexList[j + 10], TopFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(TopNormalList[j], TopNormalList[j + 1], TopNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(TopNormalList[j + 3], TopNormalList[j + 4], TopNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(TopNormalList[j + 6], TopNormalList[j + 7], TopNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(TopNormalList[j + 9], TopNormalList[j + 10], TopNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(TopUVList[j], TopUVList[j + 1], TopUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(TopUVList[j + 3], TopUVList[j + 4], TopUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(TopUVList[j + 6], TopUVList[j + 7], TopUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(TopUVList[j + 9], TopUVList[j + 10], TopUVList[j + 11]);

            faces[i] = tempFace;
        }

        for (int i = 0, j = 0; i < NorthCount[0]; i++, j += 12)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(NorthFaceVertexList[j    ], NorthFaceVertexList[j + 1],  NorthFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(NorthFaceVertexList[j + 3], NorthFaceVertexList[j + 4],  NorthFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(NorthFaceVertexList[j + 6], NorthFaceVertexList[j + 7],  NorthFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(NorthFaceVertexList[j + 9], NorthFaceVertexList[j + 10], NorthFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(NorthNormalList[j    ], NorthNormalList[j + 1],  NorthNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(NorthNormalList[j + 3], NorthNormalList[j + 4],  NorthNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(NorthNormalList[j + 6], NorthNormalList[j + 7],  NorthNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(NorthNormalList[j + 9], NorthNormalList[j + 10], NorthNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(NorthUVList[j    ], NorthUVList[j + 1],  NorthUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(NorthUVList[j + 3], NorthUVList[j + 4],  NorthUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(NorthUVList[j + 6], NorthUVList[j + 7],  NorthUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(NorthUVList[j + 9], NorthUVList[j + 10], NorthUVList[j + 11]);

            faces[TopCount[0] + i] = tempFace;
        }

        for (int i = 0, j = 0; i < EastCount[0]; i++, j += 12)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(EastFaceVertexList[j],     EastFaceVertexList[j + 1],  EastFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(EastFaceVertexList[j + 3], EastFaceVertexList[j + 4],  EastFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(EastFaceVertexList[j + 6], EastFaceVertexList[j + 7],  EastFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(EastFaceVertexList[j + 9], EastFaceVertexList[j + 10], EastFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(EastNormalList[j],     EastNormalList[j + 1],  EastNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(EastNormalList[j + 3], EastNormalList[j + 4],  EastNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(EastNormalList[j + 6], EastNormalList[j + 7],  EastNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(EastNormalList[j + 9], EastNormalList[j + 10], EastNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(EastUVList[j    ], EastUVList[j + 1],  EastUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(EastUVList[j + 3], EastUVList[j + 4],  EastUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(EastUVList[j + 6], EastUVList[j + 7],  EastUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(EastUVList[j + 9], EastUVList[j + 10], EastUVList[j + 11]);

            faces[TopCount[0] + NorthCount[0] + i] = tempFace;

        }

        for (int i = 0, j = 0; i < SouthCount[0]; i++, j += 12)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(SouthFaceVertexList[j], SouthFaceVertexList[j + 1], SouthFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(SouthFaceVertexList[j + 3], SouthFaceVertexList[j + 4],  SouthFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(SouthFaceVertexList[j + 6], SouthFaceVertexList[j + 7],  SouthFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(SouthFaceVertexList[j + 9], SouthFaceVertexList[j + 10], SouthFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(SouthNormalList[j],     SouthNormalList[j + 1],  SouthNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(SouthNormalList[j + 3], SouthNormalList[j + 4],  SouthNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(SouthNormalList[j + 6], SouthNormalList[j + 7],  SouthNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(SouthNormalList[j + 9], SouthNormalList[j + 10], SouthNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(SouthUVList[j],     SouthUVList[j + 1],  SouthUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(SouthUVList[j + 3], SouthUVList[j + 4],  SouthUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(SouthUVList[j + 6], SouthUVList[j + 7],  SouthUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(SouthUVList[j + 9], SouthUVList[j + 10], SouthUVList[j + 11]);

            faces[TopCount[0] + NorthCount[0] + EastCount[0] + i] = tempFace;

        }

        for (int i = 0, j = 0; i < WestCount[0]; i++, j += 12)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(WestFaceVertexList[j],     WestFaceVertexList[j + 1],  WestFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(WestFaceVertexList[j + 3], WestFaceVertexList[j + 4],  WestFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(WestFaceVertexList[j + 6], WestFaceVertexList[j + 7],  WestFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(WestFaceVertexList[j + 9], WestFaceVertexList[j + 10], WestFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(WestNormalList[j],     WestNormalList[j + 1],  WestNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(WestNormalList[j + 3], WestNormalList[j + 4],  WestNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(WestNormalList[j + 6], WestNormalList[j + 7],  WestNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(WestNormalList[j + 9], WestNormalList[j + 10], WestNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(WestUVList[j],     WestUVList[j + 1],  WestUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(WestUVList[j + 3], WestUVList[j + 4],  WestUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(WestUVList[j + 6], WestUVList[j + 7],  WestUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(WestUVList[j + 9], WestUVList[j + 10], WestUVList[j + 11]);

            faces[TopCount[0] + NorthCount[0] + EastCount[0] + SouthCount[0] + i] = tempFace;

        }

        for (int i = 0, j = 0; i < BottomCount[0]; i++, j += 12)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(BottomFaceVertexList[j],     BottomFaceVertexList[j + 1],  BottomFaceVertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(BottomFaceVertexList[j + 3], BottomFaceVertexList[j + 4],  BottomFaceVertexList[j + 5]);
            tempFace.Vertices[2] = new Vector3(BottomFaceVertexList[j + 6], BottomFaceVertexList[j + 7],  BottomFaceVertexList[j + 8]);
            tempFace.Vertices[3] = new Vector3(BottomFaceVertexList[j + 9], BottomFaceVertexList[j + 10], BottomFaceVertexList[j + 11]);

            tempFace.Normals[0] = new Vector3(BottomNormalList[j],     BottomNormalList[j + 1],  BottomNormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(BottomNormalList[j + 3], BottomNormalList[j + 4],  BottomNormalList[j + 5]);
            tempFace.Normals[2] = new Vector3(BottomNormalList[j + 6], BottomNormalList[j + 7],  BottomNormalList[j + 8]);
            tempFace.Normals[3] = new Vector3(BottomNormalList[j + 9], BottomNormalList[j + 10], BottomNormalList[j + 11]);

            tempFace.UVs[0] = new Vector3(BottomUVList[j],     BottomUVList[j + 1],  BottomUVList[j + 2]);
            tempFace.UVs[1] = new Vector3(BottomUVList[j + 3], BottomUVList[j + 4],  BottomUVList[j + 5]);
            tempFace.UVs[2] = new Vector3(BottomUVList[j + 6], BottomUVList[j + 7],  BottomUVList[j + 8]);
            tempFace.UVs[3] = new Vector3(BottomUVList[j + 9], BottomUVList[j + 10], BottomUVList[j + 11]);

            faces[TopCount[0] + NorthCount[0] + EastCount[0] + SouthCount[0] + WestCount[0] + i] = tempFace;
        }

        return faces;
	}

}
