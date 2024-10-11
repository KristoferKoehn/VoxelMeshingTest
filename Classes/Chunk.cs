using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;




public struct Face
{
    public Vector3[] Vertices = new Vector3[4];
    public Vector3[] Normals = new Vector3[4];
    public Vector3[] UVs = new Vector3[4];
    public int[] Indices = new int[]
    {
            0, 1, 2, 0, 2, 3
    };

    public Face()
    {
    }
}

[StructLayout(layoutKind: LayoutKind.Sequential)]
public struct Quad
{
    public Vector4[] Vertices;
    public Vector3 Normals;
    public int UVIndex;
}



public partial class Chunk : Node
{
    public uint[] ChunkData;

    [Export] bool generate = false;
    public MeshInstance3D MeshInstance;

    int[] INDICES = new int[] { 0, 1, 2, 0, 2, 3 };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{


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

    public void ProcessFaces(Face[] faces)
    {
        Array surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> Vertices = new List<Vector3>();
        List<Vector3> Normals = new List<Vector3>();
        List<Vector3> UVs = new List<Vector3>();
        List<int> Indices = new List<int>();

        int TotalVertexIndexCount = 0;
        foreach (Face face in faces)
        {
            Vertices.AddRange(face.Vertices);
            Normals.AddRange(face.Normals);
            UVs.AddRange(face.UVs);

            foreach (int index in face.Indices)
            {
                Indices.Add(TotalVertexIndexCount + index);
            }
            TotalVertexIndexCount += 4;
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

        AssignMesh(surfaceArray);
    }

    public void ProcessQuads(Quad[] faces)
    {
        Array surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> Vertices = new List<Vector3>();
        List<Vector3> Normals = new List<Vector3>();
        List<Vector3> UVs = new List<Vector3>();
        List<int> Indices = new List<int>();

        int TotalVertexIndexCount = 0;
        foreach (Quad face in faces)
        {

            Vertices.Add(new Vector3(face.Vertices[0].X, face.Vertices[0].Y, face.Vertices[0].Z));
            Vertices.Add(new Vector3(face.Vertices[0].W, face.Vertices[1].X, face.Vertices[1].Y));
            Vertices.Add(new Vector3(face.Vertices[1].Z, face.Vertices[1].W, face.Vertices[2].X));
            Vertices.Add(new Vector3(face.Vertices[2].Y, face.Vertices[2].Z, face.Vertices[2].W));

            Normals.Add(face.Normals);
            Normals.Add(face.Normals);
            Normals.Add(face.Normals);
            Normals.Add(face.Normals);

            Vector3 UVindex = new Vector3(face.UVIndex % 32, face.UVIndex / 32, 0) / 32.0f;

            Vector3 uva = new Vector3(0.03125f, 0.03125f, 0) + UVindex;
            Vector3 uvb = new Vector3(0, 0.03125f, 0) + UVindex;
            Vector3 uvc = new Vector3(0, 0, 0) + UVindex;
            Vector3 uvd = new Vector3(0.03125f, 0, 0) + UVindex;

            UVs.Add(uva);
            UVs.Add(uvb);
            UVs.Add(uvc);
            UVs.Add(uvd);

            foreach (int index in INDICES)
            {
                Indices.Add(TotalVertexIndexCount + index);
            }
            TotalVertexIndexCount += 4;
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

        AssignMesh(surfaceArray);
    }

    public void ProcessBytes(float[] data)
    {

        Array surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> Vertices = new List<Vector3>();
        List<Vector3> Normals = new List<Vector3>();
        List<Vector3> UVs = new List<Vector3>();
        List<int> Indices = new List<int>();

        int TotalVertexIndexCount = 0;

        GD.Print($"Length of float data {data.Length}");

        if ( data == null || data.Length == 0)
        {
            AssignMesh(null); 
            return;
        }

        for (int j = 0, i = 0; j < data.Length/16; j++, i += 16)
        {

            Vertices.Add(new Vector3(data[i], data[i + 1], data[i + 2]));
            Vertices.Add(new Vector3(data[i + 3], data[i + 4], data[i + 5]));
            Vertices.Add(new Vector3(data[i + 6], data[i + 7], data[i + 8]));
            Vertices.Add(new Vector3(data[i + 9], data[i + 10], data[i + 11]));

            Vector3 norms = new Vector3(data[i + 13], data[i + 14], data[i + 15]);

            Normals.Add(norms);
            Normals.Add(norms);
            Normals.Add(norms);
            Normals.Add(norms);

            Vector3 UVindex = new Vector3((int)data[i + 12] % 32, (int)data[i + 12] / 32, 0) / 32.0f;

            Vector3 uva = new Vector3(0.03125f, 0.03125f, 0) + UVindex;
            Vector3 uvb = new Vector3(0, 0.03125f, 0) + UVindex;
            Vector3 uvc = new Vector3(0, 0, 0) + UVindex;
            Vector3 uvd = new Vector3(0.03125f, 0, 0) + UVindex;

            UVs.Add(uva);
            UVs.Add(uvb);
            UVs.Add(uvc);
            UVs.Add(uvd);

            foreach (int index in INDICES)
            {
                Indices.Add(TotalVertexIndexCount + index);
            }
            TotalVertexIndexCount += 4;

        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

        AssignMesh(surfaceArray);
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
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, SurfaceArray);
        }
    }


    public void GenerateBlock(Vector3 pos)
    {

        List<Face> faces = new List<Face>();

        Face TopFace = new Face();

        int[] indices = new int[]
        {
            0, 1, 2, 0, 2, 3
        };

        Vector3[] UVs = new Vector3[4]
        {
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
        };


        TopFace.Vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3( 0.5f, 0.5f, -0.5f),
            new Vector3( 0.5f, 0.5f,  0.5f),
            new Vector3(-0.5f, 0.5f,  0.5f),
        };

        TopFace.Normals = new Vector3[4]
        {
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0)
        };

        TopFace.UVs = UVs;
        TopFace.Indices = indices;


        Face NorthFace = new Face();

        NorthFace.Vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, 0.5f,  -0.5f),
            new Vector3(-0.5f, 0.5f,  -0.5f),
        };

        NorthFace.Normals = new Vector3[4]
        {
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1)
        };

        NorthFace.UVs = UVs;
        NorthFace.Indices = indices;


        Face EastFace = new Face();

        EastFace.Vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
        };

        EastFace.Normals = new Vector3[4]
        {
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 0)
        };

        EastFace.UVs = UVs;
        EastFace.Indices = indices;

        Face WestFace = new Face();

        WestFace.Vertices = new Vector3[4]
        {
            new Vector3(0.5f,  -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f,  0.5f),
            new Vector3(0.5f,  0.5f,  -0.5f),
        };

        WestFace.Normals = new Vector3[4]
        {
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0)
        };

        WestFace.UVs = UVs;
        WestFace.Indices = indices;

        Face SouthFace = new Face();

        SouthFace.Vertices = new Vector3[4]
        {
            new Vector3(0.5f,  -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f,  0.5f),
            new Vector3(0.5f,  0.5f,  0.5f),
        };

        SouthFace.Normals = new Vector3[4]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1)
        };

        SouthFace.UVs = UVs;
        SouthFace.Indices = indices;

        Face BottomFace = new Face();

        BottomFace.Vertices = new Vector3[4]
        {
            new Vector3(0.5f,  -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(0.5f,  -0.5f,  0.5f),
        };

        BottomFace.Normals = new Vector3[4]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1)
        };

        BottomFace.UVs = UVs;
        BottomFace.Indices = indices;

        faces.Add(BottomFace);
        faces.Add(SouthFace);
        faces.Add(TopFace);
        faces.Add(NorthFace);
        faces.Add(EastFace);
        faces.Add(WestFace);

        ProcessFaces(faces.ToArray());
    }

}
