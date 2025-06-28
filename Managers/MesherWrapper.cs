using Godot;
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

	public ArrayMesh GenerateAndMeshChunk(Vector3 pos)
	{
		return (ArrayMesh)mesher.Call("generate_and_mesh", pos);
	}

	
}
