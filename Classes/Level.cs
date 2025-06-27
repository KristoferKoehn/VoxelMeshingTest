using Godot;
using System.Collections.Generic;

public partial class NewScript : Resource
{
    Dictionary<Vector3I, int[][][]> Chunks = new Dictionary<Vector3I, int[][][]>();

    public bool Generate = true;
    public bool SaveChunks = true;

    public int[][][] GetChunk(Vector3I pos)
    {
        if(Chunks.ContainsKey(pos)) return Chunks[pos];

        return null;
    }

    public void SaveChunk(Vector3I pos, int[][][] chunk)
    {
        Chunks[pos] = chunk;
    }

    //the angel requires you to perform some task. you hit the small rock for days and days and days
    //the angel asks you if you want to leave
    //if no, go back to the beginning and loop the question
    //when you say yes, the angel describes a glorious way to gain harmonious divinity. There is a wishing rock far to the east, if it is moved, you are granted two wishes.
    //it says that it finds you worthy of the attempt, describes the arduous path through many worlds, and sends you on your way.

    //you walk through the grassland towards the portal in the east.
    //when you reach the portal, you pass through into a grand desert. There are earthquakes, and bottomless pits. Sand plumes.
    //there are others here, they ask, they muse on their position on the divine ladder.
    //some feel it is too good to be true, but the mere chance that it *could* be possible pulls them forward.
    //the next portal brings you to the monolithic cave hall. The light filters down from the bright abyss above to the dark oblivion below, through the swiss-cheese stone above.
    //there are ones that have seen others fall. some are in shock, some smugly rationalize about their worthiness.
    //the last area, grass again. Small. There are many wishing stones. an angel guides you past many stones, some moved, some with people moving them.
    //you watch as the stone next to yours has individuals surrounding it, someone pushes the stone after great effort. The people around cheer.
    //it is your turn to move your stone. you interact with the stone and the sun moves down towards the horizon.
    //you interact again and it's night time. You interact again, it is morning. 
    //when you look at the angel, it conveys to you that perhaps this wasn't to be. that you should return back to the stone farm.
    //you can choose to walk back for an ending. you return to the stone farm, start hitting stones, and the screen fades. THE END
    //you can interact with the wishing stone more. the angel leaves. after several interactions, a man appears next to you and asks you if you want to know the secret.
    //he imparts that the wishing stone you were lead to does not grant wishes, has never granted wishes, and will never grant wishes because it will never move.
    //he shows you the depth of the wishing stone, the sky turns black and the ground fades away. your wishing stone extends far into the ground.
    //perhaps one should consider the meaning of the stone
    //and why you were sent here.
    //the game ends.
}
