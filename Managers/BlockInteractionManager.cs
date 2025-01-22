using Godot;
using System;

public partial class BlockInteractionManager : Node
{

    private static BlockInteractionManager instance;

    private BlockInteractionManager()
    {

    }

    public static BlockInteractionManager Instance()
    {
        if (instance == null)
        {
            instance = new BlockInteractionManager();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "BlockInteractionManager";
        }
        return instance;
    }



    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
