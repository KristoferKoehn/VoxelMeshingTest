using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class Chunk : Node
{
	public uint[] ChunkData;


	public MeshInstance3D MeshInstance;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MeshInstance = new MeshInstance3D();
		MeshInstance.Mesh = new ArrayMesh();
        AddChild(MeshInstance);

        List<Vector3> vector3s = new List<Vector3>
        {
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 0)
        };

        List<Vector3> normals = new List<Vector3>
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1)
        };

        List<Vector3> uvs = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0)
        };


        AssignMesh(vector3s, normals, uvs);
        MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
	
	public void AssignMesh(List<Vector3> Vertices, List<Vector3> Normals, List<Vector3> UVs)
	{
        Array surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> verts = Vertices;


        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();


        ArrayMesh arrMesh = MeshInstance.Mesh as ArrayMesh;
        if (arrMesh != null)
        {
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        }
	}

    public void AssignMesh(Array SurfaceArray)
    {
        ArrayMesh arrMesh = MeshInstance.Mesh as ArrayMesh;
        if (arrMesh != null)
        {
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, SurfaceArray);
        }
    }
}
