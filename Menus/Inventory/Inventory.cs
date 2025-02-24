using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Resource
{
	public enum SlotType {
		None,
		Weapon,
		Armor,
		Feet,
		Legs,
		Ring,
		Head,
		Hands,
		Shoulders,
		Shield,
		Sword,
		Axe,
		Staff,
		Hammer,
		Clown_Hammer,

	}

	[Export]
	public Array<InventoryItem> Slots { get; set; }

	[Export]
	public Array<InventoryItem> Equips { get; set; }

	[Export]
	public Array<SlotType> InventorySlots { get; set; }

	public void insert(InventoryItem test) {

	}
}
