using Godot;
using Godot.Collections;

public partial class InventoryPanel : Panel2
{
    [Export]
    Inventory Inventory { get; set; }
    [Export]
    GridContainer Container { get; set; }

    Array Slots { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Slots = (Array)Container.GetChildren();
		base._Ready();
        UpdateSlots();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Visible = false;
        }
        else
        {
            Visible = true;
        }
    }

    public void UpdateSlots()
    {
        for (int i = 0; i < Mathf.Min(Slots.Count, Inventory.Slots.Count); i++) {
            ((InventorySlot)Slots[i]).Update(Inventory.Slots[i]);
        }
    }
}
