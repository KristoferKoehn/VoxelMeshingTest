using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VoxelMeshingTest.Classes;

public partial class ChunkManager : Node
{

    private static ChunkManager instance;

    static public float UpdateInterval = 0.1f;

    private ConcurrentDictionary<Vector3I, bool> loadedChunks = new();
    ChunkQueue pq;

    Godot.Timer ChunkTimer;

    Vector3 main_camera_position = Vector3.Zero;
    Vector3 main_camera_direction = Vector3.Zero;



    private ChunkManager()
    {
        pq = new ChunkQueue(GetPriority);
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
        Instance();
        ChunkTimer = new Godot.Timer();
        this.AddChild(ChunkTimer);
        ChunkTimer.Start(UpdateInterval);
        ChunkTimer.Timeout += QueryChunks;

        for (int i = 0; i < 1; i++)
        {
            var thread = new Thread(() => WorkerThread(pq));
            thread.IsBackground = true;
            thread.Start();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        main_camera_position = GetTree().Root.GetViewport().GetCamera3D().GlobalPosition;
        main_camera_direction = GetTree().Root.GetViewport().GetCamera3D().Quaternion * new Vector3(0, 0, -1);
    }

    public void QueryChunks()
    {
        Vector3 playerPos = main_camera_position;
        Vector3I pos_c = (Vector3I)(playerPos / 64.0f).Floor();

        int diameter = GameConstants.SPAWN_RADIUS * 2;
        int half = diameter / 2;

        for (int i = -half; i <= half; i++)
        {
            for (int j = -half; j <= half; j++)
            {
                // Only include chunks within circular distance
                if (new Vector2(i, j).Length() > half)
                    continue;

                Vector3I chunkPos = new Vector3I(pos_c.X + i, 0, pos_c.Z + j);

                if (!loadedChunks.ContainsKey(chunkPos))
                {
                    pq.Enqueue(chunkPos);
                }
            }
        }
    }

    float GetPriority(Vector3I chunk)
    {
        Vector3 c_pos = main_camera_position;
        Vector3 forwardView = main_camera_direction;
        Vector3 worldPos = chunk * 64;
        float distance = (c_pos - worldPos).Length();
        float alignment = (worldPos - c_pos).Normalized().Dot(forwardView);

        // Weighted score: prioritize alignment but still consider distance
        return alignment - (distance * GameConstants.ALIGNMENT_SCORE_WEIGHT); // Adjust weighting as needed
    }

    public void DeregisterChunk(Vector3I pos)
    {
        if (loadedChunks.ContainsKey(pos))
        {
            loadedChunks.TryRemove(new KeyValuePair<Vector3I, bool>(pos, loadedChunks[pos]));
        }
    } 

    public class ChunkQueue
    {

        private readonly object _dequeueLock = new();
        private readonly ConcurrentDictionary<Vector3I, byte> _chunkSet = new();

        private readonly Func<Vector3I, float> _getPriority;

        public ChunkQueue(Func<Vector3I, float> getPriority)
        {
            _getPriority = getPriority;
        }

        public void Enqueue(Vector3I chunk)
        {
            _chunkSet.TryAdd(chunk, 0); // Add if not already present
        }

        public List<Vector3I> DequeueBatch(int maxCount)
        {
            var batch = _chunkSet.Keys
            //.Where(_isStillRelevant) // Cull stale chunks // we're going to cull later
            .OrderByDescending(_getPriority) // Prioritize
            .Take(maxCount)
            .ToList();

            var list = new List<Vector3I>();
            foreach (var chunk in batch)
            {
                if (_chunkSet.TryRemove(chunk, out _))
                {
                    list.Add(chunk);
                }// Remove from queue

            }

            return list;
        }
    }

    void WorkerThread(ChunkQueue queue)
    {
        while (true)
        {
            var batch = queue.DequeueBatch(1); // Pull an amount

            if (batch.Count == 0)
            {
                Thread.Sleep(1); // Avoid busy spin
                continue;
            }

            Vector3 p = PlayerTrackingManager.Instance().GetPlayerLocation();
            Vector3I CameraChunkCoord = new Vector3I(
                                                    Mathf.FloorToInt(p.X / 64.0f),
                                                    Mathf.FloorToInt(0),
                                                    Mathf.FloorToInt(p.Z / 64.0f)
                                                    );

            foreach (Vector3I chunk in batch)
            {
                if ((CameraChunkCoord - chunk).Length() <= GameConstants.DESPAWN_RADIUS)
                {
                    loadedChunks[chunk] = true;
                    C_Chunk m = new C_Chunk();
                    ArrayMesh am = new ArrayMesh();
                    m.Mesh = am;
                    m.Position = chunk * 64;
                    m.VoxelData = MesherWrapper.ProcessChunk(m, m.VoxelData, true);
                    CallDeferred("add_child", m);
                    m.Position -= new Vector3(0, 32, 0); //this is for the spawn in animation. if this is done before ProcessChunk, it messes with the generation
                }
                //else cull
            }
        }
    }
}
