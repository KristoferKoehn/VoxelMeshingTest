using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;


public partial class Chunk : Node3D
{
    public int[,,] ChunkData = null;

    public bool Regen = false;
    public List<Tuple<Vector3I, int>> ChangePackets = new();

    public bool Generated = false;
    public bool Meshed = false;
    public bool Animating = false;
    public bool Collision = false;

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

    public bool ImmediateChunk = false;
    public bool FirstGenerated = false;

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
        
        if ((pos - GlobalPosition).Length() > 520 && !Animating)
        {

            Animating = true;
            Tween tween = GetTree().CreateTween();
            tween.SetTrans(Tween.TransitionType.Spring);
            tween.TweenProperty(this, "global_position", GlobalPosition + new Vector3(0, -32, 0), 0.5);
            tween.Finished += () => {
                tween.Dispose();
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
                tween.Finished += tween.Dispose;
                tween.Finished += () => { 
                    Animating = false;
                    FirstGenerated = true;
                };
            }

            Meshed = false;
        }
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
