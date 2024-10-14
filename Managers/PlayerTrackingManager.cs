using Godot;

public partial class PlayerTrackingManager : Node
{

	[Export]
	public Node3D TrackingItem { get; set; }

    private static PlayerTrackingManager instance;

    public static PlayerTrackingManager Instance()
    {
        if (instance == null)
        {
            instance = new PlayerTrackingManager();
            SceneSwitcher.Instance().AddChild(instance);
            instance.Name = "PlayerTrackingManager";
        }
        return instance;
    }

    public static Vector3 GetPlayerLocation()
    {
        return instance.TrackingItem.GlobalPosition;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        if (instance == null)
        {
            instance = this;
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
