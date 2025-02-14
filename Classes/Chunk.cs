using Godot;
using System;
using System.Collections.Generic;
using VoxelMeshingTest.Classes;


public partial class Chunk : Node3D
{
    public int[,,] ChunkData = null;

    public bool Generated = false;
    public bool Meshed = false;
    public bool Animating = false;
    public bool Deleting = false;
    public bool Stale = false;


    public MeshInstance3D MeshInstance;
    public StaticBody3D SB;
    public CollisionShape3D CollisionShape;
    public ConcavePolygonShape3D ConcavePolygon;

    public Vector3 ChunkPosition { get; set; }
    public Vector3I ChunkCoordinates { get; set; }

    public byte[] meshbytes { get; set; } = null;

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

    public override void _ExitTree()
    {
        ChunkSpawnManager.Instance().DeleteAllChunks -= SpecialDispose;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
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
            MeshData.Clear();
            MeshData = null;
            MeshInstance.MaterialOverride = GD.Load<ShaderMaterial>("res://Resources/Test.tres");

            Meshed = true;

            if (!FirstGenerated)
            {
                Animating = true;
                MeshInstance.Position += new Vector3(0, -16, 0);
            }

        }

        Vector3 pos = PlayerTrackingManager.Instance().GetPlayerLocation();
        
        if ((pos - GlobalPosition).Length() > GameConstants.DESPAWN_RADIUS * 72 && !Animating)
        {
            Animating = true;
            Tween tween = GetTree().CreateTween();
            tween.SetTrans(Tween.TransitionType.Spring);
            tween.TweenProperty(MeshInstance, "position", MeshInstance.Position + new Vector3(0, -32, 0), 0.3);
            tween.Finished += () => {
                //GD.Print($"Despawning chunk at: {GlobalPosition}");
                ChunkSpawnManager.Instance().DeregisterChunk(this, ChunkCoordinates);
                //QueueFree();
                CallDeferred("queue_free");
            };
        }

        if (Meshed)
        {
            if (!FirstGenerated) {
                Tween tween = GetTree().CreateTween();
                tween.SetTrans(Tween.TransitionType.Spring);
                tween.TweenProperty(MeshInstance, "position", MeshInstance.Position + new Vector3(0, 16, 0), 0.25);
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
    }

    public void QueueDataChange(Vector3I BlockPosition, int data)
    {
        GD.Print($"data position : {BlockPosition} at {ChunkCoordinates}");
        //ChangePackets.Add(new Tuple<Vector3I, int>(BlockPosition, data));
        ChunkData[BlockPosition.X, BlockPosition.Y, BlockPosition.Z] = data;
        Stale = true;
    }
    //end queue data change system. SHould I care about this

    public void SpecialDispose()
    {
        if (IsInstanceValid(this) && !IsQueuedForDeletion())
        {
            ChunkSpawnManager.Instance().DeregisterChunk(this, ChunkCoordinates);
            ChunkData = null;
            CallDeferred("queue_free");
        }
    }
}