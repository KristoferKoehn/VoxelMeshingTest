using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkManager : Node
{

    private static ChunkManager instance;

    static public int ChunkSize = 16;
    static public int LoadRadius = 8;
    static public float UpdateInterval = 0.25f;

    private HashSet<Vector3I> loadedChunks = new();
    private Vector3 lastPlayerChunkPos = Vector3.Zero;
    private float timeSinceLastUpdate = 0f;


    private ChunkManager()
    {

    }

    public static ChunkManager Instance()
    {
        if (instance == null)
        {
            instance = new ChunkManager();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "ChunkManager";
        }
        return instance;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        timeSinceLastUpdate += (float)delta;

        if (timeSinceLastUpdate >= UpdateInterval)
        {
            timeSinceLastUpdate = 0f;
            Vector3 playerPos = GetPlayerPosition();
            Vector3I playerChunk = ChunkRegionHelper.WorldToChunk(playerPos, ChunkSize);

            if ((playerChunk - lastPlayerChunkPos).Length() >= 1)
            {
                lastPlayerChunkPos = playerChunk;
                RequestCandidateChunks(playerChunk);
            }
        }
    }

    private Vector3 GetPlayerPosition()
    {
        var player = GetNodeOrNull<Node3D>("/root/Player");
        return player?.GlobalPosition ?? Vector3.Zero;
    }

    private void RequestCandidateChunks(Vector3I centerChunk)
    {
        List<Vector3I> candidates = CandidateChunkFinder.GetCandidateChunks(
            centerChunk,
            LoadRadius,
            loadedChunks,
            GetPlayerPosition(),
            GetPlayerDirection()
        );

        foreach (var chunkPos in candidates)
        {
            // Send chunk job to worker pool
            ChunkWorkerPool.Instance.QueueChunkJob(chunkPos, ChunkSize);
            loadedChunks.Add(chunkPos); // Pre-mark to avoid duplicate work
        }
    }

    private Vector3 GetPlayerDirection()
    {
        var player = GetNodeOrNull<Node3D>("/root/Player");
        return player?.GlobalTransform.Basis.Z.Normalized() ?? Vector3.Forward;
    }

    public void RegisterLoadedChunk(Vector3I chunkCoord)
    {
        loadedChunks.Add(chunkCoord);
    }

    public bool IsChunkLoaded(Vector3I chunkCoord)
    {
        return loadedChunks.Contains(chunkCoord);
    }
}
