using Godot;
using System;
using System.Collections.Generic;
using VoxelMeshingTest.Classes;


public partial class Chunk : Node3D
{
    public int[,,] ChunkData = null;

    public bool Regen = false;
    public List<Tuple<Vector3I, int>> ChangePackets = new();

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

    public bool North = false;
    public bool East = false;
    public bool South = false;
    public bool West = false;
    public bool Up = false;
    public bool Down = false;

    public bool ImmediateChunk = false;
    public bool Visibility = true;

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
        GlobalPosition = ChunkPosition;

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
        if (ChunkData == null) {
            return; 
        }
        Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
        
        if ((pos - GlobalPosition).Length() > 640)
        {
            Visible = false;
            Visibility = Visible;
            return;
        }
        else
        {
            if (!Visible) {
                Remesh();
            }
            Visible = true;
            Visibility = Visible;
        }
        


        if ((GlobalPosition - pos).Y > GameConstants.CHUNK_DATA_SIZE)
        {
            if (Up) { Remesh(); }
            Up = false;
        }
        else
        {
            if (!Up) { Remesh(); }
            Up = true;
        }

        //if above GameConstants.CHUNK_DATA_SIZE, turn off down, else, turn on down
        if ((GlobalPosition - pos).Y < -GameConstants.CHUNK_DATA_SIZE)
        {
            if (Down) { Remesh(); }
            Down = false;
        }
        else
        {
            if (!Down) { Remesh(); }
            Down = true;
        }

        if ((GlobalPosition - pos).X < -GameConstants.CHUNK_DATA_SIZE)
        {
            if (East) { Remesh(); }
            East = false;
        }
        else
        {
            if (!East) { Remesh(); }
            East = true;
        }

        if ((GlobalPosition - pos).X > GameConstants.CHUNK_DATA_SIZE)
        {
            if (West) { Remesh(); }
            West = false;
        }
        else
        {
            if (!West) { Remesh(); }
            West = true;
        }

        if ((GlobalPosition - pos).Z < -GameConstants.CHUNK_DATA_SIZE)
        {
            if (North) { Remesh(); }
            North = false;
        }
        else
        {
            if (!North) { Remesh(); }
            North = true;
        }

        if ((GlobalPosition - pos).Z > GameConstants.CHUNK_DATA_SIZE)
        {
            if (South) { Remesh(); }
            South = false;
        }
        else
        {
            if (!South) { Remesh(); }
            South = true;
        }


        //checks out
        //GD.Print($"UP: {Up}, NORTH: {North}, EAST: {East}, SOUTH: {South}, WEST: {West}, DOWN: {Down}");

        if (!Meshed)
        {
            if (meshbytes != null)
            {
                PChunkByteIngestion(meshbytes);
            }
        }

        if ((pos - GlobalPosition).Length() > 2000)
        {
            //GD.Print($"Despawning chunk at: {GlobalPosition}");
            //QueueFree();
        }



        if (Regen)
        {
            Remesh();
        }
    }

    public void PChunkByteAssignment(byte[] quadbytes)
    {
        meshbytes = quadbytes;
        Meshed = false;
    }

    public void PChunkByteIngestion(byte[] quadbytes)
    {
        if (MeshInstance != null)
        {
            MeshInstance.QueueFree();
        }

        var PChunk = ClassDB.Instantiate("PChunk");
        PChunk.AsGodotObject().Call("set_bytes2", quadbytes, true);
        AddChild((MeshInstance3D)PChunk);
        MeshInstance = (MeshInstance3D)PChunk;
        MeshInstance.Mesh.CallDeferred(Mesh.MethodName.SurfaceSetMaterial, 0, GD.Load<ShaderMaterial>("res://Resources/Test.tres"));
        Meshed = true;

        Vector3[] vertices = (Vector3[])PChunk.AsGodotObject().Call("GetCollisionMesh");
        ConcavePolygon.SetFaces(vertices);
        Collision = true;

    }

    public void Remesh()
    {

        foreach(Tuple<Vector3I, int> change in ChangePackets)
        {
            ChunkData[change.Item1.X,change.Item1.Y,change.Item1.Z] = change.Item2;
        }
        ChangePackets.Clear();
        ChunkMeshManager.Instance().RequestChunkMeshUpdate(this, true);
        //ChunkMeshManager.Instance().GeneratePChunkMesh4(ChunkData, this);
        Regen = false;
    }

    public void QueueDataChange(Vector3I BlockPosition, int data)
    {
        GD.Print($"data position : {BlockPosition} at {ChunkCoordinates}");
        ChangePackets.Add(new Tuple<Vector3I, int>(BlockPosition, data));
        Regen = true;
    }

}
