using Godot;
using Godot.Collections;


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

    public static Array GenerateMesh(uint[] Data)
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
		Rid StorageBuffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
		Array<RDUniform> Uniforms = new Array<RDUniform>();
		RDUniform ComputeUniform = new RDUniform();
		Uniforms.Add(ComputeUniform);
		ComputeUniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
		ComputeUniform.Binding = 0;
		ComputeUniform.AddId(StorageBuffer);

		Rid UniformSet = rd.UniformSetCreate(Uniforms, ShaderRID, 0);

		rd.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
		rd.ComputeListBindComputePipeline(ComputeList, pipelineRID);
		rd.ComputeListDispatch(ComputeList, 1, 1, 1);
		rd.ComputeListEnd();

		rd.Submit();
		rd.Sync();

		byte[] OutputData = rd.BufferGetData(StorageBuffer);

		System.Buffer.BlockCopy(OutputData, 0, Data, 0, OutputData.Length);


		foreach(uint b in Data)
		{
            GD.Print($"{b},");
        }
		

        return null;
	}

}
