using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class Inventory : Resource
{

	[Export]
	public Array<InventoryItem> Slots { get; set; }

	public void insert(InventoryItem test) {

	}
}
