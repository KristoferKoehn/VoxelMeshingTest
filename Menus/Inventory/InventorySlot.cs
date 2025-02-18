using Godot;
using System;

public partial class InventorySlot : Panel
{
	[Export]
	Sprite2D Sprite2D { get; set; }


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Update(InventoryItem inventoryItem)
	{
		if (inventoryItem != null)
		{
            Sprite2D.Visible = true;
			Sprite2D.Texture = inventoryItem.Sprite;
        }
		else
		{
			Sprite2D.Visible = false;
		}
	}
}
