using Godot;

public partial class InventorySlot : Panel
{
	[Export]
	Sprite2D Sprite2D { get; set; }
	[Export]
	InventorySlot item { get; set; }
	[Export]
	int amount { get; set; } = 0;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void Update(InventorySlotRes InventorySlot)
	{
		if (InventorySlot.Item != null)
		{
            Sprite2D.Visible = true;
			Sprite2D.Texture = InventorySlot.Item.Sprite;
        }
		else
		{
			Sprite2D.Visible = false;
		}
	}
}
