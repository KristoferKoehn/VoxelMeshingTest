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
    public bool Animating = false;
    public bool Deleting = false;
    public bool Stale = false;


    public MeshInstance3D MeshInstance;
    public StaticBody3D SB;
    public CollisionShape3D CollisionShape;
    public ConcavePolygonShape3D ConcavePolygon;
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

    public bool FirstGenerated = false;


    public Godot.Collections.Array MeshData = null;


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

    public override void _EnterTree()
    {
        GlobalPosition = ChunkPosition;
        Meshed = true;
        if (!FirstGenerated)
        {
            Animating = true;
            GlobalPosition += new Vector3(0, -32, 0);
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        

        if (MeshInstance != null)
        {
            MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");
        } else
        {
            GD.Print("material fucked");
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
        
        
        if ((pos - GlobalPosition).Length() > GameConstants.DESPAWN_RADIUS * 128 && !Animating)
        {
            Animating = true;
            Tween tween = GetTree().CreateTween();
            tween.SetTrans(Tween.TransitionType.Spring);
            tween.TweenProperty(this, "global_position", GlobalPosition + new Vector3(0, -32, 0), 0.3);
            tween.Finished += () => {

                GD.Print($"Despawning chunk at: {GlobalPosition}");
                ChunkSpawnManager.Instance().DeregisterChunk(this, ChunkCoordinates);
                //QueueFree();

                CallDeferred("queue_free");

            };
        } 

        if (Regen)
        {
            Remesh();
        }

        if (Meshed)
        {
            
            if (!FirstGenerated) {
                Tween tween = GetTree().CreateTween();
                tween.SetTrans(Tween.TransitionType.Spring);
                tween.TweenProperty(this, "global_position", GlobalPosition + new Vector3(0, 32, 0), 0.5);
                tween.Finished += () => { 
                    Animating = false;
                    FirstGenerated = true;
                };
            }
            /*
            Animating = false;
            FirstGenerated = true;
            */
            Meshed = false;
        }

        if (MeshData != null)
        {
            ArrayMesh am = new ArrayMesh();
            if (MeshInstance != null)
            {
                MeshInstance.QueueFree();
            }

            MeshInstance = new MeshInstance3D();
            AddChild(MeshInstance);
            MeshInstance.Mesh = am;
            am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshData);
            MeshData = null;
            MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");
        }
    }

    // queue data change system 
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
    //end queue data change system. SHould I care about this

    public void DoneMeshing()
    {
        Meshed = true;
        if (!FirstGenerated)
        {
            Animating = true;
            GlobalPosition += new Vector3(0, -32, 0);
        }
    }
}
