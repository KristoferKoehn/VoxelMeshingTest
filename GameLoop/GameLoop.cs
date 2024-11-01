using Godot;
using System.Collections.Generic;
using VoxelMeshingTest.Classes;

public partial class GameLoop : Node3D
{
    [Export]
    public FastNoiseLite Terrain { get; set; }
    [Export]
    public FastNoiseLite SurfaceCutoff { get; set; }

    [Export]
    public Vector2 CutoffOffset { get; set; }

    [Export]
    RayCast3D RayCast { get; set; }

    public List<Chunk> Chunks { get; set; } = new List<Chunk>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        RNGManager.Instance();
        ChunkGeneratorManager.Instance();

        ChunkGeneratorManager.Terrain = Terrain;
        ChunkGeneratorManager.SurfaceCutoff = SurfaceCutoff;
        ChunkGeneratorManager.CutoffOffset = CutoffOffset;
        ChunkMeshManager.Instance();
        //ChunkGeneratorManager.Instance().PreGenerate();
        ChunkSpawnManager.Instance();
        //ChunkSpawnManager.Instance().GenerateWorld();

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("regenerate"))
        {
            //ChunkSpawnManager.Instance().GenerateWorld();
        }
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
                    GD.Print($"{block}, {ch.GlobalPosition}, {RayCast.GetCollisionPoint()}, block data: {ch.ChunkData[(block.Z + 1) + (block.Y + 1) * GameConstants.CHUNK_DATA_SIZE + (block.X + 1) * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE]}");

                    ch.ChunkData[(block.Z + 1) + (block.Y + 1) * GameConstants.CHUNK_DATA_SIZE + (block.X + 1) * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE] = 0;
                    ch.Generated = false;
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
                    GD.Print($"{block}, {ch.GlobalPosition}, {RayCast.GetCollisionPoint()}, block data: {ch.ChunkData[(block.Z + 1) + (block.Y + 1) * GameConstants.CHUNK_DATA_SIZE + (block.X + 1) * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE]}");

                    ch.ChunkData[(block.Z + 1) + (block.Y + 1) * GameConstants.CHUNK_DATA_SIZE + (block.X + 1) * GameConstants.CHUNK_DATA_SIZE * GameConstants.CHUNK_DATA_SIZE] = 1;
                    ch.Generated = false;
                }
            }
        }
    }
}
