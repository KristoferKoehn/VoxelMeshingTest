using Godot;
using Godot.NativeInterop;
using System;
using System.Runtime.CompilerServices;

public partial class MesherWrapper : Node
{
    Node mesher;
    private static MesherWrapper instance;

    public static MesherWrapper Instance()
    {
        if (instance == null)
        {
            instance = new MesherWrapper();
            SceneSwitcher.root.CallDeferred("add_child", instance);
            instance.Name = "MesherWrapper";
        }

        return instance;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        mesher = (Node)ClassDB.Instantiate("GDExample");
        instance.AddChild(mesher);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public static ArrayMesh GenerateAndMeshChunk(Vector3 pos)
	{
		return (ArrayMesh)Instance().mesher.Call("generate_and_mesh", pos * 64);
	}

    public static int[] ProcessChunk(MeshInstance3D m, int[] data, bool generate_data)
    {
        return (int[])Instance().mesher.Call("process_chunk", m, data, generate_data);
    }
}
