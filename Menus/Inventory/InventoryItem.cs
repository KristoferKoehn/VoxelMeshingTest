using Godot;
using Godot.Collections;

[GlobalClass]
public partial class InventoryItem : Resource
{
    [Export]
    public string Name = "";
    [Export]
    public PackedScene Item = null;
    [Export]
    public Texture2D Sprite = null;
    [Export]
    public bool Stackable = false;
    [Export]
    public int StackAmount = 1;
    [Export]
    public int StackMax = 99;
    [Export]
    public Inventory.SlotType SlotType = Inventory.SlotType.None;
    [Export]
    public Array<Script> Components = new Array<Script>();
    [Export]
    public Dictionary StatChanges = new Dictionary();


    //make a component that attaches the thing to the correct slot. It can take in the packed scene?
    //and later it can RPC call attach the bullshit
}
