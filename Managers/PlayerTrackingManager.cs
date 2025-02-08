using Godot;

public partial class PlayerTrackingManager : Node
{

	[Export]
	public Node3D TrackingItem { get; set; }

    private static PlayerTrackingManager instance;

    public Vector3 TrackerPosition { get; set; } = Vector3.Zero;
    public Vector3 PreviousPosition { get; set; } = Vector3.Zero;
    public Basis TrackerBasis { get; set; } = Basis.Identity;
    public Vector3 TrackerVelocity { get; set; } = Vector3.Zero;

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

    public Vector3 GetPlayerLocation()
    {
        return instance.TrackerPosition;
    }

    public Vector3 GetPlayerVelocity()
    {
        return instance.TrackerVelocity;
    }

    public Basis GetPlayerBasis()
    {
        return instance.TrackerBasis;
    }

    public Node3D GetPlayerNode() {
        return instance.TrackingItem; 
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
        TrackingItem = GetViewport().GetCamera3D();
        TrackerPosition = TrackingItem.GlobalPosition;
        TrackerVelocity = TrackerPosition - PreviousPosition;
        TrackerBasis = TrackingItem.GlobalBasis;
        PreviousPosition = TrackerPosition;
    }
}
