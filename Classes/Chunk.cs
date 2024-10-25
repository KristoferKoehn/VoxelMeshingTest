using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
public partial class Chunk : Node3D
{
    public int[] ChunkData;

    public bool Generated = false;
    public bool Meshed = false;
    public bool Collision = false;

    public MeshInstance3D MeshInstance;
    public StaticBody3D SB;
    public CollisionShape3D CollisionShape;
    ConcavePolygonShape3D ConcavePolygon;
    public VisibleOnScreenEnabler3D VisibleOnScreenEnabler;
    public VisibleOnScreenNotifier3D VisibleOnScreenNotifier;
    public Vector3 ChunkPosition { get; set; }
    public Vector3I ChunkCoordinates { get; set; }

    public byte[] meshbytes { get; set; } = null;

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
        ConcavePolygon = new ConcavePolygonShape3D();
        CollisionShape.Shape = ConcavePolygon;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.GlobalPosition = ChunkPosition;
        /*
        MeshInstance.GlobalPosition = GlobalPosition;
        SB.GlobalPosition = GlobalPosition;
        //GenerateBlock(new Vector3(0,0,0));
        */
        if (MeshInstance != null)
        {
            MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");
        } else
        {
            GD.Print("material fucked");
        }
        Visible = true;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!Collision && Meshed)
        {
            ConcavePolygon.SetFaces(MeshInstance.Mesh.GetFaces());
            Collision = true;
        }

        if (!Meshed)
        {
            if (meshbytes != null)
            {
                PChunkByteIngestion(meshbytes);
            }
        }
    }

    async public void PChunkByteIngestion(byte[] quadbytes, byte[] countBytes)
    {
        await Task.Run(() =>
        {
            if (MeshInstance != null)
            {
                MeshInstance.QueueFree();
            }

            var PChunk = ClassDB.Instantiate("PChunk");
            PChunk.AsGodotObject().Call("set_bytes", quadbytes);
            CallDeferred("add_child", PChunk);
            MeshInstance = (MeshInstance3D)PChunk;
            MeshInstance.Mesh.CallDeferred(Mesh.MethodName.SurfaceSetMaterial, 0, GD.Load<ShaderMaterial>("res://Resources/Test.tres"));
            ConcavePolygon.CallDeferred("set_faces", MeshInstance.Mesh.GetFaces());
        });
    }

    public void PChunkByteAssignment(byte[] quadbytes)
    {
        meshbytes = quadbytes;
    }

    public void PChunkByteIngestion(byte[] quadbytes)
    {
        if (MeshInstance != null)
        {
            MeshInstance.QueueFree();
        }

        var PChunk = ClassDB.Instantiate("PChunk");
        PChunk.AsGodotObject().Call("set_bytes", quadbytes);
        AddChild((MeshInstance3D)PChunk);
        MeshInstance = (MeshInstance3D)PChunk;
        MeshInstance.Mesh.CallDeferred(Mesh.MethodName.SurfaceSetMaterial, 0, GD.Load<ShaderMaterial>("res://Resources/Test.tres"));
        GD.Print("setting collision...");
        Meshed = true;
    }
}
