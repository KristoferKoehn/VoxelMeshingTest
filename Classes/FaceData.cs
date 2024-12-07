using Godot;
using Godot.Collections;

[GlobalClass]
public partial class FaceData : Resource
{
    //FACE is a construct used to describe the totality of meshes associated with a single cardinal direction of a voxel
    [Export]
    public int ID;
    [Export]
    public bool transparent;
    [Export]
    public Array<QuadData> UpFace;
    [Export]
    public Array<QuadData> NorthFace;
    [Export]
    public Array<QuadData> EastFace;
    [Export]
    public Array<QuadData> SouthFace;
    [Export]
    public Array<QuadData> WestFace;
    [Export]
    public Array<QuadData> DownFace;


    public Array<Array<QuadData>> GetFacesArray()
    {
        return new Array<Array<QuadData>> {   UpFace, NorthFace, EastFace, SouthFace, WestFace, DownFace};
    }
    //the general idea is that the binary representation of this class will be a series of indicies of offsets pointing to quads in the quad array.
    //these will be interpreted as shader-structs 

}
