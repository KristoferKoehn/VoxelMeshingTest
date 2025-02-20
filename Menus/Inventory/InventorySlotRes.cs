using Godot;
using System;

[GlobalClass]
public partial class InventorySlotRes : Resource
{
    [Export]
    public InventoryItem Item;
    [Export]
    public int Amount;
    [Export]
    public bool Stackable;
}
