
using Godot;

namespace VoxelMeshingTest.Classes
{
    public class GameConstants
    {
        public static int CHUNK_SIZE = 64;
        public static int CHUNK_DATA_SIZE = CHUNK_SIZE + 2;
        public static int WORKGROUPS = 32;
        public static uint BUFFER_SIZE = 786432 * 12 * 4;
        public static int SPAWN_RADIUS = 8;
        public static int CHUNK_THREADS = 4;
    }
}
