using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Threading;


enum CullDirection
{
    North,
    West,
    NorthWest,
    NorthEast,
    South,
    SouthWest,
    SouthEast,
    East,
    None
}
public partial class Chunk : Node
{
    public int[] ChunkData;
    public Vector3 GlobalPosition = Vector3.Zero;


    [Export] bool generate = false;
    public MeshInstance3D MeshInstance;
    public StaticBody3D SB;
    public CollisionShape3D CollisionShape;
    ConcavePolygonShape3D ConcavePolygon;
    public VisibleOnScreenEnabler3D VisibleOnScreenEnabler;
    public VisibleOnScreenNotifier3D VisibleOnScreenNotifier;

    int[] INDICES = new int[] { 0, 1, 2, 0, 2, 3 };

    private CullDirection CullDirection { get; set; }

    public Chunk()
    {
        MeshInstance = new MeshInstance3D();
        AddChild(MeshInstance);
        SB = new StaticBody3D();
        AddChild(SB);
        CollisionShape = new CollisionShape3D();
        SB.AddChild(CollisionShape);
        ConcavePolygon =  new ConcavePolygonShape3D();
        CollisionShape.Shape = ConcavePolygon;
    }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        MeshInstance.GlobalPosition = GlobalPosition;
        SB.GlobalPosition = GlobalPosition;
        //GenerateBlock(new Vector3(0,0,0));

        if (MeshInstance != null)
        {
            MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (generate)
        {
            generate = false;
            MeshInstance.QueueFree();
            MeshInstance = new MeshInstance3D();
            MeshInstance.Mesh = new ArrayMesh();
            AddChild(MeshInstance);

            //GenerateBlock(new Vector3(0, 0, 0));
        }
    }

    public void ProcessBytes(float[] data)
    {

        Array surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> Vertices = new List<Vector3>();
        List<Vector3> Normals = new List<Vector3>();
        //List<Vector3> UVs = new List<Vector3>();
        List<Color> Colors = new List<Color>();
        List<int> Indices = new List<int>();

        int TotalVertexIndexCount = 0;

        if ( data == null || data.Length == 0)
        {
            AssignMesh(null); 
            return;
        }

        for (int j = 0, i = 0; j < data.Length/16; j++, i += 16)
        {

            Vector3 vectorA = new Vector3(data[i], data[i + 1], data[i + 2]);
            Vector3 vectorB = new Vector3(data[i + 3], data[i + 4], data[i + 5]);
            Vector3 vectorC = new Vector3(data[i + 6], data[i + 7], data[i + 8]);
            Vector3 vectorD = new Vector3(data[i + 9], data[i + 10], data[i + 11]);

            Vertices.Add(vectorA);
            Vertices.Add(vectorB);
            Vertices.Add(vectorC);
            Vertices.Add(vectorD);

            Vector3 norms = new Vector3(data[i + 13], data[i + 14], data[i + 15]);

            Normals.Add(norms);
            Normals.Add(norms);
            Normals.Add(norms);
            Normals.Add(norms);

            //Vector3 UVindex = new Vector3((int)data[i + 12] % 32, (int)data[i + 12] / 32, 0) / 32.0f;

            switch ((int)data[i + 12])
            {
                case 1:
                    Colors.Add(Color.Color8(8, 147, 0));
                    Colors.Add(Color.Color8(8, 147, 0));
                    Colors.Add(Color.Color8(8, 147, 0));
                    Colors.Add(Color.Color8(8, 147, 0));
                    break;
                case 2:
                    Colors.Add(Color.Color8(219, 168, 96));
                    Colors.Add(Color.Color8(219, 168, 96));
                    Colors.Add(Color.Color8(219, 168, 96));
                    Colors.Add(Color.Color8(219, 168, 96));
                    break;
                case 3:
                    Colors.Add(Color.Color8(128, 128, 128));
                    Colors.Add(Color.Color8(128, 128, 128));
                    Colors.Add(Color.Color8(128, 128, 128));
                    Colors.Add(Color.Color8(128, 128, 128));
                    break;
                case 16:
                    Colors.Add(new Color(0, 0, 1));
                    Colors.Add(new Color(0, 0, 1));
                    Colors.Add(new Color(0, 0, 1));
                    Colors.Add(new Color(0, 0, 1));
                    break;
                default:
                    break;

            }

            /*
            Vector3 uva = new Vector3(0.03125f, 0.03125f, 0) + UVindex;
            Vector3 uvb = new Vector3(0.005f, 0.03125f, 0) + UVindex;
            Vector3 uvc = new Vector3(0.005f, 0.005f, 0) + UVindex;
            Vector3 uvd = new Vector3(0.03125f, 0.005f, 0) + UVindex;

            UVs.Add(uva);
            UVs.Add(uvb);
            UVs.Add(uvc);
            UVs.Add(uvd);
            */

            foreach (int index in INDICES)
            {
                Indices.Add(TotalVertexIndexCount + index);
            }
            TotalVertexIndexCount += 4;

        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        //surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = Colors.ToArray();

        AssignMesh(surfaceArray);
    }

    public void AssignChunkData(int[] ChunkData)
    {
        this.ChunkData = ChunkData;
    }

    public void AssignMesh(Array SurfaceArray)
    {

        if (MeshInstance != null)
        {
            MeshInstance.QueueFree();
        }
        MeshInstance = new MeshInstance3D();
        MeshInstance.Mesh = new ArrayMesh();
        AddChild(MeshInstance);

        if (SurfaceArray == null)
        {
            return;
        }

        ArrayMesh arrMesh = MeshInstance.Mesh as ArrayMesh;
        if (arrMesh != null)
        {
            //arrMesh.CallDeferred("add_surface_from_arrays", (int)Mesh.PrimitiveType.Triangles, SurfaceArray);

            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, SurfaceArray);
        }

        ThreadPool.QueueUserWorkItem((state) =>
        {
            ConcavePolygon.CallDeferred("set_faces", MeshInstance.Mesh.GetFaces());
        });

        //let's wait on this a beat lmao
        //the mesh is far too unoptimized for this to be anywhere near good enough
        //MeshInstance.CreateTrimeshCollision();
    }
}
