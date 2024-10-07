using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class ChunkMeshManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private static ChunkMeshManager instance = null;


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

		Rid VertexBuffer = rd.StorageBufferCreate(256);
        Rid NormalBuffer = rd.StorageBufferCreate(256);
        Rid UVBuffer = rd.StorageBufferCreate(256);
        Rid CountBuffer = rd.StorageBufferCreate(sizeof(uint));

        Rid ChunkDataBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);

        Array<RDUniform> Uniforms = new Array<RDUniform>();

        //Vertex uniform
        RDUniform VertexUniform = new RDUniform();
		Uniforms.Add(VertexUniform);
        VertexUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        VertexUniform.Binding = 0;
        VertexUniform.AddId(VertexBuffer);

        //Normal uniform
        RDUniform NormalUniform = new RDUniform();
        Uniforms.Add(NormalUniform);
        NormalUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        NormalUniform.Binding = 1;
        NormalUniform.AddId(NormalBuffer);

        //UV uniform
        RDUniform UVUniform = new RDUniform();
        Uniforms.Add(UVUniform);
        UVUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        UVUniform.Binding = 2;
        UVUniform.AddId(UVBuffer);

        //FaceCount uniform
        RDUniform CountUniform = new RDUniform();
        Uniforms.Add(CountUniform);
        CountUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        CountUniform.Binding = 3;
        CountUniform.AddId(CountBuffer);

        //chunk data input uniform
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

		rd.Submit();
		rd.Sync();

        byte[] VertexBytes = rd.BufferGetData(VertexBuffer);
        byte[] NormalBytes = rd.BufferGetData(NormalBuffer);
        byte[] UVBytes = rd.BufferGetData(UVBuffer);
        byte[] countBytes = rd.BufferGetData(CountBuffer);


        float[] VertexList = new float[VertexBytes.Length / 4];
        float[] NormalList = new float[NormalBytes.Length / 4];
        float[] UVList = new float[UVBytes.Length / 4];
        int[] Count = new int[countBytes.Length / 4];

        Buffer.BlockCopy(VertexBytes, 0, VertexList, 0, VertexBytes.Length);
        Buffer.BlockCopy(NormalBytes, 0, NormalList, 0, NormalBytes.Length);
        Buffer.BlockCopy(UVBytes, 0, UVList, 0, UVBytes.Length);
        Buffer.BlockCopy(countBytes, 0, Count, 0, countBytes.Length);

        Face[] faces = new Face[Count[0]];

        for(int i = 0, j = 0; i < Count[0]; i++ , j+=16)
        {
            Face tempFace = new Face();

            tempFace.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            tempFace.Vertices[0] = new Vector3(VertexList[j], VertexList[j + 1], VertexList[j + 2]);
            tempFace.Vertices[1] = new Vector3(VertexList[j + 4], VertexList[j + 5], VertexList[j + 6]);
            tempFace.Vertices[2] = new Vector3(VertexList[j + 8], VertexList[j + 9], VertexList[j + 10]);
            tempFace.Vertices[3] = new Vector3(VertexList[j + 12], VertexList[j + 13], VertexList[j + 14]);


            tempFace.Normals[0] = new Vector3(NormalList[j], NormalList[j + 1], NormalList[j + 2]);
            tempFace.Normals[1] = new Vector3(NormalList[j + 4], NormalList[j + 5], NormalList[j + 6]);
            tempFace.Normals[2] = new Vector3(NormalList[j + 8], NormalList[j + 9], NormalList[j + 10]);
            tempFace.Normals[3] = new Vector3(NormalList[j + 12], NormalList[j + 13], NormalList[j + 14]);


            tempFace.UVs[0] = new Vector3(UVList[j], UVList[j + 1], UVList[j + 2]);
            tempFace.UVs[1] = new Vector3(UVList[j + 4], UVList[j + 5], UVList[j + 6]);
            tempFace.UVs[2] = new Vector3(UVList[j + 8], UVList[j + 9], UVList[j + 10]);
            tempFace.UVs[3] = new Vector3(UVList[j + 12], UVList[j + 13], UVList[j + 14]);


            faces[i] = tempFace;
        }

        //GD.Print(Vertices.Length);
        

        return faces;
	}

}
