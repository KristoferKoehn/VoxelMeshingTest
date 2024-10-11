using Godot;
using System.Threading.Tasks;

public partial class GameLoop : Node3D
{
    [Export]
    public FastNoiseLite Terrain { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
	{


		RNGManager.Instance();
		ChunkGeneratorManager.Instance();
        ChunkGeneratorManager.Terrain = Terrain;
		ChunkMeshManager.Instance();


		Face face = new Face();


        /*
		for(int i  = 0; i < 1; i++)
		{
			for (int j = 0; j < 1; j++)
			{
                uint[] data = ChunkGeneratorManager.Instance().GenerateChunk(i, 0, j);
                Face[] faces = ChunkMeshManager.GenerateMeshSlow(data);

                Chunk ch = new Chunk();
                ch.ProcessFaces(faces);
                AddChild(ch);
				ch.MeshInstance.GlobalPosition = new Vector3(i * 128, 0, j * 128);
            }
		}
		*/


        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                
                //Quad[] faces = ChunkMeshManager.GenerateMeshFast(data);
                //float[] meshBytes = ChunkMeshManager.GenerateMeshFastBytes(data);

                Task<Chunk> getData = Task.Run(() =>
                {

                    uint[] data = ChunkGeneratorManager.Instance().GenerateChunk(i, 0, j);
                    float[] meshBytes = ChunkMeshManager.GenerateMeshFastBytes(data);

                    Chunk chunk = new Chunk();
                    chunk.ProcessBytes(meshBytes);
                    
                    return chunk;
                });

                Chunk ch = await getData;
                AddChild(ch);
                ch.MeshInstance.GlobalPosition = new Vector3(i * 64, 0, j * 64);
                //ch.ProcessQuads(faces);
            }
        }

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
