
using Godot;

namespace VoxelMeshingTest.Classes
{
    public class GameConstants
    {
        public static int CHUNK_SIZE = 64;
        public static int CHUNK_DATA_SIZE = CHUNK_SIZE + 2;
        public static int WORKGROUPS = 32;
        public static uint BUFFER_SIZE = 786432 * 12 * 4;
        public static int SPAWN_RADIUS = 4;
        public static int DESPAWN_MARGIN = 1;
        public static int DESPAWN_RADIUS = SPAWN_RADIUS + DESPAWN_MARGIN;
        public static int CHUNK_THREADS = 1;
        public static int WORLD_DEPTH = -3;
        public static int WORLD_HEIGHT = 2;
        public static float ALIGNMENT_SCORE_WEIGHT = 0.01f;
    }
}
