using Godot;
using System;

[GlobalClass]
public partial class QuadData : Resource
{
    [Export]
    public Vector3[] vertices = new Vector3[4];

    [Export]
    public Color[] Color = new Color[4];

    [Export]
    public Vector3 Normals = new Vector3();

    [Export]
    public bool Greedy = false;

    [Export]
    public float UVTextureIndex = 0;

    [Export]
    public float UVMetallicityIndex = 0;

    [Export]
    public float UVTransparencyIndex = 0;

    [Export]
    public float UVEmissivenessIndex = 0;

    public int NextFace = -1;

    //This data must be converted to shader-struct

    /// <summary>
    /// returns the binary representation of the quad
    /// </summary>
    /// <returns>132 bytes such that in order, 12 floats (position), 12 floats (color), 3 floats (normal), 4 ints (Custom0 data, metallicity etc,
    /// 1 boolish (greedy flag), 1 int (next quad index)
    /// </returns>
    public byte[] Serialize()
    {
        float[] floats = new float[31] { 
            //12 floats
            vertices[0].X, vertices[0].Y, vertices[0].Z,
            vertices[1].X, vertices[1].Y, vertices[1].Z,
            vertices[2].X, vertices[2].Y, vertices[2].Z,
            vertices[3].X, vertices[3].Y, vertices[3].Z,

            //12 floats
            Color[0].R, Color[0].G, Color[0].B,
            Color[1].R, Color[1].G, Color[1].B,
            Color[2].R, Color[2].G, Color[2].B,
            Color[3].R, Color[3].G, Color[3].B,

            //4 floats
            UVTextureIndex, UVMetallicityIndex, UVEmissivenessIndex, UVTransparencyIndex,

            //3 floats
            Normals.X, Normals.Y, Normals.Z,
        };

        int[] ints = new int[2] {
                        //greedy 1 float
            Greedy ? 1 : 0,

            //next face index in array, 1 int
            NextFace,
        };

        byte[] bytes = new byte[132];
        Buffer.BlockCopy(floats, 0, bytes, 0, 124);
        Buffer.BlockCopy(ints, 0, bytes, 124, 8);
        return bytes;
    }
}
