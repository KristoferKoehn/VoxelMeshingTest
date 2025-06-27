using Godot;
using System;
using System.Collections.Generic;

public partial class CandidateChunkFinder : Node
{
    private static CandidateChunkFinder instance;

    [Export] public float ForwardBias = 10f;

    private static CandidateChunkFinder Instance()
    {
        if (instance == null)
        {
            instance = new CandidateChunkFinder();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "CandidateChunkFinder";
        }
        return instance;
    }

    static public List<Vector3I> GetCandidateChunks(
       Vector3I centerChunk,
       int radius,
       HashSet<Vector3I> alreadyLoaded,
       Vector3 playerPos,
       Vector3 playerForward
    )
    {
        CandidateChunkFinder instance = Instance();
        List<(Vector3I, float)> scoredChunks = new();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -1; y <= 1; y++) // 3D or flat world adjustment
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3I chunkCoord = centerChunk + new Vector3I(x, y, z);

                    if (alreadyLoaded.Contains(chunkCoord))
                        continue;

                    Vector3 worldCenter = ChunkRegionHelper.ChunkToWorld(chunkCoord, ChunkManager.ChunkSize);
                    Vector3 toChunk = (worldCenter - playerPos).Normalized();
                    float dist = worldCenter.DistanceTo(playerPos);
                    float align = toChunk.Dot(playerForward);
                    float score = dist - (align * instance.ForwardBias);

                    scoredChunks.Add((chunkCoord, score));
                }
            }
        }

        scoredChunks.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        List<Vector3I> sortedChunks = new();
        foreach (var (coord, _) in scoredChunks)
            sortedChunks.Add(coord);

        return sortedChunks;
    }

}

public static class ChunkRegionHelper
{
    public static Vector3I WorldToChunk(Vector3 worldPos, int chunkSize)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPos.X / chunkSize),
            Mathf.FloorToInt(worldPos.Y / chunkSize),
            Mathf.FloorToInt(worldPos.Z / chunkSize)
        );
    }

    public static Vector3 ChunkToWorld(Vector3I chunkCoord, int chunkSize)
    {
        return new Vector3(
            chunkCoord.X * chunkSize,
            chunkCoord.Y * chunkSize,
            chunkCoord.Z * chunkSize
        );
    }
}