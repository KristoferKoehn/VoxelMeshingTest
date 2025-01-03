using Godot;
using System.Collections.Generic;
using VoxelMeshingTest.Classes;

public partial class GameLoop : Node3D
{
    [Export]
    public FastNoiseLite Terrain1 { get; set; }
    [Export]
    public FastNoiseLite Terrain2 { get; set; }
    [Export]
    public FastNoiseLite Terrain3 { get; set; }
    [Export]
    public FastNoiseLite SurfaceCutoff { get; set; }
    [Export]
    public FastNoiseLite TemperatureValues { get; set; }
    [Export]
    public FastNoiseLite GenQualityValue { get; set; }


    [Export]
    RayCast3D RayCast { get; set; }

    public List<Chunk> Chunks { get; set; } = new List<Chunk>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("WE get here at ready");
        RNGManager.Instance();
        ChunkGeneratorManager.Instance();

        ChunkGeneratorManager.Terrain1 = Terrain1;
        ChunkGeneratorManager.Terrain2 = Terrain2;
        ChunkGeneratorManager.Terrain3 = Terrain3;
        ChunkGeneratorManager.SurfaceCutoff = SurfaceCutoff;
        ChunkGeneratorManager.BiomeTemp = TemperatureValues;
        ChunkGeneratorManager.BiomeQual = GenQualityValue;
        ChunkMeshManager.Instance();
        //ChunkGeneratorManager.Instance().PreGenerate();
        ChunkSpawnManager.Instance();
        //ChunkSpawnManager.Instance().GenerateWorld();

        ChunkSpawnManager.Instance().HandleChunkLoading();

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("click"))
        {
            if (RayCast.IsColliding())
            {
                Chunk ch = ((Node)RayCast.GetCollider()).GetParent() as Chunk;
                if (ch != null)
                {
                    Vector3 pos = new Vector3(0.5f, 0.5f, 0.5f) + RayCast.GetCollisionPoint() - ch.GlobalPosition - RayCast.GetCollisionNormal() * 0.1f;

                    Vector3I block = new Vector3I(Mathf.FloorToInt(pos.X), Mathf.FloorToInt(pos.Y), Mathf.FloorToInt(pos.Z)) + new Vector3I(1, 1, 1) * GameConstants.CHUNK_SIZE/2;

                    if (block.X == 0)
                    {
                        Chunk adjacent = ChunkSpawnManager.Instance().GetChunk(ch.ChunkCoordinates + new Vector3I(-1, 0, 0));
                        if (adjacent != null)
                        {
                            adjacent.QueueDataChange(block + new Vector3I(GameConstants.CHUNK_DATA_SIZE - 1,1,1), 0);
                            GD.Print($"did a chunk transfer remeshing at x = {GameConstants.CHUNK_DATA_SIZE -1} on chunk: {adjacent.ChunkCoordinates}");
                        }
                    }

                    if (block.X == GameConstants.CHUNK_DATA_SIZE - 3)
                    {
                        Chunk adjacent = ChunkSpawnManager.Instance().GetChunk(ch.ChunkCoordinates + new Vector3I(1, 0, 0));
                        if (adjacent != null)
                        {
                            adjacent.QueueDataChange(block + new Vector3I(3-GameConstants.CHUNK_DATA_SIZE, 1, 1), 0);
                            GD.Print($"did a chunk transfer remeshing at x = {GameConstants.CHUNK_DATA_SIZE + 1} on chunk: {adjacent.ChunkCoordinates}");
                        }
                    }

                    if (block.Y == 0)
                    {
                        Chunk adjacent = ChunkSpawnManager.Instance().GetChunk(ch.ChunkCoordinates + new Vector3I(0, -1, 0));
                        if (adjacent != null)
                        {
                            adjacent.QueueDataChange(block + new Vector3I(1 , GameConstants.CHUNK_DATA_SIZE - 1, 1), 0);
                            //adjacent.Regen = true;
                            GD.Print($"did a chunk transfer remeshing at y = {GameConstants.CHUNK_DATA_SIZE - 1} on chunk: {adjacent.ChunkCoordinates}");
                        }
                    }

                    if (block.Y == GameConstants.CHUNK_DATA_SIZE - 3)
                    {
                        Chunk adjacent = ChunkSpawnManager.Instance().GetChunk(ch.ChunkCoordinates + new Vector3I(0, 1, 0));
                        if (adjacent != null)
                        {
                            adjacent.QueueDataChange(block + new Vector3I(1, 3 - GameConstants.CHUNK_DATA_SIZE, 1), 0);
                            //adjacent.Regen = true;
                            GD.Print($"did a chunk transfer remeshing at y = {GameConstants.CHUNK_DATA_SIZE + 1} on chunk: {adjacent.ChunkCoordinates}");
                        }
                    }

                    ch.QueueDataChange(block + new Vector3I(1,1,1), 0);
                    ch.Regen = true;
                }
            }
        }

        if (@event.IsActionPressed("place"))
        {
            if (RayCast.IsColliding())
            {
                Chunk ch = ((Node)RayCast.GetCollider()).GetParent() as Chunk;
                if (ch != null)
                {
                    Vector3 pos = new Vector3(0.5f, 0.5f, 0.5f) + RayCast.GetCollisionPoint() - ch.GlobalPosition + RayCast.GetCollisionNormal() * 0.1f;

                    Vector3I block = new Vector3I(Mathf.FloorToInt(pos.X), Mathf.FloorToInt(pos.Y), Mathf.FloorToInt(pos.Z)) + new Vector3I(1, 1, 1) * GameConstants.CHUNK_SIZE / 2;
                    if (block.X == 0)
                    {
                        Chunk adjacent = ChunkSpawnManager.Instance().GetChunk(ch.ChunkCoordinates - new Vector3I(-1, 0, 0));
                        if (adjacent != null)
                        {
                            adjacent.QueueDataChange(block + new Vector3I(1, 1, 1), 1);
                            GD.Print("did a chunk transfer remeshing");
                        }
                    }

                    //ch.ChunkData[(block.Z + 1) + (block.Y + 1) * GameConstants.CHUNK_DATA_SIZE + (block.X + 1) * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE] = 1;
                    ch.QueueDataChange(block + new Vector3I(1, 1, 1), 1);
                    ch.Regen = true;
                    //ch.Remesh();
                }
            }
        }
    }
}
